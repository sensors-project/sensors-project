import time
import threading
import math
import random
import json
import paho.mqtt.client as mqtt
from abc import ABC, abstractmethod
from datetime import datetime, timezone

class Sensor(ABC):
    _id_counter = 0

    def __init__(self, sleepLowerBound=0.1, sleepUpperBound=1, broker="localhost", port=1883):
        self._sleepLowerBound = sleepLowerBound
        self._sleepUpperBound = sleepUpperBound
        Sensor._id_counter += 1
        self._id = Sensor._id_counter
        self._client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
        self._broker = broker
        self._port = port
        self._topic = "sensors/data"
        self._sensorType = "generic"
        self._isRunning = False
        
        self._range_min = None
        self._range_max = None

    def connect(self):
        return self._connect()

    def _connect(self):
        try:
            if self._client.is_connected():
                return True
            self._client.on_message = self._on_message
            self._client.connect(self._broker, self._port)
            self._client.loop_start()
            self._client.subscribe("sensors/control")
            return True
        except Exception as e:
            print(f"Failed to connect to MQTT broker: {e}")
            return False
        
    def __del__(self):
        try:
            self._client.loop_stop()
            self._client.disconnect()
        except:
            pass

    def _on_message(self, client, userdata, message):
        try:
            payload = json.loads(message.payload.decode("utf-8"))
            command = payload.get("command")
            target_id = payload.get("sensorId")
            
            if command == "start_all":
                self.start()
            elif command == "stop_all":
                self.stop()

            elif target_id == self._id:
                if command == "start":
                    print(f"Sensor {self._id} starting...")
                    self.start()
                elif command == "stop":
                    print(f"Sensor {self._id} stopping...")
                    self.stop()
                
                elif command == "set_range":
                    r_min = payload.get("min")
                    r_max = payload.get("max")
                    if r_min is not None and r_max is not None:
                        if r_min < r_max:
                            self._range_min = float(r_min)
                            self._range_max = float(r_max)
                            print(f"Sensor {self._id} range updated: [{self._range_min}, {self._range_max}]")
                        else:
                            print(f"Sensor {self._id} Error: Min must be less than Max")

                elif command == "set_rate":
                    rate = payload.get("rate") 
                    if rate is not None and float(rate) > 0:
                        target_interval = 60.0 / float(rate)
                        self._sleepLowerBound = target_interval * 0.9
                        self._sleepUpperBound = target_interval * 1.1
                        print(f"Sensor {self._id} rate updated: {rate} msg/min (Interval ~{target_interval:.2f}s)")
                    else:
                        print(f"Sensor {self._id} Error: Rate must be > 0")

                elif payload.get("value") is not None:
                    target_value = payload.get("value")
                    print(f"Sensor {self._id} received control command: {target_value}")
                    self.generateValue(target_value)
            
        except Exception as e:
            print(f"Error processing control message: {e}")

    def start(self):
        if self._isRunning:
            return
            
        if not self._client.is_connected():
            if not self._connect():
                print(f"Cannot start sensor {self._id} - MQTT connection failed")
                return

        self._isRunning = True
        self._thread = threading.Thread(target=self._produceData)
        self._thread.daemon = True
        self._thread.start()

    def stop(self):
        if not self._isRunning:
            return
            
        self._isRunning = False
        if hasattr(self, "_thread") and self._thread.is_alive():
            self._thread.join()

    def generateValue(self, value):
        final_val = self._clamp(float(value))
        message = self._createMessage(final_val)
        self._client.publish(self._topic, json.dumps(message))
        print(f"published: {message} to topic: {self._topic}")

    def getName(self):
        return f"{self._sensorType} no.: {self._id}"

    def _createMessage(self, value):
        return {
            "sensorId": self._id,
            "sensorType": self._sensorType,
            "value": value,
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "unit": self._unit
        }

    def _clamp(self, value):
        if self._range_min is None or self._range_max is None:
            return value
        return max(self._range_min, min(value, self._range_max))

    def _scale_to_range(self, normalized_signal):
        if self._range_min is not None and self._range_max is not None:
            target_span = self._range_max - self._range_min
            scaled_value = self._range_min + (normalized_signal * target_span)
            return self._clamp(scaled_value)
        return None

    def _produceData(self):
        while self._isRunning:
            value = self._productionFunction(time.time())
            message = self._createMessage(value)
            self._client.publish(self._topic, json.dumps(message))
            print(f"[{self._sensorType}-{self._id}] {value} {self._unit}")
            
            time.sleep(random.uniform(self._sleepLowerBound, self._sleepUpperBound))

    @abstractmethod
    def _productionFunction(self, t):
        pass


class TemperatureSensor(Sensor):
    def __init__(self, sleepLowerBound=1, sleepUpperBound=10, broker="localhost", port=1883):
        super().__init__(sleepLowerBound, sleepUpperBound, broker, port)
        self._topic = "sensors/temperature"
        self._sensorType = "TEMPERATURE"
        self._unit = "°C"
        
    def _productionFunction(self, t):
        raw_sin = math.sin(t / 10)
        normalized_signal = (raw_sin + 1) / 2 
        normalized_signal += random.uniform(-0.05, 0.05)
        
        custom_val = self._scale_to_range(normalized_signal)
        if custom_val is not None:
            return round(custom_val, 2)

        return round(22.5 + 7.5 * raw_sin + random.uniform(-2, 2), 2)


class PressureSensor(Sensor):
    def __init__(self, sleepLowerBound=1, sleepUpperBound=10, broker="localhost", port=1883):
        super().__init__(sleepLowerBound, sleepUpperBound, broker, port)
        self._topic = "sensors/pressure"
        self._sensorType = "PRESSURE"
        self._unit = "hPa"
        
    def _productionFunction(self, t):
        raw_cos = math.cos(t / 20)
        normalized_signal = (raw_cos + 1) / 2
        normalized_signal += random.uniform(-0.02, 0.02)

        custom_val = self._scale_to_range(normalized_signal)
        if custom_val is not None:
            return round(custom_val, 2)

        return round(1013 + 20 * raw_cos + random.uniform(-5, 5), 2)


class Co2Sensor(Sensor):
    def __init__(self, sleepLowerBound=1, sleepUpperBound=10, broker="localhost", port=1883):
        super().__init__(sleepLowerBound, sleepUpperBound, broker, port)
        self._topic = "sensors/co2"
        self._sensorType = "CO2"
        self._unit = "ppm"
        
    def _productionFunction(self, t):
        raw_sin = math.sin(t / 15)
        normalized_signal = (raw_sin + 1) / 2
        normalized_signal += random.uniform(-0.05, 0.05)

        custom_val = self._scale_to_range(normalized_signal)
        if custom_val is not None:
            return round(custom_val, 2)

        return round(500 + 100 * raw_sin + random.uniform(-20, 20), 2)


class DissolvedOxygenSensor(Sensor):
    def __init__(self, sleepLowerBound=1, sleepUpperBound=10, broker="localhost", port=1883):
        super().__init__(sleepLowerBound, sleepUpperBound, broker, port)
        self._topic = "sensors/oxygen"
        self._sensorType = "DISSOLVED_OXYGEN"
        self._unit = "mg/L"
        
    def _productionFunction(self, t):
        raw_cos = math.cos(t / 12)
        normalized_signal = (raw_cos + 1) / 2
        normalized_signal += random.uniform(-0.05, 0.05)

        custom_val = self._scale_to_range(normalized_signal)
        if custom_val is not None:
            return round(custom_val, 2)

        return round(8 + 2 * raw_cos + random.uniform(-0.5, 0.5), 2)