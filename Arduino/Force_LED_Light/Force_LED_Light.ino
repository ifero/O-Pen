//use 1M Ω resistor, parallel to vibratorSensor
//use 10K Ω resistor attached to ground and force sensor and force sensor also to 5V

const int vibrationSensor = A0;
const int lightSensor = A1;
const int threshold = 255;
int sensorReading = 0;
int lightReading = 0


void setup() {
  pinMode(9,OUTPUT);
  Serial.begin(115200);
}

void loop(){
  lightReading = analogRead(lightSensor);
  sensorReading = analogRead(vibrationSensor);
  if (sensorReading > 0)
  {
    Serial.print("Force: ");
    Serial.print(sensorReading);
    Serial.print(" Light: ");
    Serial.print(lightSensor);
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
