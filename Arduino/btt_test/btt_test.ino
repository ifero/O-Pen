char val;

void setup() {
  
  Serial.begin(115200);
  
}

void loop() {
  if (Serial.available())
  {
    val = Serial.read();
    Serial.print("you wrote \"");
    Serial.print(val);
    Serial.println("\"");
  }
}
  
