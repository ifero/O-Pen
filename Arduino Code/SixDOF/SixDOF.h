/*
  Gyroscope_ITG3200.h - Library for handling
  ITG3200 Gyroscopes
*/
#ifndef SixDOF_h
#define SixDOF_h

#include "Arduino.h"
#include <Wire.h> //I2C library


//Accelerometer ADXL345 parameters
#define ACC 0x53  //ADXL address
#define ACC_POWER_CTL 0x2D
#define ACC_REG_ADDRESS 0x32 //first axis-acceleration-data register on the ADXL245
#define ACC_TO_READ 6 //number of bytes to be read each time (2 bytes each axis)

// Gyroscope ITG3200 
#define GYRO 0x69 // gyro address, binary = 11101001 when AD0 is connected to Vcc (see schematics of your breakout board)
#define G_SMPLRT_DIV 0x15
#define G_DLPF_FS 0x16
#define G_INT_CFG 0x17
#define G_PWR_MGM 0x3E
#define G_REG_ADDRESS 0x1B
#define G_TO_READ 8 // 2 bytes for each axis x, y, z

class SixDOF
{
  public:
    SixDOF(int offx, int offy, int offz);
    
    void initAccelerometer();
    void getAccelerometerData(int *result);
    void rawAccToG(int *raw, float * RwAcc);
    void normalize3DVec(float * vector);

    void initGyroscope();
    void getGyroscopeData(int * result);
    void rawGyroToDegsec(int * raw, float * gyro_ds);
  
  private:
    ;    
};

#endif 