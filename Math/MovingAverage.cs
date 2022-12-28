using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class MovingAverage
        {
            #region fields
            private Queue<double> values;
            private int length;
            #endregion

            #region properties
            public double Average { get { return GetAverageValue(); } }
            public int Length { 
                get { return length; }
                set { length = value; }
            }
            #endregion

            #region constructors
            public MovingAverage()
            {
                length = 0;
                values = new Queue<double>();
            }

            public MovingAverage(int length)
            {
                this.length = length;
                values = new Queue<double>(length);
            }
            #endregion

            #region methods
            public void AddValue(double newVal)
            {
                values.Enqueue(newVal);
                TrimLength();
            }

            private void TrimLength()
            {
                while(values.Count > length)
                {
                    values.Dequeue();
                }
            }

            public double GetAverageValue()
            {
                double sum = 0;
                foreach (double val in values)
                {
                    sum += val;
                }
                return sum / values.Count;
            }
            #endregion
        }
    }
}
