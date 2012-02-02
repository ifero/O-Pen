//use 1M Ω resistor, parallel to vibratorSensor
//use 10K Ω resistor attached to ground and force sensor and force sensor also to 5V

const int vibrationSensor = A0;
const int threshold = 255;
int sensorReading = 0;

void setup() {
  pinMode(9,OUTPUT);
  Serial.begin(115200);
}

void loop(){
  sensorReading = analogRead(vibrationSensor);
  if (sensorReading > 0)
  {
    Serial.println(sensorReading);
  }
  if(sensorReading > threshold)
  {
    analogWrite(9,threshold);
  }
  else
  {
    analogWrite(9,sensorReading);
  }
  delay(30);
}
