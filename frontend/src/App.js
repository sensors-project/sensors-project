import './App.css';

import React, {useEffect, useState} from 'react';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5001';

function App() {
  const [sensorData, setSensorData] = useState([]);
  const [sensorTypes, setSensorTypes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
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

  // Fetch sensor types on mount
  useEffect(() => {
    fetch(`${API_URL}/api/sensordata/types`)
        .then(res => res.json())
        .then(data => setSensorTypes(data))
        .catch(err => console.error('Failed to fetch sensor types:', err));
  }, []);

  // Fetch sensor data when filters change
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const params = new URLSearchParams();
        if (filters.sensorType) params.append('sensorType', filters.sensorType);
        if (filters.sensorId) params.append('sensorId', filters.sensorId);
        if (filters.startDate) params.append('startDate', filters.startDate);
        if (filters.endDate) params.append('endDate', filters.endDate);
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
    if (filters.startDate) params.append('startDate', filters.startDate);
    if (filters.endDate) params.append('endDate', filters.endDate);
    if (filters.sortBy) params.append('sortBy', filters.sortBy);
    params.append('sortDescending', filters.sortDescending);

    window.open(
        `${API_URL}/api/sensordata/export/${format}?${params}`, '_blank');
  };

  return (
    <div className='App'>
      <header className='App-header'>
        <h1>Sensor Data Dashboard</h1>
      </header>
      
      <main className='App-main'>
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
          {loading ? (
            <p>Loading...</p>
          ) : error ? (
            <p className='error'>{error}</p>
          ) : sensorData.length === 0 ? (
            <p>No data available. Start the sensors to begin collecting data.</p>
          ) : (
            <>
              <table>
                <thead>
                  <tr>
                    <th onClick={() => handleSort('sensorId')}>
                      Sensor ID {filters.sortBy === 'sensorId' && (filters.sortDescending ? '↓' : '↑')}
                    </th>
                    <th onClick={() => handleSort('sensorType')}>
                      Type {filters.sortBy === 'sensorType' && (filters.sortDescending ? '↓' : '↑')}
                    </th>
                    <th onClick={() => handleSort('value')}>
                      Value {filters.sortBy === 'value' && (filters.sortDescending ? '↓' : '↑')}
                    </th>
                    <th>Unit</th>
                    <th onClick={() => handleSort('timestamp')}>
                      Timestamp {filters.sortBy === 'timestamp' && (filters.sortDescending ? '↓' : '↑')}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {sensorData.map((item, index) => (
                    <tr key={item.id || index}>
                      <td>{item.sensorId}</td>
                      <td>{item.sensorType}</td>
                      <td>{(item.value ?? 0).toFixed(2)}</td>
                      <td>{item.unit}</td>
                      <td>{new Date(item.timestamp).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              
              <div className="pagination">
                <button 
                  disabled={filters.page <= 1} 
                  onClick={() => setFilters(prev => ({ ...prev, page: prev.page - 1 }))}
                >
                  Previous
                </button>
                <span>Page {filters.page} of {totalPages}</span>
                <button 
                  disabled={filters.page >= totalPages}
                  onClick={() => setFilters(prev => ({ ...prev, page: prev.page + 1 }))}
                >
                  Next
                </button>
              </div>
            </>
          )}
        </section>
      </main>
    </div>
  );
}

export default App;
