FROM python:3.11-slim

WORKDIR /app

# Copy requirements and install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

# Set auto-start mode for Docker deployment
ENV AUTO_START=false
ENV MQTT_BROKER=mosquitto
ENV MQTT_PORT=1883

CMD ["python", "-u", "main.py"]
