// the setup routine runs once when you press reset:


int ledOnBoard = 13;
int led1 = 9;
int led2 = 8;
int led3 = 7;
//*/

double freq = (1.0 / 37.0) * 1000.0;
//double freq = 1 * 1000.0;
//



//1010
//xx1x010
//0110010
//0000

//0000
//xx0x000

//1111
//xx1x111

//011 = 3
//101 = 5

//110 = 6
//111 = 7
//111

//10
//xx1x0
//11100

//11101
//01 1
//101 = 5 

//111111111000000

//11
//xx1x1
//01111

//01
//xx0x1
//10011
//111000000111111

/*
#define PARITY 1
bool parityCoding = true;
bool manchester = false;
int breakingBits = 3;

int dataLen = 2;
uint8_t led1ID_BEFORE[2] =     { 1, 0}; //red
uint8_t led2ID_BEFORE[2] =    { 0, 1}; //green
uint8_t led3ID_BEFORE[2] =   { 1, 1};  //blue

int stringLen = 5;
uint8_t led1ID[5] = { 0, 0, 0, 0, 0};
uint8_t led2ID[5] = { 0, 0, 0, 0, 0};
uint8_t led3ID[5] = { 0, 0, 0, 0, 0};
//*/


bool parityCoding = false;
bool manchester = false;
int breakingBits = 0;
int stringLen = 5;
uint8_t led1ID[5] =     { 1, 0, 1, 0, 0}; //red 1010110101
uint8_t led2ID[5] =    { 1, 0, 1, 0, 1}; //green 100011000110001
uint8_t led3ID[5] =   { 1, 1, 1, 0, 0};  //blue 1110011100
//*/

/*
bool parityCoding = false;
bool manchester = false;
int breakingBits = 0;
int stringLen = 15;
uint8_t led1ID[15] =     { 1, 0, 1, 0, 0, 1, 0, 1, 1, 1, 1,0,0,0,1}; //red 1010110101
uint8_t led2ID[15] =    { 1, 0, 1, 0, 1, 1, 0, 0, 0, 1, 1,0,0,1,1}; //green 100011000110001
uint8_t led3ID[15] =   { 1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 1,0,1,0,1};  //blue 1110011100

//*/


//*/

/*
bool parityCoding = false;
bool manchester = true;
int breakingBits = 3;
int stringLen = 4;
uint8_t led1ID[4] =     {0, 1, 0, 1}; //red 00001 00001
uint8_t led2ID[4] =     {0, 1, 1, 1}; //green 00010 00010
uint8_t led3ID[4] =     {0, 0, 0, 1};  //blue 0011
//*/

// 00111111
// 10101010
// 10010101

  
// 1122334411223344
// 1010101010101010
// 1100001111000011
// 0110100101101001
//   0 0 1 1 0 0 1

// 0110000001110001111110011100000011101000111000000
//   10  0  1  0  1  1  0 1  0  0  1  010  1  0  0
//   10  0  1  0  1  1  0 1  0  0  1  10  1  0  0
//100101101001 010100



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

void setupParityBits(uint8_t* before, uint8_t* after)
{
  //011 = 3
  //101 = 5
  
  after[0] = before[0] ^ before[1];
  after[1] = before[0];
  after[2] = before[0]; //3
  after[3] = before[1];
  after[4] = before[1]; //5
  
  //10
  //11
  //01
  
  //10110
}

void loop() {
 
  digitalWrite(ledOnBoard, getEncoded(led1ID[frameCount]) ? HIGH : LOW);
  
  digitalWrite(led1, getEncoded(led1ID[frameCount]) ? HIGH : LOW);
  digitalWrite(led2, getEncoded(led2ID[frameCount]) ? HIGH : LOW);  
  digitalWrite(led3, getEncoded(led3ID[frameCount]) ? HIGH : LOW);
  
  clockFlip = !clockFlip;
  transmitCount++;
  
  delay(freq);
  
  if(!manchester || transmitCount == 2)
  {
    transmitCount = 0;
    
    frameCount++;
    if(frameCount == stringLen)
    {
      if(breakingBits > 0)
      {
        digitalWrite(ledOnBoard, LOW);  
        digitalWrite(led1, LOW);
        digitalWrite(led2, LOW);  
        digitalWrite(led3, LOW);
        delay(freq * (double)(breakingBits));
      }
      frameCount = 0;
    }
  }
}


void setup() {                

#ifdef PARITY
  setupParityBits(led1ID_BEFORE, led1ID);
  setupParityBits(led2ID_BEFORE, led2ID);
  setupParityBits(led3ID_BEFORE, led3ID);
#endif  
  
  //16MHz
  
  // initialize the digital pin as an output.
  //pinMode(0, OUTPUT); //LED on Model B
  pinMode(ledOnBoard, OUTPUT); //LED on Model A
  
  pinMode(led1, OUTPUT);
  pinMode(led2, OUTPUT);
  pinMode(led3, OUTPUT);
  
}
