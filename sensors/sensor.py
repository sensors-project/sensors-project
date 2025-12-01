import time
import threading
import math
import random
import json
import paho.mqtt.client as mqtt
from abc import ABC, abstractmethod
from datetime import datetime

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
            target_value = payload.get("value")
            
            # Handle specific sensor commands
            if target_id == self._id:
                if command == "start":
                    print(f"Sensor {self._id} starting...")
                    self.start()
                elif command == "stop":
                    print(f"Sensor {self._id} stopping...")
                    self.stop()
                elif target_value is not None:
                    print(f"Sensor {self._id} received control command: {target_value}")
                    self.generateValue(target_value)
            
            # Handle global commands
            elif command == "start_all":
                self.start()
            elif command == "stop_all":
                self.stop()
                
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
        self._thread.start()

    def stop(self):
        if not self._isRunning:
            return
            
        self._isRunning = False
        if hasattr(self, "_thread") and self._thread.is_alive():
            self._thread.join()

    def generateValue(self, value):
        message = self._createMessage(float(value))
        self._client.publish(self._topic, json.dumps(message))
        print(f"published: {message} to topic: {self._topic}")

    def getName(self):
        return f"{self._sensorType} no.: {self._id}"

    def _createMessage(self, value):
        return {
            "sensorId": self._id,
            "sensorType": self._sensorType,
            "value": value,
            "timestamp": datetime.utcnow().isoformat() + "Z",
            "unit": self._unit
        }

    def _produceData(self):
        while self._isRunning:
            value = self._productionFunction(time.time())
            message = self._createMessage(value)
            self._client.publish(self._topic, json.dumps(message))
            print(f"published: {message} to topic: {self._topic}")
            
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
        # Simulate temperature between 15-30°C with some variation
        return round(22.5 + 7.5 * math.sin(t / 100) + random.uniform(-2, 2), 2)


class PressureSensor(Sensor):
    def __init__(self, sleepLowerBound=1, sleepUpperBound=10, broker="localhost", port=1883):
        super().__init__(sleepLowerBound, sleepUpperBound, broker, port)
        self._topic = "sensors/pressure"
        self._sensorType = "PRESSURE"
        self._unit = "hPa"
        
    def _productionFunction(self, t):
        # Simulate atmospheric pressure around 1013 hPa
        return round(1013 + 20 * math.cos(t / 200) + random.uniform(-5, 5), 2)


class Co2Sensor(Sensor):
    def __init__(self, sleepLowerBound=1, sleepUpperBound=10, broker="localhost", port=1883):
        super().__init__(sleepLowerBound, sleepUpperBound, broker, port)
        self._topic = "sensors/co2"
        self._sensorType = "CO2"
        self._unit = "ppm"
        
    def _productionFunction(self, t):
        # Simulate CO2 levels between 400-600 ppm
        return round(500 + 100 * math.sin(t / 150) + random.uniform(-20, 20), 2)


class DissolvedOxygenSensor(Sensor):
    def __init__(self, sleepLowerBound=1, sleepUpperBound=10, broker="localhost", port=1883):
        super().__init__(sleepLowerBound, sleepUpperBound, broker, port)
        self._topic = "sensors/oxygen"
        self._sensorType = "DISSOLVED_OXYGEN"
        self._unit = "mg/L"
        
    def _productionFunction(self, t):
        # Simulate dissolved oxygen between 6-10 mg/L
        return round(8 + 2 * math.cos(t / 120) + random.uniform(-0.5, 0.5), 2)
