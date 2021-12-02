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
        public class PIDController<T>
        {
            // Gain vars
            private double pGain;
            private double iGain;
            private double dGain;

            // Integral / Derivative vars
            private int maxErrorMemory;
            T[] errorVals;

            // Private methods
            private void addToErrorMemory(T errorVal)
            {

            }

            // Public methods
            public T PIDController.getControlValue(T errorVal)
            {
                // Add error value to memory
                addToErrorMemory(errorVal);

                // Get P value
                T pVal = errorVal * pGain;

                // Get I value

            }

            // Constructors
            public PIDController<T>(double PGain, double IGain, double DGain, int MaxErrorMemory)
        {
            // Set gain vars
            pGain = PGain;
            iGain = IGain;
            dGain = DGain;

            // Create error Vals dictionary with max size
            maxErrorMemory = MaxErrorMemory;
            errorVals = new Dictionary<double, T>(MaxErrorMemory);
        }
    }
}
}
