int i = 1;

void setup() {
  Serial.begin(9600);
}

void loop() {
  Serial.print("just saying hi for the ");
  Serial.print(i);
  Serial.print("th time. \n");
  delay(2000);
  i++;
}
