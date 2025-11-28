using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models;

/// <summary>
/// Represents a sensor wallet mapping
/// </summary>
public class SensorWallet
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("sensorId")]
    public int SensorId { get; set; }

    [BsonElement("walletAddress")]
    public string WalletAddress { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isRegistered")]
    public bool IsRegistered { get; set; }
}

/// <summary>
/// Represents a token transfer event
/// </summary>
public class TokenTransfer
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("sensorId")]
    public int SensorId { get; set; }

    [BsonElement("walletAddress")]
    public string WalletAddress { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("transactionHash")]
    public string TransactionHash { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("status")]
    public string Status { get; set; } = "pending";
}

/// <summary>
/// DTO for sensor token balance
/// </summary>
public class SensorTokenBalance
{
    public int SensorId { get; set; }
    public string WalletAddress { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public int MessageCount { get; set; }
}

/// <summary>
/// Blockchain configuration
/// </summary>
public class BlockchainConfig
{
    public string RpcUrl { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public string OwnerPrivateKey { get; set; } = string.Empty;
    public int ChainId { get; set; }
    public decimal RewardAmount { get; set; } = 1.0m;
}
