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
        public class MovementController
        {
            private Base6Directions.Axis movementAxis;
            public Base6Directions.Axis MovementAxis { get { return movementAxis; } }

            private DecayingIntegralPIDController velocityController = new DecayingIntegralPIDController(2, 1, -10, 0.5);
            private double maxVelDistanceGain;
            private double maxVelMin;

            private IEnumerator<int> StepMovement()
            {
                yield return 0;
            }

            public void Run()
            {

            }

        }
    }
}
