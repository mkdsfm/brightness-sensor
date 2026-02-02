// Arduino IDE sketch version (without PlatformIO)

constexpr int kLightSensorPin = 1;               // GPIO1 (ADC)
constexpr unsigned long kReadIntervalMs = 500;   // period between measurements
constexpr const char *kDeviceId = "esp32c3-01";
constexpr const char *kSensorId = "light0";

unsigned long lastReadAtMs = 0;

void setup() {
  Serial.begin(115200);
  analogReadResolution(12); // 0..4095
  delay(1000);
}

void loop() {
  const unsigned long now = millis();
  if (now - lastReadAtMs < kReadIntervalMs) {
    return;
  }

  lastReadAtMs = now;
  const int rawValue = analogRead(kLightSensorPin);

  Serial.printf(
      "{\"deviceId\":\"%s\",\"sensorId\":\"%s\",\"ts\":%lu,\"value\":%d}\\n",
      kDeviceId,
      kSensorId,
      now,
      rawValue);
}
