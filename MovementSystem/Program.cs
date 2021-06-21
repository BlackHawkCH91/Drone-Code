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
        Dictionary<Base6Directions.Direction, ThrustGroup> thrustDirections = new Dictionary<Base6Directions.Direction, ThrustGroup>();
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

                // Counter any gravity influencing the ship
                CounterGravity();

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
            // Create thruster groups
            foreach (Base6Directions.Direction direction in Enum.GetValues(typeof(Base6Directions.Direction)))
            {
                thrustDirections.Add(direction, new ThrustGroup());
            }

            // Get all thrusters
            List<IMyThrust> allThrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(allThrusters);

            // Adding thrusters to groups depending on their direction
            foreach (IMyThrust thruster in allThrusters)
            {
                // Corrects the directions to use the controller as a reference
                Base6Directions.Direction correctedForwardDirection = controller.Orientation.TransformDirection(thruster.Orientation.Forward);

                // Add the thruster to the direction that it's thrust is pointing in - opposite of it's forward direction
                thrustDirections[Base6Directions.GetOppositeDirection(correctedForwardDirection)].AddThruster(thruster);
            }
        }

        public void CounterGravity()
        {
            // Get total gravity force vector (f = m*a)
            Vector3D gravity = controller.GetNaturalGravity() * controller.CalculateShipMass().TotalMass;

            // Calculate how much force gravity is applying in each direction and apply thrust in that direction
            foreach (Base6Directions.Direction direction in Enum.GetValues(typeof(Base6Directions.Direction)))
            {
                Vector3D directionVector = Base6Directions.GetVector(direction);
                Vector3D gravityForceVector = Vector3D.ProjectOnVector(ref gravity, ref directionVector);
                double gravityForce = gravityForceVector.Length();

                // If gravity force is negative then it is working on the opposite axis - set to 0
                if(gravityForce < 0)
                {
                    gravityForce = 0;
                }

                // Get thrust group for direction and apply thrust force
                thrustDirections[direction].ApplyThrustForce(gravityForce);

            }
        }
    }
}
