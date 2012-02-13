
const int zAccPin = A0;
const int yAccPin = A1;
const int xAccPin = A2;

int zAcc = 0;
int yAcc = 0;
int xAcc = 0;


void setup() {
  pinMode(9,OUTPUT);
  Serial.begin(115200);
}

void loop(){

  zAcc = analogRead(zAccPin);
  yAcc = analogRead(yAccPin);
  xAcc = analogRead(xAccPin);

  int xAng = map(xAcc, 265,402, -90, 90);
  int yAng = map(yAcc, 265,402, -90, 90);
  int zAng = map(zAcc, 265,402, -90, 90);
  int x = RAD_TO_DEG * (atan2(-yAng, -zAng) + PI);
  int y = RAD_TO_DEG * (atan2(-xAng, -zAng) + PI);
  int z = RAD_TO_DEG * (atan2(-yAng, -xAng) + PI);
  
  Serial.print(x);
  Serial.print(".");
  Serial.print(y);
  Serial.print(".");
  Serial.println(z);

  delay(100);
}
