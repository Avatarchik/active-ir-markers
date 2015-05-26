using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    public class CamBlob
    {
        public Vector2 Position;
        public double Size;
        public int ID = -1;

        public byte Thereshold = 0;

        public int BestPatternCount = 0;

        public int LastID = -1;
        public int LastStartPos = 0;


        public long LastTime = 0;
        public long LastTimePositive = 0; //+1 bit

        public List<KeyValuePair<long, byte>> Data = new List<KeyValuePair<long, byte>>();

        public List<byte> DecodedData = new List<byte>(); //Seperated by '7'
        public int DecodedCount = 0;
        public int FailedCount = 0;

        public int StartMarkerCount = 0; //when using start marker / zero break
        public int PositiveCount = 0; //when not zero break

        public long ClassifierAlive = 0;
        public Dictionary<int, long> ClassifierCount = new Dictionary<int, long>();

        public double Probablity
        {
            get
            {
                if (ID >= 0)
                {
                    return 0d;
                }
                return (double)ClassifierCount[ID] / (double)ClassifierAlive;
            }
        }
    }
}
