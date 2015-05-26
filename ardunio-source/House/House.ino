// the setup routine runs once when you press reset:

/*
int ledOnBoard = 13;
int ledBottomLeft = 5;
int ledBottomMiddle = 7;
int ledBottomRight = 8;
int ledTopMiddle = 9;
//*/


int ledOnBoard = 1;
int ledBottomLeft = 5;
int ledBottomMiddle = 4;
int ledBottomRight = 3;
int ledTopMiddle = 2;
//*/

double freq = (1.0 / 37.0) * 1000.0;
//double freq = 5 * 1000.0;
//

/*
int stringLen = 8;
uint8_t ledBottomLeftID[8] =  {1, 0, 0, 1, 0, 0, 0, 1}; //10010001 //red
uint8_t ledBottomRightID[8] = {1, 0, 0, 0, 0, 1, 0, 1}; //10000101 //green
uint8_t ledTopMiddleID[8] =   {1, 0, 0, 0, 0, 0, 1, 1}; //10000011 //blue
*/

int stringLen = 4;
uint8_t ledBottomLeftID[4] =  { 0, 0, 0, 1}; //10010001 //red
uint8_t ledBottomRightID[4] = { 0, 1, 0, 1}; //10000101 //green
uint8_t ledTopMiddleID[4] =   { 0, 0, 1, 1}; //10000011 //blue

//red
//  1  0  0  1  0  0  0  1
//c 10 10 10 10 10 10 10 10
//t 12 34 56 78 90 12 34 56  
//  01 10 10 01 10 10 10 01 
//  1  0  0  1  0  0  0  1
// 0110100110101001

//green
//  1  0  0  1  0  0  0  1
//c 10 10 10 10 10 10 10 10
//  01 10 10 01 10 10 10 01
// 0110100110101001

//blue
// 1  0  0  0  0  0  1  1
// 10 10 10 10 10 10 10 10
// 01 10 10 10 10 10 01 01
// 0110101010100101

bool testLed = false;
bool manchester = true;

int frameCount = 0;
int transmitCount = 0;
uint8_t clockFlip = 1;

uint8_t getEncoded(uint8_t val)
{
  if(manchester)
  {
    return val ^ clockFlip;
  }
  else
  {
    return val;
  }
}

// the loop routine runs over and over again forever:
void loop() {
  //return;
  //digitalWrite(ledBottomLeft, getEncoded(ledBottomLeftID[frameCount]) ? HIGH : LOW);
  digitalWrite(ledBottomMiddle, getEncoded(ledBottomLeftID[frameCount]) ? HIGH : LOW);  
  digitalWrite(ledBottomRight, getEncoded(ledBottomRightID[frameCount]) ? HIGH : LOW);
  digitalWrite(ledTopMiddle, getEncoded(ledTopMiddleID[frameCount]) ? HIGH : LOW);
  
  clockFlip = !clockFlip;
  transmitCount++;
  
  digitalWrite(ledOnBoard, testLed ? HIGH : LOW);
  testLed = !testLed;

  delay(freq);
  
  if(!manchester || transmitCount % 2 == 0)
  {
    frameCount++;
    if(frameCount == stringLen)
    {
      frameCount = 0;
      
      if(!manchester)
      {
        digitalWrite(ledBottomLeft, LOW);
        digitalWrite(ledBottomMiddle, LOW);    
        digitalWrite(ledBottomRight, LOW);
        digitalWrite(ledTopMiddle, LOW);
        
        delay(freq * 4);
      }    
    }
  }
}


void setup() {                
  
  //16MHz
  
  // initialize the digital pin as an output.
  //pinMode(0, OUTPUT); //LED on Model B
  pinMode(ledOnBoard, OUTPUT); //LED on Model A   
  digitalWrite(ledOnBoard, LOW);
  
  pinMode(ledBottomLeft, OUTPUT);
  pinMode(ledBottomMiddle, OUTPUT);
  pinMode(ledBottomRight, OUTPUT);
  pinMode(ledTopMiddle, OUTPUT);
  
  digitalWrite(ledBottomMiddle, HIGH);
  
  digitalWrite(ledBottomLeft, HIGH);
  digitalWrite(ledBottomMiddle, HIGH);
  digitalWrite(ledBottomRight, HIGH);
  digitalWrite(ledTopMiddle, HIGH);
}
