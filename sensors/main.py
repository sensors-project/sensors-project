import time
import os
import sys
from sensor import *

# Get MQTT broker configuration from environment variables
mqtt_broker = os.environ.get('MQTT_BROKER', 'localhost')
mqtt_port = int(os.environ.get('MQTT_PORT', '1883'))

print(f"Initializing sensors (connecting to MQTT broker at {mqtt_broker}:{mqtt_port})")

# Create 4 sensors of each type (16 sensors total as per requirement 10)
temperatureSensors = [TemperatureSensor(broker=mqtt_broker, port=mqtt_port) for i in range(4)]
pressureSensors = [PressureSensor(broker=mqtt_broker, port=mqtt_port) for i in range(4)]
co2Sensors = [Co2Sensor(broker=mqtt_broker, port=mqtt_port) for i in range(4)]
oxygenSensors = [DissolvedOxygenSensor(broker=mqtt_broker, port=mqtt_port) for i in range(4)]

all_sensors = temperatureSensors + pressureSensors + co2Sensors + oxygenSensors

print(f"Initialized {len(all_sensors)} sensors")

# Always connect sensors to MQTT so they can receive commands
print("Connecting sensors to MQTT...")
for sensor in all_sensors:
    sensor.connect()

print("Sensors initialized in standby mode. Waiting for MQTT commands...")

# Keep the main thread alive
try:
    while True:
        time.sleep(1)
except KeyboardInterrupt:
    print("Exiting program...")
    for sensor in all_sensors:
        sensor.stop()
