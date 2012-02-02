//use 1M Ω resistor, parallel to vibratorSensor
//use 10K Ω resistor attached to ground and force sensor and force sensor also to 5V

const int vibrationSensor = A0;
int sensorReading = 0;

void setup() {
  Serial.begin(115200);
}

void loop(){
  sensorReading = analogRead(vibrationSensor);
  if (sensorReading > 0)
  {
    Serial.println(sensorReading);
  }
  delay(100);
}
