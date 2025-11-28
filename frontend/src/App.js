import './App.css';

import React, {useCallback, useEffect, useState} from 'react';
import {CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis} from 'recharts';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5001';

// Helper function to convert datetime-local format to ISO 8601
const toISOString = (dateTimeLocal) => {
  if (!dateTimeLocal) return '';
  // datetime-local format is YYYY-MM-DDTHH:MM, need to convert to ISO
  const date = new Date(dateTimeLocal);
  return date.toISOString();
};

function App() {
  const [sensorData, setSensorData] = useState([]);
  const [sensorTypes, setSensorTypes] = useState([]);
  const [sensorStats, setSensorStats] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [activeTab, setActiveTab] = useState('dashboard');
  const [filters, setFilters] = useState({
    sensorType: '',
    sensorId: '',
    startDate: '',
    endDate: '',
    sortBy: 'timestamp',
    sortDescending: true,
    page: 1,
    pageSize: 50
  });
  const [totalPages, setTotalPages] = useState(1);

  // Blockchain state
  const [tokenBalances, setTokenBalances] = useState([]);
  const [recentTransfers, setRecentTransfers] = useState([]);
  const [blockchainStatus, setBlockchainStatus] = useState(null);
  const [blockchainLoading, setBlockchainLoading] = useState(false);

  // Fetch sensor types on mount
  useEffect(() => {
    fetch(`${API_URL}/api/sensordata/types`)
        .then(res => res.json())
        .then(data => setSensorTypes(data))
        .catch(err => console.error('Failed to fetch sensor types:', err));


    // Fetch blockchain status
    fetch(`${API_URL}/api/blockchain/status`)
        .then(res => res.json())
        .then(data => setBlockchainStatus(data))
        .catch(err => console.error('Failed to fetch blockchain status:', err));
  }, []);

  // Fetch sensor stats for dashboard - with auto-refresh
  const fetchStats = useCallback(async () => {
    try {
      const response =
          await fetch(`${API_URL}/api/sensordata/stats?sampleCount=100`);
      const data = await response.json();
      setSensorStats(data);
    } catch (err) {
      console.error('Failed to fetch sensor stats:', err);
    }
  }, []);


  useEffect(() => {
    fetchStats();
    // Auto-refresh stats every 5 seconds
    const interval = setInterval(fetchStats, 5000);
    return () => clearInterval(interval);
  }, [fetchStats]);

  // Fetch sensor data when filters change
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const params = new URLSearchParams();
        if (filters.sensorType) params.append('sensorType', filters.sensorType);
        if (filters.sensorId) params.append('sensorId', filters.sensorId);
        // Convert datetime-local format to ISO 8601
        if (filters.startDate)
          params.append('startDate', toISOString(filters.startDate));
        if (filters.endDate)
          params.append('endDate', toISOString(filters.endDate));
        if (filters.sortBy) params.append('sortBy', filters.sortBy);
        params.append('sortDescending', filters.sortDescending);
        params.append('page', filters.page);
        params.append('pageSize', filters.pageSize);

        const response = await fetch(`${API_URL}/api/sensordata?${params}`);
        const result = await response.json();

        setSensorData(result.data || []);
        setTotalPages(result.totalPages || 1);
        setError(null);
      } catch (err) {
        setError(
            'Failed to fetch sensor data. Make sure the backend is running.');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
    // Auto-refresh data every 5 seconds
    const interval = setInterval(fetchData, 5000);
    return () => clearInterval(interval);
  }, [filters]);

  const handleFilterChange = (e) => {
    const {name, value, type, checked} = e.target;
    setFilters(prev => ({
                 ...prev,
                 [name]: type === 'checkbox' ? checked : value,
                 page: 1  // Reset to first page when filters change
               }));
  };

  const handleSort = (column) => {
    setFilters(
        prev => ({
          ...prev,
          sortBy: column,
          sortDescending: prev.sortBy === column ? !prev.sortDescending : true
        }));
  };

  const exportData = (format) => {
    const params = new URLSearchParams();
    if (filters.sensorType) params.append('sensorType', filters.sensorType);
    if (filters.sensorId) params.append('sensorId', filters.sensorId);
    // Convert datetime-local format to ISO 8601 for export
    if (filters.startDate)
      params.append('startDate', toISOString(filters.startDate));
    if (filters.endDate) params.append('endDate', toISOString(filters.endDate));
    if (filters.sortBy) params.append('sortBy', filters.sortBy);
    params.append('sortDescending', filters.sortDescending);

    window.open(
        `${API_URL}/api/sensordata/export/${format}?${params}`, '_blank');
  };

  // Prepare chart data from sensor data
  const chartData =
      sensorData
          .map(item => ({
                 timestamp: new Date(item.timestamp).toLocaleTimeString(),
                 value: item.value,
                 sensorId: `${item.sensorType}-${item.sensorId}`,
                 sensorType: item.sensorType
               }))
          .reverse();

  // Group stats by sensor type for dashboard
  const statsBySensorType = sensorStats.reduce((acc, stat) => {
    if (!acc[stat.sensorType]) acc[stat.sensorType] = [];
    acc[stat.sensorType].push(stat);
    return acc;
  }, {});

  // Fetch blockchain data when tab changes to tokens
  useEffect(() => {
    if (activeTab === 'tokens') {
      fetchBlockchainData();
    }
  }, [activeTab]);

  const fetchBlockchainData = async () => {
    setBlockchainLoading(true);
    try {
      const [balancesRes, transfersRes] = await Promise.all([
        fetch(`${API_URL}/api/blockchain/balances`),
        fetch(`${API_URL}/api/blockchain/transfers?limit=20`)
      ]);

      const balances = await balancesRes.json();
      const transfers = await transfersRes.json();

      setTokenBalances(balances);
      setRecentTransfers(transfers);
    } catch (err) {
      console.error('Failed to fetch blockchain data:', err);
    } finally {
      setBlockchainLoading(false);
    }
  };

  return (
    <div className='App'>
      <header className='App-header'>
        <h1>Sensor Data Dashboard</h1>
        <nav className='tab-nav'>
          <button 
            className={activeTab === 'dashboard' ? 'active' : ''} 
            onClick={() => setActiveTab('dashboard')}
          >
            Dashboard
          </button>
          <button
  className = {activeTab === 'data' ? 'active' : ''} onClick =
      {() => setActiveTab('data')} >
      Data Table<
          /button>
          <button 
            className={activeTab === 'charts' ? 'active' : ''} 
            onClick={() => setActiveTab('charts')}
          >
            Charts
          </button><
      button
            className={activeTab === 'tokens' ? 'active' : ''} 
            onClick={() => setActiveTab('tokens')}
          >
            Token Balances
          </button>
        </nav>
      </header>
      
      <main className='App-main'>
        {activeTab === 'dashboard' && (
          <section className='dashboard'>
            <h2>Real-time Sensor Dashboard</h2>
            <p className='auto-refresh-note'>Auto-refreshes every 5 seconds</p>
            {Object.keys(statsBySensorType).length === 0 ? (
              <p>No sensor data available. Start the sensors to begin collecting data.</p>
            ) : (
              Object.entries(statsBySensorType).map(([type, sensors]) => (
                <div key={type} className='sensor-type-group'>
                  <h3>{type}</h3>
                  <div className='sensor-cards'>
                    {sensors.map(stat => (
                      <div key={`${stat.sensorType}-${stat.sensorId}`} className='sensor-card'>
                        <div className='sensor-card-header'>
                          <span className='sensor-id'>Sensor {stat.sensorId}</span>
                          <span className='sensor-unit'>{stat.unit}</span>
                        </div>
                        <div className='sensor-card-body'>
                          <div className='sensor-stat'>
                            <span className='stat-label'>Last Value</span>
                            <span className='stat-value last-value'>{stat.lastValue.toFixed(2)}</span>
                          </div>
                          <div className='sensor-stat'>
                            <span className='stat-label'>Average (last {stat.sampleCount})</span>
                            <span className='stat-value avg-value'>{stat.averageValue.toFixed(2)}</span>
                          </div>
                        </div>
                        <div className='sensor-card-footer'>
                          <span className='last-update'>
                            Last: {new Date(stat.lastTimestamp).toLocaleString()}
                          </span>
                        </div>
                      </div>
                    ))
}
                  </div>
                </div>
              ))
            )
                  }
          </section>
        )}

        {activeTab === 'charts' && (
          <section className='charts'>
            <h2>Sensor Data Charts</h2>
            <div className='filters'>
              <div className='filter-row'>
                <label>
                  Sensor Type:
                  <select name='sensorType' value={filters.sensorType} onChange={handleFilterChange}>
                    <option value=''>All Types</option>
                    {sensorTypes.map(type => (
                      <option key={type} value={type}>{type}</option>
                    ))
          }
          </select>
                </label>

              <label>Sensor ID: < input
          type = 'number'
          name = 'sensorId'
                    value={filters.sensorId}
                    onChange={handleFilterChange}
                    placeholder='Any'
                  />
                </label>
                
                <label>
                  Start Date:
                  <input
                    type="datetime-local"
                    name="startDate"
                    value={filters.startDate}
                    onChange={handleFilterChange}
                  />
                </label>
                
                <label>
                  End Date:
                  <input
                    type="datetime-local"
                    name="endDate"
                    value={filters.endDate}
                    onChange={handleFilterChange}
                  />
                </label>
              </div>
            </div>
            {loading ? (
              <p>Loading chart data...</p>
            ) : chartData.length === 0 ? (
              <p>No data available for the selected filters.</p>
            ) : (
              <div className='chart-container'>
                <ResponsiveContainer width="100%" height={400}>
                  <LineChart data={chartData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey='timestamp' />
                    <YAxis />
                    <Tooltip />
                    <Legend />
                    <Line type='monotone' dataKey='value' stroke='#8884d8' dot={false} name='Sensor Value' />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            )}
          </section>
        )
                    }

                    {activeTab === 'data' && (
          <>
            <section className='filters'>
              <h2>Filters</h2>
              <div className="filter-row">
                <label>
                  Sensor Type:
                  <select name="sensorType" value={filters.sensorType} onChange={handleFilterChange}>
                    <option value="">All Types</option>
                    {sensorTypes.map(type => (
                      <option key={type} value={type}>{type}</option>
                    ))}
                  </select>
                </label>
                
                <label>
                  Sensor ID:
                  <input
                    type="number"
                    name="sensorId"
                    value={filters.sensorId}
                    onChange={handleFilterChange}
                    placeholder="Any"
                  />
                </label>
                
                <label>
                  Start Date:
                  <input
                    type="datetime-local"
                    name="startDate"
                    value={filters.startDate}
                    onChange={handleFilterChange}
                  />
                </label>
                
                <label>
                  End Date:
                  <input
                    type="datetime-local"
                    name="endDate"
                    value={filters.endDate}
                    onChange={handleFilterChange}
                  />
                </label>
              </div>
              
              <div className='export-buttons'>
                <button onClick={() => exportData('json')}>Export JSON</button>
                <button onClick={() => exportData('csv')}>Export CSV</button>
              </div>
            </section>

                <section className='data-table'>
                  <h2>Sensor Data</h2>
              <p className='auto-refresh-note'>Auto-refreshes every 5 seconds</p>
                  {
      loading ?
          (<p>Loading...<
              /p>
                  ) : error ? (
                    <p className='error'>{error}</p>) :
          sensorData.length === 0 ?
          (<p>No data available.Start the sensors to begin collecting data.<
               /p>
                  ) : (
                    <>
                      <table>
                        <thead>
                          <tr>
                            <th onClick={() => handleSort('sensorId')}>
                              Sensor ID {filters.sortBy === 'sensorId' && (filters.sortDescending ? '↓' : '↑')}
                            </th>
           <th onClick = {() => handleSort('sensorType')}>Type{
               filters.sortBy === 'sensorType' &&
               (filters.sortDescending ? '↓' : '↑')} <
           /th>
                            <th onClick={() => handleSort('value')}>
                              Value {filters.sortBy === 'value' && (filters.sortDescending ? '↓' : '↑')}
                            </th >
           <th>Unit<
               /th>
                            <th onClick={() => handleSort('timestamp')}>
                              Timestamp {filters.sortBy === 'timestamp' && (filters.sortDescending ? '↓' : '↑')}
                            </th>
           </tr>
                        </thead><tbody>{sensorData.map(
               (item, index) => (
                   <tr key = {item.id || index}><td>{item.sensorId} <
                   /td>
                              <td>{item.sensorType}</td >
                   <td>{(item.value ?? 0).toFixed(2)} <
                   /td>
                              <td>{item.unit}</td >
                   <td>{new Date(item.timestamp).toLocaleString()} <
                   /td>
                            </tr >))} <
           /tbody>
                      </table >

           <div className = 'pagination'>< button
      disabled = {filters.page <= 1} onClick =
          {() => setFilters(prev => ({...prev, page: prev.page - 1}))} >
          Previous<
              /button>
                        <span>Page {filters.page} of {totalPages}</span><
          button
                          disabled={filters.page >= totalPages}
                          onClick={() => setFilters(prev => ({ ...prev, page: prev.page + 1 }))}
                        >
                          Next
                        </button>
                      </div>
                    </>
                  )}
                </section>
          </>
        )}

        {activeTab === 'tokens' && (
          <>
            <section className='blockchain-status'>
              <h2>Blockchain Status</h2>
              {blockchainStatus && (
                <div className='status-info'>
                    <>
                      <p><strong>Network:</strong> Chain ID {blockchainStatus.chainId}</p>
                      <p><strong>Reward per Message:</strong> {blockchainStatus.rewardAmount} SENS</p>
                      {blockchainStatus.contractAddress && (
                        <p><strong>Contract:</strong> <code>{blockchainStatus.contractAddress}</code></p>
                      )}
                    </>
                </div>
              )}
              <button className='refresh-btn' onClick={fetchBlockchainData} disabled={blockchainLoading}>
                {blockchainLoading ? 'Loading...' : 'Refresh'}
              </button>
            </section>

            <section className='data-table'>
              <h2>Sensor Token Balances</h2>
              {blockchainLoading ? (
                <p>Loading...</p>
              ) : tokenBalances.length === 0 ? (
                <p>No sensor wallets found. Sensors will be registered when they send data.</p>
              ) : (
                <table>
                  <thead>
                    <tr>
                      <th>Sensor ID</th>
                      <th>Wallet Address</th>
                      <th>Token Balance (SENS)</th>
                      <th>Messages Sent</th>
                    </tr>
                  </thead>
                  <tbody>
                    {tokenBalances.map((item) => (
                      <tr key={item.sensorId}>
                        <td>{item.sensorId}</td>
                        <td><code>{item.walletAddress}</code></td>
                        <td>{item.balance.toFixed(2)}</td>
                        <td>{item.messageCount}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>

            <section className='data-table'>
              <h2>Recent Token Transfers</h2>
              {blockchainLoading ? (
                <p>Loading...</p>
              ) : recentTransfers.length === 0 ? (
                <p>No transfers yet.</p>
              ) : (
                <table>
                  <thead>
                    <tr>
                      <th>Sensor ID</th>
                      <th>Amount (SENS)</th>
                      <th>Status</th>
                      <th>Transaction Hash</th>
                      <th>Timestamp</th>
                    </tr>
                  </thead>
                  <tbody>
                    {recentTransfers.map((item, index) => (
                      <tr key={item.id || index}>
                        <td>{item.sensorId}</td>
                        <td>{item.amount.toFixed(2)}</td>
                        <td className={`status-${item.status}`}>{item.status}</td>
                        <td><code>{item.transactionHash && typeof item.transactionHash === 'string' ? item.transactionHash.substring(0, 16) + '...' : '-'}</code></td>
                        <td>{new Date(item.timestamp).toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>
          </>
        )}
      </main>
    </div>
  );
}

export default App;
