using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
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
    partial class Program : MyGridProgram
    {
        // VARS
        double maxSpeed = 100;
        double velocityProportionalGain = 0.125;

        Vector3D destinationVector = new Vector3(-37806.02, -36183.24, -36770.72);

        List<IMyThrust> allThrusters = new List<IMyThrust>();

        float[] maxThrustInDirections = new float[6];
        List<IMyThrust>[] thrustersInDirections = new List<IMyThrust>[6];

        /*
        * THRUSTER INDEXES:
        * 0 - FORWARD
        * 1 - BACKWARD
        * 2 - LEFT
        * 3 - RIGHT
        * 4 - UP
        * 5 - DOWN
        */


        IMyShipController controller;


        public Program()
        {

            controller = (IMyShipController)GridTerminalSystem.GetBlockWithName("PrimaryController");
            Runtime.EstablishCoroutines(Echo);
            Coroutine.AddCoroutine(MoveCoroutine);

        }
        
        public void Save()
        {



        }

        public void Main(string argument, UpdateType updateSource)
        {

            Coroutine.StepCoroutines(updateSource);

        }


        public IEnumerator<int> MoveCoroutine()
        {

            while (true)
            {
                // Calculate desired destination vector
                Vector3D localDestVector = destinationVector.ConvertToLocalPosition(controller);

                // Get thrusters
                GetThrusters();
                CalcMaxEffectiveThrusts();

                // Counter any gravity influencing the ship
                Vector3D gravity = controller.GetNaturalGravity();

                // Move in desired direction

                // Calculate desired velocity
                Vector3D desiredVelocity = localDestVector * velocityProportionalGain;
                if(desiredVelocity.Length() > maxSpeed)
                {
                    desiredVelocity = Vector3D.Normalize(desiredVelocity) * maxSpeed;
                }

                yield return 0;
            }
            
        }

        public void GetThrusters()
        {
            // Create lists at each index for thrust directions array
            for (int i = 0; i < 6; i++)
            {
                thrustersInDirections[i] = new List<IMyThrust>();
            }

            // Adding thrusters to lists depending on their direction
            foreach (IMyThrust Thruster in allThrusters)
            {

                foreach (Base6Directions.Direction Direction in Enum.GetValues(typeof(Base6Directions.Direction)))
                {

                    if (Base6Directions.GetOppositeDirection(Thruster.Orientation.Forward) == controller.Orientation.TransformDirection(Direction))
                    {

                        thrustersInDirections[(int)Direction].Add(Thruster);
                        break;

                    }

                }

            }

        }
        public void CalcMaxEffectiveThrusts()
        {
            for (int DirectionIndex = 0; DirectionIndex < 6; DirectionIndex++)
            {

                maxThrustInDirections[DirectionIndex] = 0;

                foreach (IMyThrust Thruster in thrustersInDirections[DirectionIndex])
                {

                    maxThrustInDirections[DirectionIndex] += Thruster.MaxEffectiveThrust;

                }

            }
        }
    }
}
