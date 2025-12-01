import json
import sys
import paho.mqtt.client as mqtt
import time

def send_control_command(command, sensor_id=None, value=None, broker="localhost", port=1883):
    client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
    try:
        client.connect(broker, port)
        
        message = {"command": command}
        if sensor_id is not None:
            message["sensorId"] = int(sensor_id)
        if value is not None:
            message["value"] = float(value)
        
        topic = "sensors/control"
        client.publish(topic, json.dumps(message))
        print(f"Sent command to {topic}: {message}")
        
        client.disconnect()
    except Exception as e:
        print(f"Error: {e}")

def print_usage():
    print("Usage:")
    print("  Start all:   python control_sensor.py start_all")
    print("  Stop all:    python control_sensor.py stop_all")
    print("  Start one:   python control_sensor.py start <sensor_id>")
    print("  Stop one:    python control_sensor.py stop <sensor_id>")
    print("  Set value:   python control_sensor.py set <sensor_id> <value>")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print_usage()
        sys.exit(1)
        
    cmd = sys.argv[1]
    broker = "localhost"
    port = 1883
    
    # Handle arguments
    if cmd in ["start_all", "stop_all"]:
        send_control_command(cmd)
    elif cmd in ["start", "stop"]:
        if len(sys.argv) < 3:
            print(f"Error: {cmd} requires a sensor ID")
            sys.exit(1)
        send_control_command(cmd, sensor_id=sys.argv[2])
    elif cmd == "set":
        if len(sys.argv) < 4:
            print("Error: set requires sensor ID and value")
            sys.exit(1)
        send_control_command("set", sensor_id=sys.argv[2], value=sys.argv[3])
    # Legacy support: python control_sensor.py <id> <value>
    elif cmd.isdigit():
        if len(sys.argv) < 3:
            print_usage()
            sys.exit(1)
        send_control_command("set", sensor_id=sys.argv[1], value=sys.argv[2])
    else:
        print_usage()
