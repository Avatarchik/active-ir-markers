using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    public class TimedPosition
    {
        public float Time
        {
            get;
            set;
        }

        public Vector3 Positon
        {
            get;
            set;
        }
    }

    public class MarkerFiltered
    {
        public const int HistoryCount = 100;
        public const float Q_Error = 0.5f; //Speed based error
        public const float R_Error = 0.5f; //Measure error

        private Matrix3x3 Covariance = Matrix3x3.identity;

        public GameObject GameObjectMarker
        {
            get;
            set;
        }

        public Vector3 Positon
        {
            get;
            set;
        }

        //Units @ second
        public Vector3 Speed
        {
            get;
            set;
        }

        private List<TimedPosition> _history = new List<TimedPosition>();
        public List<TimedPosition> History
        {
            get
            {
                return _history;
            }
        }

        public void UpdatePosition(Vector3 measuredPos, MarkerFiltered[] other)
        {
            float timeSinceLast = 0f;
            if (_history.Count > 0)
            {
                timeSinceLast = Time.time - _history.Last().Time;
            }

            if (_history.Count > HistoryCount)
            {
                _history.RemoveAt(0);
            }
            _history.Add(new TimedPosition()
            {
                Time = Time.time,
                Positon = measuredPos
            });

            if (_history.Count > 10)
            {
                Vector3 speed = Vector3.zero;
                foreach (TimedPosition each in _history)
                {
                    speed += each.Positon;
                }
                speed /= (_history.Last().Time - _history.First().Time);
                
                //Average speed from other diodes
                float count = 1f;
                foreach(MarkerFiltered each in other)
                {
                    if (each != this && each.History.Count > 10)
                    {
                        speed += each.Speed;
                        count += 1f;
                    }
                }
                speed /= count;

                Speed = speed;

                Vector3 X_n_p = Positon + Speed * timeSinceLast;

                Matrix3x3 Q = new Matrix3x3(
                Q_Error * timeSinceLast, 0, 0,
                0, Q_Error * timeSinceLast, 0,
                0, 0, Q_Error * timeSinceLast
                );
                Matrix3x3 P_n_p = Covariance + Q;

                Vector3 X_n_pp = measuredPos - X_n_p;

                Matrix3x3 R = new Matrix3x3(
                R_Error * timeSinceLast, 0, 0,
                0, R_Error * timeSinceLast, 0,
                0, 0, R_Error * timeSinceLast
                );
                Matrix3x3 S = P_n_p + R;

                Matrix3x3 K = P_n_p * ExtensionMethods.Inverse(S);

                Positon = X_n_p + ExtensionMethods.MultiplyVector(K, X_n_pp);
                Covariance = K * P_n_p;

                //Update last pos
                _history.Last().Positon = Positon;
            }
            else
            {
                Positon = measuredPos;
            }

            if(GameObjectMarker != null)
            {
                GameObjectMarker.transform.position = Positon; 
            }
        }
    }
}
