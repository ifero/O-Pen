/*
  Gyroscope_ITG3200.cpp - Library for handling
*/

#include "Arduino.h"
#include "SixDOF.h"

// offsets are chip specific. 
int _g_offx;
int _g_offy;
int _g_offz;


SixDOF::SixDOF(int offx, int offy, int offz)
{
  Wire.begin();

  _g_offx = offx;
  _g_offy = offy;
  _g_offz = offz;

  initAccelerometer();
  initGyroscope();
} 

//initializes the Accelerometer
 /*****************************************
  * ADXL345
  * WAKE_UP set, with 8Hz frequency
  * AUTO_SLEEP set and MEASURE set.
  ******************************************/
void SixDOF::initAccelerometer() {
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
void SixDOF::getAccelerometerData(int * result) {
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

void SixDOF::rawAccToG(int *raw, float * RwAcc) {
  RwAcc[0] = ( (float) raw[0]) / 256.0;
  RwAcc[1] = ( (float) raw[1]) / 256.0;
  RwAcc[2] = ( (float) raw[2]) / 256.0;
}

void SixDOF::normalize3DVec(float * vector) {
  float R;
  R = sqrt(vector[0]*vector[0] + vector[1]*vector[1] + vector[2]*vector[2]);
  vector[0] /= R;
  vector[1] /= R;  
  vector[2] /= R;  
}


//initializes the gyroscope
  /*****************************************
  * ITG 3200
  * power management set to:
  * clock select = internal oscillator
  *     no reset, no sleep mode
  *   no standby mode
  * sample rate to = 125Hz
  * parameter to +/- 2000 degrees/sec
  * low pass filter = 5Hz
  * no interrupt
  ******************************************/
void SixDOF::initGyroscope() {
  byte val;
  Wire.beginTransmission(GYRO);
  Wire.write(G_PWR_MGM);
  val = 0x00;
  Wire.write(val);
  Wire.endTransmission();
  Wire.beginTransmission(GYRO);
  Wire.write(G_SMPLRT_DIV); // EB, 50, 80, 7F, DE, 23, 20, FF
  val = 0x07;
  Wire.write(val);
  Wire.endTransmission();
  Wire.beginTransmission(GYRO);
  Wire.write(G_DLPF_FS);  // +/- 2000 dgrs/sec, 1KHz, 1E, 19
  val = 0x1E;
  Wire.write(val);
  Wire.endTransmission();
  Wire.beginTransmission(GYRO);
  Wire.write(G_INT_CFG);
  val = 0x00;
  Wire.write(val);
  Wire.endTransmission();
}

/**************************************
  Gyro ITG-3200 I2C
  registers:
  temp MSB = 1B, temp LSB = 1C
  x axis MSB = 1D, x axis LSB = 1E
  y axis MSB = 1F, y axis LSB = 20
  z axis MSB = 21, z axis LSB = 22
  *************************************/
void SixDOF::getGyroscopeData(int * result) {
  int temp, x, y, z;
  byte buff[G_TO_READ];

  //read the gyro data from the ITG3200
  Wire.beginTransmission(GYRO); //start transmission to GYRO 
  Wire.write(G_REG_ADDRESS);        //sends address to read from
  Wire.endTransmission(); //end transmission

  Wire.beginTransmission(GYRO); //start transmission to GYRO
  Wire.requestFrom(GYRO, G_TO_READ);    // request 6 bytes from ACC

  int i = 0;
  while(Wire.available())    //ACC may send less than requested (abnormal)
  { 
    buff[i] = Wire.read(); // receive a byte
    i++;
  }
  Wire.endTransmission(); //end transmission

  result[0] = ((buff[2] << 8) | buff[3]) + _g_offx;
  result[1] = ((buff[4] << 8) | buff[5]) + _g_offy;
  result[2] = ((buff[6] << 8) | buff[7]) + _g_offz;
  result[3] = (buff[0] << 8) | buff[1]; // temperature

}

// convert raw readings to degrees/sec
void SixDOF::rawGyroToDegsec(int * raw, float * gyro_ds) {
  gyro_ds[0] = ((float) raw[0]) / 14.375;
  gyro_ds[1] = ((float) raw[1]) / 14.375;
  gyro_ds[2] = ((float) raw[2]) / 14.375;
}
