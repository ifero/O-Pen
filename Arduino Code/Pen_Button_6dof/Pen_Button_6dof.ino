#include <Wire.h>
#include <SixDOF.h>

// Button 
int buttonPin = 3;
int buttonValue = 0;

// Raw data from Accelerometer and Gyroscope
int acc[3];
int gyro[4];

// Processed data from Accelerometer and Gyroscope
float RwAcc[3];  //projection of normalized gravitation force vector on x/y/z axis, as measured by accelerometer
float RwGyro[3];

// The Gyroscope offsets are chip specific. 
int g_offx = 120;
int g_offy = 20;
int g_offz = 93;

// Initialzing the pen sensor board
SixDOF pen(g_offx, g_offy, g_offz);


void setup()
{
  Serial.begin(19200);
  pinMode(buttonPin, INPUT);
}

void loop()
{
  getPenData();
  sendPenData();
  delay(300);
}


void getPenData(){
  buttonValue = digitalRead(buttonPin);
  if(!Serial.available()) 
  {
    pen.getAccelerometerData(acc);
    pen.rawAccToG(acc, RwAcc);
    pen.normalize3DVec(RwAcc);
    
    pen.getGyroscopeData(gyro);
    pen.rawGyroToDegsec(gyro, RwGyro);
  }
}


void sendPenData(){
    Serial.print(buttonValue); Serial.print(";");
    Serial.print(RwAcc[0]);    Serial.print(";");
    Serial.print(RwAcc[1]);    Serial.print(";");
    Serial.print(RwAcc[2]);    Serial.print(";");
    Serial.print(RwGyro[0]);   Serial.print(";");
    Serial.print(RwGyro[1]);   Serial.print(";");
    Serial.print(RwGyro[2]);   Serial.println();
}
