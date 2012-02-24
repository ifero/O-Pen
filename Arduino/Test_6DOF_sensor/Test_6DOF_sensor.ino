#include <Wire.h> //I2C library

//Accelerometer ADXL345 parameters
#define ACC 0x53  //ADXL address
#define ACC_POWER_CTL 0x2D
#define ACC_REG_ADDRESS 0x32 //first axis-acceleration-data register on the ADXL245
#define ACC_TO_READ 6 //number of bytes to be read each time (2 bytes each axis)

//Gyroscope ITG3200 parameters
#define GYRO 0x68 //ITG3200 address
#define G_SMPLRT_DIV 0x15
#define G_DLPF_FS 0x16
#define G_INT_CFG 0x17
#define G_PWR_MGM 0x3E
#define G_REG_ADDRESS 0x1B
#define G_TO_READ 8 //number of bytes to be read each time (2 bytes each axis)

// offsets are chip specific. 
int g_offx = 120;
int g_offy = 20;
int g_offz = 93;

boolean firstSample = true;

float RwAcc[3];  //projection of normalized gravitation force vector on x/y/z axis, as measured by accelerometer
float Gyro_ds[3]; //Gyro readings
float RwGyro[3];        //Rw obtained from last estimated value and gyro movement
float Awz[2];           //angles between projection of R on XZ/YZ plane and Z axis (deg)
float RwEst[3];

int lastTime = 0;
int interval = 0;
float wGyro = 10.0;

//initializes the Accelerometer
 /*****************************************
  * ADXL345
  * WAKE_UP set, with 8Hz frequency
  * AUTO_SLEEP set and MEASURE set.
  ******************************************/
void initAccelerometer() {
  writeTo(ACC, ACC_POWER_CTL, 0);
  writeTo(ACC, ACC_POWER_CTL, 16);
  writeTo(ACC, ACC_POWER_CTL, 8);
}

/**************************************
  Accelerometer ADXL345 I2C
  registers:
  x axis MSB = 32, x axis LSB = 33
  y axis MSB = 34, y axis LSB = 35
  z axis MSB = 36, z axis LSB = 37
  *************************************/
void getAccelerometerData(int *result) {
  byte buffer[ACC_TO_READ];
  readFrom(ACC, ACC_REG_ADDRESS, ACC_TO_READ, buffer);
  result[0] = ( (int) buffer[1] << 8) | buffer[0];
  result[1] = ( (int) buffer[3] << 8) | buffer[2];
  result[2] = ( (int) buffer[5] << 8) | buffer[4];
}

void rawAccToG(int *raw, float * RwAcc) {
  RwAcc[0] = ( (float) raw[0]) / 256.0;
  RwAcc[1] = ( (float) raw[1]) / 256.0;
  RwAcc[2] = ( (float) raw[2]) / 256.0;
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
void initGyroscope()
{
  writeTo(GYRO, G_PWR_MGM, 0x00);
  writeTo(GYRO, G_SMPLRT_DIV, 0x07);
  writeTo(GYRO, G_DLPF_FS, 0x1E);
  writeTo(GYRO, G_INT_CFG, 0x00);
}

/**************************************
  Gyro ITG-3200 I2C
  registers:
  temp MSB = 1B, temp LSB = 1C
  x axis MSB = 1D, x axis LSB = 1E
  y axis MSB = 1F, y axis LSB = 20
  z axis MSB = 21, z axis LSB = 22
  *************************************/
void getGyroscopeData(int * result)
{
  byte buffer[G_TO_READ];
  readFrom(GYRO, G_REG_ADDRESS, G_TO_READ, buffer);
  result[0] = ( (buffer[2] << 8) | buffer[3]) + g_offx;
  result[0] = ( (buffer[4] << 8) | buffer[5]) + g_offy;
  result[0] = ( (buffer[6] << 8) | buffer[7]) + g_offz;
  result[0] = ( (buffer[0] << 8) | buffer[1]);
}

//convert raw readings to degrees/sec
void rawGyroToDegsec(int *raw, float *gyro_ds)
{
  gyro_ds[0] = ( (float) raw[0]) / 14.375;
  gyro_ds[1] = ( (float) raw[1]) / 14.375;
  gyro_ds[2] = ( (float) raw[2]) / 14.375;
}

void normalize3DVec(float * vector) {
  float R;
  R = sqrt(vector[0]*vector[0] + vector[1]*vector[1] + vector[2]*vector[2]);
  vector[0] /= R;
  vector[1] /= R;  
  vector[2] /= R;  
}


float squared(float x){
  return x*x;
}


void getInclination() {
  int w = 0;
  float tmpf = 0.0;
  int currentTime, signRzGyro;
  
  
  currentTime = millis();
  interval = currentTime - lastTime;
  lastTime = currentTime;
  
  if (firstSample) { // the NaN check is used to wait for good data from the Arduino
    for(w=0;w<=2;w++) {
      RwEst[w] = RwAcc[w];    //initialize with accelerometer readings
    }
  }
  else{
    //evaluate RwGyro vector
    if(abs(RwEst[2]) < 0.1) {
      //Rz is too small and because it is used as reference for computing Axz, Ayz it's error fluctuations will amplify leading to bad results
      //in this case skip the gyro data and just use previous estimate
      for(w=0;w<=2;w++) {
        RwGyro[w] = RwEst[w];
      }
    }
    else {
      //get angles between projection of R on ZX/ZY plane and Z axis, based on last RwEst
      for(w=0;w<=1;w++){
        tmpf = Gyro_ds[w];                        //get current gyro rate in deg/s
        tmpf *= interval / 1000.0f;                     //get angle change in deg
        Awz[w] = atan2(RwEst[w],RwEst[2]) * 180 / PI;   //get angle and convert to degrees 
        Awz[w] += tmpf;             //get updated angle according to gyro movement
      }
      
      //estimate sign of RzGyro by looking in what qudrant the angle Axz is, 
      //RzGyro is pozitive if  Axz in range -90 ..90 => cos(Awz) >= 0
      signRzGyro = ( cos(Awz[0] * PI / 180) >=0 ) ? 1 : -1;
      
      //reverse calculation of RwGyro from Awz angles, for formulas deductions see  http://starlino.com/imu_guide.html
      for(w=0;w<=1;w++){
        RwGyro[0] = sin(Awz[0] * PI / 180);
        RwGyro[0] /= sqrt( 1 + squared(cos(Awz[0] * PI / 180)) * squared(tan(Awz[1] * PI / 180)) );
        RwGyro[1] = sin(Awz[1] * PI / 180);
        RwGyro[1] /= sqrt( 1 + squared(cos(Awz[1] * PI / 180)) * squared(tan(Awz[0] * PI / 180)) );        
      }
      RwGyro[2] = signRzGyro * sqrt(1 - squared(RwGyro[0]) - squared(RwGyro[1]));
    }
    
    //combine Accelerometer and gyro readings
    for(w=0;w<=2;w++) RwEst[w] = (RwAcc[w] + wGyro * RwGyro[w]) / (1 + wGyro);

    normalize3DVec(RwEst);
  }
  
  firstSample = false;
}

void setup()
{
  Serial.begin(19200);
  Wire.begin();
  initAccelerometer();
  initGyroscope();
}

void loop()
{
  if(!Serial.available()) 
  {
    int acc[3];
    int gyro[4];
    
    getAccelerometerData(acc);
    rawAccToG(acc, RwAcc);
    
    getGyroscopeData(gyro);
    rawGyroToDegsec(gyro, Gyro_ds);
    
        serialPrintFloatArr(RwAcc, 3);
    serialPrintFloatArr(Gyro_ds, 3);
    serialPrintFloatArr(RwGyro, 3);
    serialPrintFloatArr(Awz, 2);
    serialPrintFloatArr(RwEst, 3);
   
    Serial.println();
  }
}

void serialPrintFloatArr(float * arr, int length) {
  for(int i=0; i<length; i++) {
    serialFloatPrint(arr[i]);
    Serial.print(",");
  }
}


void serialFloatPrint(float f) {
  byte * b = (byte *) &f;
  Serial.print("f:");
  for(int i=0; i<4; i++) {
    
    byte b1 = (b[i] >> 4) & 0x0f;
    byte b2 = (b[i] & 0x0f);
    
    char c1 = (b1 < 10) ? ('0' + b1) : 'A' + b1 - 10;
    char c2 = (b2 < 10) ? ('0' + b2) : 'A' + b2 - 10;
    
    Serial.print(c1);
    Serial.print(c2);
  }
}

//---------------- Functions
//Writes val to address register on ACC
void writeTo(int DEVICE, byte address, byte val) {
   Wire.beginTransmission(DEVICE); //start transmission to ACC 
   Wire.write(address);        // send register address
   Wire.write(val);        // send value to write
   Wire.endTransmission(); //end transmission
}


//reads num bytes starting from address register on ACC in to buff array
void readFrom(int DEVICE, byte address, int num, byte buff[]) {
  Wire.beginTransmission(DEVICE); //start transmission to ACC 
  Wire.write(address);        //sends address to read from
  Wire.endTransmission(); //end transmission
  
  Wire.beginTransmission(DEVICE); //start transmission to ACC
  Wire.requestFrom(DEVICE, num);    // request 6 bytes from ACC
  
  int i = 0;
  while(Wire.available())    //ACC may send less than requested (abnormal)
  { 
    buff[i] = Wire.read(); // receive a byte
    i++;
  }
  Wire.endTransmission(); //end transmission
}
