int i = 1;

void setup() {
  Serial.begin(19200);
}

void loop() {
  Serial.print("just saying hi for the ");
  Serial.print(i);
  Serial.print("th time. \n");
  delay(200);
  i++;
}
