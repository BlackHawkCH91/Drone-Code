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
        public class LinearMap
        {
            #region fields
            private double inputMin;
            private double inputMax;
            private double outputMin;
            private double outputMax;
            private double slope;
            #endregion

            #region properties
            public double InputMin
            {
                get { return inputMin; }
                set
                {
                    inputMin = value;
                    CalculateSlope();
                }
            }
            public double InputMax
            {
                get { return inputMax; }
                set
                {
                    inputMax = value;
                    CalculateSlope();
                }
            }
            public double OutputMin
            {
                get { return outputMin; }
                set
                {
                    outputMin = value;
                    CalculateSlope();
                }
            }
            public double OutputMax
            {
                get { return outputMax; }
                set
                {
                    outputMax = value;
                    CalculateSlope();
                }
            }
            #endregion

            #region constructors
            public LinearMap(double inputMin, double inputMax, double outputMin, double outputMax)
            {
                this.inputMin = inputMin;
                this.inputMax = inputMax;
                this.outputMin = outputMin;
                this.outputMax = outputMax;
                CalculateSlope();
            }
            #endregion

            #region methods
            private void CalculateSlope()
            {
                slope = (outputMax - outputMin) / (inputMax - inputMin);
            }
            
            public double GetValue(double value)
            {
                return outputMin + slope * (value - inputMin);
            }
            #endregion
        }
    }
}
