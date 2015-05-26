using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.LeapMotion.DemoResources.Scripts
{
    public struct ObjectTrackerPixel
    {
        public int x;
        public int y;
        public int i;

        public Vector2 pos
        {
            get
            {
                return new Vector2(x, y);
            }
        }
    }

    public class ObjectTrackerGroup
    {
        public List<ObjectTrackerPixel> pixels = new List<ObjectTrackerPixel>();      
        public int intensity = 0;

        public List<Vector2> points = new List<Vector2>();
        public List<ObjectTrackerGroup> pointsGroups = new List<ObjectTrackerGroup>();
    }


    public class ObjectTrackerManager
    {
        public const int Width = 640;
        public const int Height = 240;

        public static ObjectTrackerGroup[] GetTrackedPixels(byte[] imageData)
        {
            List<ObjectTrackerGroup> bestGroups = new List<ObjectTrackerGroup>();

            //for(int i = 20; i > 7; i--)
            int i = 7;
            {
                int iDistance = i;
                iDistance = iDistance * iDistance;
 
                ObjectTrackerGroup[] testGroups = GetTrackedPixelsRaw(imageData, i);
                
                foreach(ObjectTrackerGroup testGroup in testGroups)
                {
                    foreach(ObjectTrackerPixel pixel in testGroup.pixels)
                    {
                        List<int> combine = new List<int>();

                        bool added = false;
                        for (int j = 0; j < testGroup.points.Count; j++)
                        {
                            
                            //k-means
                            foreach (ObjectTrackerPixel eachPixel in testGroup.pointsGroups[j].pixels)
                            {
                                if ((eachPixel.pos - pixel.pos).sqrMagnitude < 16)
                                {
                                    Vector2 pointPos = testGroup.points[j];
                                    pointPos += pixel.pos;
                                    pointPos *= 0.5f;

                                    testGroup.pointsGroups[j].pixels.Add(pixel);
                                    
                                    added = true;
                                    combine.Add(j);
                                    
                                    break;
                                }
                            }
                            

                            /*
                            //Hardcoded attieciba pret scale
                            if ((testGroup.points[j] - pixel.pos).sqrMagnitude < iDistance)
                            {
                                Vector2 pointPos = testGroup.points[j];
                                pointPos += pixel.pos;
                                pointPos *= 0.5f;

                                testGroup.pointsGroups[j].pixels.Add(pixel);

                                added = true;
                                break;
                            }
                             */
                        }

                        if(!added)
                        {                            
                            testGroup.points.Add(new Vector2(pixel.x, pixel.y));
                            testGroup.pointsGroups.Add(new ObjectTrackerGroup());
                            testGroup.pointsGroups.Last().pixels.Add(pixel);
                        }

                        if(combine.Count > 1)
                        {
                            for (int jC = combine.Count - 1; jC >= 1; jC--)
                            {
                                int index1 = combine[0];
                                int index2 = combine[jC];

                                testGroup.pointsGroups[index1].points.AddRange(testGroup.pointsGroups[index2].points);
                                testGroup.pointsGroups.RemoveAt(index2);

                                testGroup.points[index1] += testGroup.points[index2];
                                testGroup.points[index1] *= 0.5f;

                                testGroup.points.RemoveAt(index2);
                            }
                        }
                    }

                    if(testGroup.points.Count >= 4)
                    {

                        //bestGroups.Add(testGroup);
                        //break;

                        //at least 4 points must be points similar size
                        bool sameSize = false;
                        

                        foreach (ObjectTrackerGroup testGroupSize1 in testGroup.pointsGroups)
                        {
                            int iSameSizeCount = 0;
                            double similarity = 0.6;

                            foreach (ObjectTrackerGroup testGroupSize2 in testGroup.pointsGroups)
                            {
                                if(testGroupSize1 != testGroupSize2)
                                {
                                    if( (double)(Math.Min(testGroupSize1.pixels.Count, testGroupSize2.pixels.Count)) / (double)(Math.Max(testGroupSize1.pixels.Count, testGroupSize2.pixels.Count)) > similarity )
                                    {
                                        iSameSizeCount++;
                                        //Same with 3 others
                                        if(iSameSizeCount >= 3)
                                        {
                                            sameSize = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (iSameSizeCount >= 3)
                            {
                                break;
                            }
                        }
                        

                        if (sameSize)
                        {
                            //Debug.Log(testGroup.points.Count);
                            bestGroups.Add(testGroup);
                            break;
                        }
                        else
                        {
                           // Debug.Log("XX");
                        }
                    }
                }

                if(bestGroups.Count > 0)
                {
                   // break;
                }
            }

            return bestGroups.ToArray();
        }

        public static ObjectTrackerGroup[] GetTrackedPixelsRaw(byte[] imageData, int thereshold /*= 10*/)
        {
            if(imageData.Length < (Width * Height))
            {
                return new ObjectTrackerGroup[0];
            }

            //All values
            ObjectTrackerGroup[] rawList = new ObjectTrackerGroup[256];

            for (int i = (Width * Height) - 1; i > 0; i--)
            {                
                byte intensity = imageData[i];

                int x = i % Width;
                int y = i / Width;

                if (rawList[intensity] == null)
                {
                    rawList[intensity] = new ObjectTrackerGroup();
                }

                rawList[intensity].pixels.Add(new ObjectTrackerPixel()
                {
                    x = x,
                    y = y,
                    i = i
                });
            }

            List<ObjectTrackerGroup> result = new List<ObjectTrackerGroup>();
            List<ObjectTrackerGroup> trail = new List<ObjectTrackerGroup>();

            for (int i = 0; i < 256; i++ )
            {
                if (rawList[i] == null)
                {
                    rawList[i] = new ObjectTrackerGroup();
                }

                rawList[i].intensity = i;

                if( (rawList[i].pixels.Count >= thereshold && i > 0) || (i == 255) )
                {
                    if (trail.Count > 0)
                    {
                        int lowestPoint = GetLowestPointInTrail(trail);

                        for (int j = 0; j < trail.Count; j++)
                        {
                            if (j < lowestPoint)
                            {
                                if (result.Count > 0)
                                {
                                    result.Last().pixels.AddRange(trail[j].pixels);
                                }
                            }
                            else
                            {
                                rawList[i].pixels.AddRange(trail[j].pixels);
                            }
                        }

                        result.Add(rawList[i]);
                        trail = new List<ObjectTrackerGroup>();
                    }
                    else
                    {
                        result.Last().pixels.AddRange(rawList[i].pixels);
                    }                    
                }
                else
                {
                    trail.Add(rawList[i]);
                }
            }

            //middle points
            // thereshold ? 25

            int backgroundLayer = (int)(Width * Height * 0.005);
            int iCount = 0;
            while(iCount < result.Count)
            {
                if(result[iCount].pixels.Count >= backgroundLayer)
                {
                    result.RemoveAt(iCount);
                }
                else
                {
                    iCount++;
                }
            }

            result.Sort(delegate(ObjectTrackerGroup a, ObjectTrackerGroup b)
            {
                return b.pixels.Count - a.pixels.Count;
            });

            return result.ToArray();
        }

        private static int GetLowestPointInTrail(List<ObjectTrackerGroup> trail)
        {
            int lowestPoint = trail.Count / 2;
            int count = trail[lowestPoint].pixels.Count;
            for (int j = 0; j < trail.Count; j++)
            {
                if(count > trail[j].pixels.Count)
                {
                    lowestPoint = j;
                    count = trail[j].pixels.Count;
                }
            }

            return lowestPoint;
        }
    }
}
