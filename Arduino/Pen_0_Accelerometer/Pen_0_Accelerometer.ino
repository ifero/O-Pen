#include <Wire.h> //I2C library

//Accelerometer ADXL345 parameters
#define ACC 0x53  //ADXL address
#define ACC_POWER_CTL 0x2D
#define ACC_REG_ADDRESS 0x32 //first axis-acceleration-data register on the ADXL245
#define ACC_TO_READ 6 //number of bytes to be read each time (2 bytes each axis)

boolean firstSample = true;

float RwAcc[3];  //projection of normalized gravitation force vector on x/y/z axis, as measured by accelerometer

//initializes the Accelerometer
 /*****************************************
  * ADXL345
  * WAKE_UP set, with 8Hz frequency
  * AUTO_SLEEP set and MEASURE set.
  ******************************************/
void initAccelerometer() {
  byte val;
  Wire.beginTransmission(ACC); //start transmission to ACC 
  Wire.write(ACC_POWER_CTL);        // send register address
  val = 0;
  Wire.write(val); 
  Wire.endTransmission();
  Wire.beginTransmission(ACC); //start transmission to ACC 
  Wire.write(ACC_POWER_CTL);        // send register address
  val = 8;
  Wire.write(val);    
  Wire.endTransmission();
}

/**************************************
  Accelerometer ADXL345 I2C
  registers:
  x axis MSB = 32, x axis LSB = 33
  y axis MSB = 34, y axis LSB = 35
  z axis MSB = 36, z axis LSB = 37
  *************************************/
void getAccelerometerData(int *result) {
  byte buff[ACC_TO_READ];
  Wire.beginTransmission(ACC); //start transmission to ACC 
  Wire.write(ACC_REG_ADDRESS);        //sends address to read from
  Wire.endTransmission(); //end transmission
  
  Wire.beginTransmission(ACC); //start transmission to ACC
  Wire.requestFrom(ACC, ACC_TO_READ);    // request 6 bytes from ACC
  
  int i = 0;
  while(Wire.available())    //ACC may send less than requested (abnormal)
  { 
    buff[i] = Wire.read(); // receive a byte
    i++;
  }
  Wire.endTransmission(); //end transmission
  result[0] = (( buff[1] << 8) | buff[0]);
  result[1] = (( buff[3] << 8) | buff[2]);
  result[2] = (( buff[5] << 8) | buff[4]);
}

void rawAccToG(int *raw, float * RwAcc) {
  RwAcc[0] = ( (float) raw[0]) / 256.0;
  RwAcc[1] = ( (float) raw[1]) / 256.0;
  RwAcc[2] = ( (float) raw[2]) / 256.0;
}
  
void normalize3DVec(float * vector) {
  float R;
  R = sqrt(vector[0]*vector[0] + vector[1]*vector[1] + vector[2]*vector[2]);
  vector[0] /= R;
  vector[1] /= R;  
  vector[2] /= R;  
}

void setup()
{
  Serial.begin(19200);
  Wire.begin();
  initAccelerometer();
}

void loop()
{
  if(!Serial.available()) 
  {
    int acc[3];
    
    getAccelerometerData(acc);
    rawAccToG(acc, RwAcc);
    normalize3DVec(RwAcc);
    Serial.print("0."); // Type of Pen
    if (((RwAcc[0] <= 0.75) && (RwAcc[1] >= 0.65)) || ((RwAcc[0] <= 0.6) && (RwAcc[1] <= -0.6))
       || ((RwAcc[0] <= 0.7) && (RwAcc[2] <= -0.4 || RwAcc[2] >= 0.7)))
       Serial.print("1");
    else
      Serial.print("0");
    Serial.println();
    delay(300);
  }
}

