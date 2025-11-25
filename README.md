# Sensor Data Monitoring System

A complete sensor data monitoring system built with ASP.NET Core 8.0, React, MongoDB, and MQTT.

## System Requirements

- Docker and Docker Compose
- (For local development) .NET 8.0 SDK, Node.js 18+, Python 3.11+

## Quick Start

1. Clone the repository:
```bash
git clone https://github.com/sensors-project/sensors.git
cd sensors
```

2. Start all services with Docker Compose:
```bash
docker compose up -d
```

3. Access the applications:
   - **Frontend**: http://localhost:3000
   - **Backend API**: http://localhost:5001
   - **Swagger API Docs**: http://localhost:5001/swagger
   - **MongoDB**: localhost:27017
   - **MQTT Broker**: localhost:1883

## Architecture

The system consists of the following components:

### 1. MQTT Broker (Mosquitto)
- Eclipse Mosquitto 2.x
- Ports: 1883 (TCP), 9001 (WebSocket)
- Handles all sensor data communication

### 2. MongoDB Database
- MongoDB 6.0
- Stores all sensor readings
- Configured with authentication (root/example)

### 3. Backend (ASP.NET Core 8.0)
- REST API for sensor data
- Automatic MQTT subscription and data ingestion
- Data filtering, sorting, and export (JSON/CSV)
- Swagger/OpenAPI documentation

### 4. Frontend (React)
- Real-time sensor data display
- Filtering by date, sensor type, and sensor ID
- Sortable data table
- Data export functionality

### 5. Sensor Simulator (Python)
- Simulates 16 sensors (4 of each type):
  - Temperature sensors
  - Pressure sensors
  - CO2 sensors
  - Dissolved oxygen sensors
- Publishes data to MQTT broker

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/sensordata | Get sensor data with filtering and pagination |
| GET | /api/sensordata/types | Get available sensor types |
| GET | /api/sensordata/sensors | Get sensor IDs |
| GET | /api/sensordata/export/json | Export data as JSON |
| GET | /api/sensordata/export/csv | Export data as CSV |
| GET | /health | Health check endpoint |

### Query Parameters

- `startDate` - Filter by start date (ISO 8601)
- `endDate` - Filter by end date (ISO 8601)
- `sensorType` - Filter by sensor type (TEMPERATURE, PRESSURE, CO2, DISSOLVED_OXYGEN)
- `sensorId` - Filter by sensor ID
- `sortBy` - Sort field (timestamp, sensorId, sensorType, value)
- `sortDescending` - Sort direction (true/false)
- `page` - Page number (default: 1)
- `pageSize` - Results per page (default: 100, max: 1000)

## Development

### Backend
```bash
cd backend
dotnet restore
dotnet run
```

### Frontend
```bash
cd frontend
npm install
npm start
```

### Sensors Simulator
```bash
cd sensors
pip install -r requirements.txt
python main.py
```

## Configuration

### Environment Variables

#### Backend
- `ASPNETCORE_URLS` - Server URLs (default: http://+:5001)
- `MONGODB_CONNECTION_STRING` - MongoDB connection string
- `MONGODB_DATABASE` - Database name (default: sensorsdb)
- `MQTT_BROKER` - MQTT broker host (default: localhost)
- `MQTT_PORT` - MQTT broker port (default: 1883)

#### Frontend
- `REACT_APP_API_URL` - Backend API URL (default: http://localhost:5001)

#### Sensors
- `MQTT_BROKER` - MQTT broker host (default: localhost)
- `MQTT_PORT` - MQTT broker port (default: 1883)
- `AUTO_START` - Auto-start sensors (default: false)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
