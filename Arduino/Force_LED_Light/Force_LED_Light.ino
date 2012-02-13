//use 1M Ω resistor, parallel to vibratorSensor
//use 10K Ω resistor attached to ground and force sensor and force sensor also to 5V

const int vibrationSensor = A1;
const int lightSensor = A0;
const int threshold = 255;
int sensorReading = 0;
int lightReading = 0;


void setup() {
  pinMode(9,OUTPUT);
  Serial.begin(115200);
}

void loop(){
  lightReading = analogRead(lightSensor);
  sensorReading = analogRead(vibrationSensor);
  if (sensorReading > 0)
  {
    Serial.print(sensorReading);
    Serial.print(".");
    Serial.println(lightReading);
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
