import threading
import time
import os
import sys
from sensor import *

def input_thread():
    global programIsRunning
    while programIsRunning:
        try:
            key = int(input())
            if key == 1:
                for sensor in all_sensors:
                    sensor.start()
                print("Sensors started")
            elif key == 2:
                for sensor in all_sensors:
                    sensor.stop()
                print("Sensors stopped")
            elif key == 3:
                for sensor in all_sensors:
                    sensor.stop()
                programIsRunning = False
                print("Exiting program...")
            elif key == 4:
                print("Select a sensor (-1 to cancel):")
                for i, sensor in enumerate(all_sensors):
                    print(f"{i}: {sensor.getName()}")
                try:
                    choice = int(input())
                    if choice == -1:
                        print("Cancelled")
                    elif 0 <= choice < len(all_sensors):
                        value = input(f"Enter value to generate for {all_sensors[choice].getName()}: ")
                        all_sensors[choice].generateValue(value)
                    else:
                        print("Invalid number")
                except ValueError:
                    print("Please enter a valid number.")
        except ValueError:
            print("Please enter a valid number.")
        except EOFError:
            # Handle non-interactive mode (Docker)
            break

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
print("Press 1 to start sensors")
print("Press 2 to stop sensors")
print("Press 3 to end program")
print("Press 4 to produce specified data by specified sensor")

programIsRunning = True

# Check if running in auto-start mode (for Docker)
auto_start = os.environ.get('AUTO_START', 'false').lower() == 'true'

if auto_start:
    print("Auto-start mode enabled, starting all sensors...")
    for sensor in all_sensors:
        sensor.start()
    print("All sensors started")
else:
    threading.Thread(target=input_thread, daemon=True).start()

while programIsRunning:
    time.sleep(0.5)
