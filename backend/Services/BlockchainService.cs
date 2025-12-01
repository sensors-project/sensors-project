using System.Net.Mime;
using System.Numerics;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.ContractHandlers;
using MongoDB.Driver;
using backend.Models;

namespace backend.Services;

// Contract function definitions
[Function("registerSensor")]
public class RegisterSensorFunction : FunctionMessage
{
    [Parameter("uint256", "sensorId", 1)]
    public BigInteger SensorId { get; set; }

    [Parameter("address", "walletAddress", 2)]
    public string WalletAddress { get; set; } = string.Empty;
}

[Function("rewardSensor")]
public class RewardSensorFunction : FunctionMessage
{
    [Parameter("uint256", "sensorId", 1)]
    public BigInteger SensorId { get; set; }
}

[Function("getSensorBalance", "uint256")]
public class GetSensorBalanceFunction : FunctionMessage
{
    [Parameter("uint256", "sensorId", 1)]
    public BigInteger SensorId { get; set; }
}

/// <summary>
/// Service for interacting with the blockchain and smart contract
/// </summary>
public class BlockchainService
{
    private readonly ILogger<BlockchainService> _logger;
    private readonly BlockchainConfig _config;
    private readonly IMongoCollection<SensorWallet> _sensorWalletCollection;
    private readonly IMongoCollection<TokenTransfer> _tokenTransferCollection;
    private readonly Web3? _web3;
    private readonly Account? _account;

    public BlockchainService(IConfiguration configuration, ILogger<BlockchainService> logger)
    {
        _logger = logger;

        // Load blockchain configuration with safe parsing
        var chainIdStr = configuration["BLOCKCHAIN_CHAIN_ID"]
                         ?? Environment.GetEnvironmentVariable("BLOCKCHAIN_CHAIN_ID")
                         ?? "31337";
        if (!int.TryParse(chainIdStr, out var chainId))
        {
            chainId = 31337;
            logger.LogWarning("Invalid BLOCKCHAIN_CHAIN_ID value '{Value}', using default 31337", chainIdStr);
        }

        var rewardStr = configuration["BLOCKCHAIN_REWARD_AMOUNT"]
                        ?? Environment.GetEnvironmentVariable("BLOCKCHAIN_REWARD_AMOUNT")
                        ?? "1";
        if (!decimal.TryParse(rewardStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rewardAmount))
        {
            rewardAmount = 1m;
            logger.LogWarning("Invalid BLOCKCHAIN_REWARD_AMOUNT value '{Value}', using default 1", rewardStr);
        }

        _config = new BlockchainConfig
        {
            RpcUrl = configuration["BLOCKCHAIN_RPC_URL"]
                     ?? Environment.GetEnvironmentVariable("BLOCKCHAIN_RPC_URL")
                     ?? "http://localhost:8545",
            ContractAddress = configuration["BLOCKCHAIN_CONTRACT_ADDRESS"]
                              ?? Environment.GetEnvironmentVariable("BLOCKCHAIN_CONTRACT_ADDRESS")
                              ?? "",
            OwnerPrivateKey = configuration["BLOCKCHAIN_PRIVATE_KEY"]
                              ?? Environment.GetEnvironmentVariable("BLOCKCHAIN_PRIVATE_KEY")
                              ?? "",
            ChainId = chainId,
            RewardAmount = rewardAmount
        };

        // Setup MongoDB collections
        var connectionString = configuration["MONGODB_CONNECTION_STRING"]
                               ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
                               ?? "mongodb://root:example@localhost:27017/?authSource=admin";

        var databaseName = configuration["MONGODB_DATABASE"]
                           ?? Environment.GetEnvironmentVariable("MONGODB_DATABASE")
                           ?? "sensorsdb";

        if (string.IsNullOrEmpty(_config.ContractAddress) || string.IsNullOrEmpty(_config.OwnerPrivateKey))
        {
            _logger.LogCritical("Contract address or owner private key is not set.");
            System.Environment.Exit(1);
        }

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _sensorWalletCollection = database.GetCollection<SensorWallet>("sensor_wallets");
        _tokenTransferCollection = database.GetCollection<TokenTransfer>("token_transfers");

        CreateIndexes();

        try
        {
            _account = new Account(_config.OwnerPrivateKey, _config.ChainId);
            _web3 = new Web3(_account, _config.RpcUrl);
            _logger.LogInformation("Blockchain service initialized with account: {Address}", _account.Address);

            _logger.LogInformation("Blockchain service enabled. RPC: {RpcUrl}, Contract: {Contract}",
                _config.RpcUrl, _config.ContractAddress);
        }
        catch (Exception ex)
        {
            throw new Exception("Blockchain service initialization failed.", ex);
        }
    }

    private void CreateIndexes()
    {
        try
        {
            var walletIndexKeys = Builders<SensorWallet>.IndexKeys.Ascending(x => x.SensorId);
            _sensorWalletCollection.Indexes.CreateOne(new CreateIndexModel<SensorWallet>(walletIndexKeys));

            var transferIndexKeys = Builders<TokenTransfer>.IndexKeys
                .Ascending(x => x.SensorId)
                .Descending(x => x.Timestamp);
            _tokenTransferCollection.Indexes.CreateOne(new CreateIndexModel<TokenTransfer>(transferIndexKeys));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create blockchain indexes");
        }
    }

    /// <summary>
    /// Register or get wallet for a sensor
    /// </summary>
    public async Task<SensorWallet> GetOrCreateSensorWalletAsync(int sensorId)
    {
        var existingWallet = await _sensorWalletCollection
            .Find(w => w.SensorId == sensorId)
            .FirstOrDefaultAsync();

        if (existingWallet != null)
        {
            await RegisterSensorOnBlockchainAsync(sensorId, existingWallet.WalletAddress);
            return existingWallet;
        }

        // Generate a new wallet address for the sensor
        var newWallet = new SensorWallet
        {
            SensorId = sensorId,
            WalletAddress = GenerateWalletAddress(sensorId),
            CreatedAt = DateTime.UtcNow,
            IsRegistered = false
        };

        await _sensorWalletCollection.InsertOneAsync(newWallet);
        _logger.LogInformation("Created wallet for sensor {SensorId}: {WalletAddress}", sensorId,
            newWallet.WalletAddress);


        await RegisterSensorOnBlockchainAsync(sensorId, newWallet.WalletAddress);
        newWallet.IsRegistered = true;
        await _sensorWalletCollection.ReplaceOneAsync(w => w.SensorId == sensorId, newWallet);

        return newWallet;
    }

    /// <summary>
    /// Generate a deterministic wallet address based on sensor ID
    /// </summary>
    private string GenerateWalletAddress(int sensorId)
    {
        // Generate a deterministic simulation-only address based on sensor ID.
        // Note: These are not valid Ethereum addresses and are only used for 
        // simulation/tracking purposes when blockchain is not fully configured.
        // When connected to a real blockchain, sensors should be registered with
        // actual Ethereum wallet addresses.
        var seed = $"sensor_{sensorId}_wallet_seed";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
        return "0x" + BitConverter.ToString(hash).Replace("-", "").Substring(0, 40).ToLowerInvariant();
    }

    /// <summary>
    /// Register a sensor on the blockchain
    /// </summary>
    public async Task<bool> RegisterSensorOnBlockchainAsync(int sensorId, string walletAddress)
    {
        try
        {
            var contract = _web3!.Eth.GetContractHandler(_config.ContractAddress);
            var registerFunction = new RegisterSensorFunction
            {
                SensorId = sensorId,
                WalletAddress = walletAddress
            };

            var receipt = await contract.SendRequestAndWaitForReceiptAsync(registerFunction);
            _logger.LogInformation("Registered sensor {SensorId} on blockchain. TX: {TxHash}",
                sensorId, receipt.TransactionHash);

            return receipt.Status.Value == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register sensor {SensorId} on blockchain", sensorId);
            return false;
        }
    }

    /// <summary>
    /// Reward a sensor with tokens for sending a message
    /// </summary>
    public async Task<TokenTransfer?> RewardSensorAsync(int sensorId)
    {
        var wallet = await GetOrCreateSensorWalletAsync(sensorId);

        var transfer = new TokenTransfer
        {
            SensorId = sensorId,
            WalletAddress = wallet.WalletAddress,
            Amount = _config.RewardAmount,
            Timestamp = DateTime.UtcNow,
            Status = "pending"
        };

        await _tokenTransferCollection.InsertOneAsync(transfer);

        try
        {
            var contract = _web3!.Eth.GetContractHandler(_config.ContractAddress);

            var rewardFunction = new RewardSensorFunction
            {
                SensorId = sensorId
            };

            var receipt = await contract.SendRequestAndWaitForReceiptAsync(rewardFunction);

            transfer.TransactionHash = receipt.TransactionHash;
            transfer.Status = receipt.Status.Value == 1 ? "completed" : "failed";
            await _tokenTransferCollection.ReplaceOneAsync(t => t.Id == transfer.Id, transfer);

            // Mark wallet as registered
            if (receipt.Status.Value == 1 && !wallet.IsRegistered)
            {
                wallet.IsRegistered = true;
                await _sensorWalletCollection.ReplaceOneAsync(w => w.SensorId == sensorId, wallet);
            }

            _logger.LogInformation("Rewarded sensor {SensorId} with tokens. TX: {TxHash}",
                sensorId, receipt.TransactionHash);

            return transfer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reward sensor {SensorId}", sensorId);
            transfer.Status = "failed";
            await _tokenTransferCollection.ReplaceOneAsync(t => t.Id == transfer.Id, transfer);
            return transfer;
        }
    }

    /// <summary>
    /// Get token balance for a sensor from blockchain
    /// </summary>
    public async Task<decimal> GetSensorBalanceFromBlockchainAsync(int sensorId)
    {
        try
        {
            var contract = _web3!.Eth.GetContractHandler(_config.ContractAddress);
            var balanceFunction = new GetSensorBalanceFunction
            {
                SensorId = sensorId
            };

            var balance = await contract.QueryAsync<GetSensorBalanceFunction, BigInteger>(balanceFunction);
            return Web3.Convert.FromWei(balance);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get balance for sensor {sensorId} from blockchain: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all sensor wallets with their balances
    /// </summary>
    public async Task<List<SensorTokenBalance>> GetAllSensorBalancesAsync()
    {
        var wallets = await _sensorWalletCollection.Find(_ => true).ToListAsync();
        var balances = new List<SensorTokenBalance>();

        foreach (var wallet in wallets)
        {
            var transfers = await _tokenTransferCollection
                .Find(t => t.SensorId == wallet.SensorId && (t.Status == "completed"))
                .ToListAsync();

            balances.Add(new SensorTokenBalance
            {
                SensorId = wallet.SensorId,
                WalletAddress = wallet.WalletAddress,
                Balance = transfers.Sum(t => t.Amount),
                MessageCount = transfers.Count
            });
        }

        return balances.OrderBy(b => b.SensorId).ToList();
    }

    /// <summary>
    /// Get recent token transfers
    /// </summary>
    public async Task<List<TokenTransfer>> GetRecentTransfersAsync(int limit = 50)
    {
        return await _tokenTransferCollection
            .Find(_ => true)
            .SortByDescending(t => t.Timestamp)
            .Limit(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Get transfers for a specific sensor
    /// </summary>
    public async Task<List<TokenTransfer>> GetSensorTransfersAsync(int sensorId, int limit = 50)
    {
        return await _tokenTransferCollection
            .Find(t => t.SensorId == sensorId)
            .SortByDescending(t => t.Timestamp)
            .Limit(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Get blockchain configuration status
    /// </summary>
    public BlockchainStatus GetStatus()
    {
        return new BlockchainStatus
        {
            RpcUrl = _config.RpcUrl,
            ContractAddress = _config.ContractAddress,
            OwnerAddress = _account?.Address ?? "",
            ChainId = _config.ChainId,
            RewardAmount = _config.RewardAmount
        };
    }
}

/// <summary>
/// Blockchain status information
/// </summary>
public class BlockchainStatus
{
    public string RpcUrl { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public string OwnerAddress { get; set; } = string.Empty;
    public int ChainId { get; set; }
    public decimal RewardAmount { get; set; }
}
