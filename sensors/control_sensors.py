import json
import sys
import paho.mqtt.client as mqtt
import time

def send_control_command(command, sensor_id=None, broker="localhost", port=1883, **kwargs):
    client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
    try:
        client.connect(broker, port)
        
        message = {"command": command}
        if sensor_id is not None:
            message["sensorId"] = int(sensor_id)
        
        for k, v in kwargs.items():
            try:
                message[k] = float(v)
            except (ValueError, TypeError):
                message[k] = v
        
        topic = "sensors/control"
        client.publish(topic, json.dumps(message))
        print(f"Sent command to {topic}: {message}")
        
        client.disconnect()
    except Exception as e:
        print(f"Error: {e}")

def print_usage():
    print("Usage:")
    print("  Start all:      python control_sensor.py start_all")
    print("  Stop all:       python control_sensor.py stop_all")
    print("  Start one:      python control_sensor.py start <id>")
    print("  Stop one:       python control_sensor.py stop <id>")
    print("  Set Value:      python control_sensor.py set <id> <value>")
    print("  Set Range:      python control_sensor.py range <id> <min> <max>")
    print("  Set Rate:       python control_sensor.py rate <id> <msgs_per_min>")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print_usage()
        sys.exit(1)
        
    cmd = sys.argv[1]
    broker = "localhost"
    port = 1883
    
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
        send_control_command("set_value", sensor_id=sys.argv[2], value=sys.argv[3])

    elif cmd == "range":
        if len(sys.argv) < 5:
            print("Error: range requires sensor ID, min, and max")
            print("Example: python control_sensor.py range 1 -10 10")
            sys.exit(1)
        send_control_command("set_range", sensor_id=sys.argv[2], min=sys.argv[3], max=sys.argv[4])

    elif cmd == "rate":
        if len(sys.argv) < 4:
            print("Error: rate requires sensor ID and messages_per_minute")
            print("Example: python control_sensor.py rate 1 60")
            sys.exit(1)
        send_control_command("set_rate", sensor_id=sys.argv[2], rate=sys.argv[3])

    else:
        print(f"Unknown command: {cmd}")
        print_usage()