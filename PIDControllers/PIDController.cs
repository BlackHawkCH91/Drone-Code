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
        public class PIDController
        {

            private double _PGain = 0;
            private double _IGain = 0;
            private double _DGain = 0;

            protected double _errorSum = 0;
            protected double _prevError = 0;

            public double prevControlValue { get; private set; }

            protected virtual double GetIntegral(double errorVal, double timeStep)
            {
                return _errorSum + errorVal * timeStep;
            }

            protected virtual double GetDerivative(double errorVal, double timeStep)
            {
                return (errorVal - _prevError) * (1 / timeStep);
            }

            public double GetControlValue(double errorVal, double timeStep)
            {
                // Get integral and derivative
                double errorIntegral = GetIntegral(errorVal, timeStep);
                double errorDerivative = GetDerivative(errorVal, timeStep);

                // Store error val as prev error
                _prevError = errorVal;

                // Return control value
                prevControlValue = _PGain * errorVal + _IGain * errorIntegral + _DGain * errorDerivative;
                return prevControlValue;
            }

            // Constructor
            public PIDController(double PGain, double IGain, double DGain)
            {
                _PGain = PGain;
                _IGain = IGain;
                _DGain = DGain;
            }

        }

        public class DecayingIntegralPIDController : PIDController
        {
            private double _decayRatio;

            protected override double GetIntegral(double errorVal, double timeStep)
            {
                return _errorSum * (1.0 - _decayRatio) + errorVal * timeStep;
            }

            // Constructor
            public DecayingIntegralPIDController(double PGain, double IGain, double DGain, double decayRatio) : base(PGain, IGain, DGain)
            {
                _decayRatio = decayRatio;
            }
        }
    }
}
