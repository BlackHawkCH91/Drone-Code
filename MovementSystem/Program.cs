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

/*
 * MOVEMENT CONTROLLER STEPS:
 * 1. CALC DESIRED VELOCITY
 * 2. GET THRUSTERS
 * 3. CALC DESIRED THRUST TO HIT DESIRED VELOCITY
 * 4. REPEAT
 */

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // Velocity Controller Vars
        /*        double maxSpeed = 100;
                double velocityProportionalGain = 0.1;*/

        // Controller vars
        double PGain = 0;
        double DGain = 0;
        double IGain = 0;

        Vector3D destinationVector = new Vector3(11749.23, -81914.21, -86689.26);
        List<ThrustGroup> thrustGroups = new List<ThrustGroup>();
        IMyShipController controller;

        public Program()
        {

            controller = GetMainRemoteControl();
            Runtime.EstablishTaskScheduler(Echo);
            TaskScheduler.ResumeCoroutine(TaskScheduler.CreateCoroutine(new Func<IEnumerator<int>>(MoveCoroutine)));

        }

        public void Save()
        {



        }

        public void Main(string argument, UpdateType updateSource)
        {

            TaskScheduler.StepCoroutines(updateSource);

        }


        public IEnumerator<int> MoveControllerCoroutine()
        {

            Vector3D errorVal = destinationVector.ConvertToLocalPosition(controller);

            // Get P control value
            Vector3D PVal = errorVal * PGain;

            // Get I control value
            Vector3D IVal = errorVal * IGain;

            // Get D control value
            Vector3D DVal = ;


            /*// Display controller direction vector
            controller.CustomName = controller.WorldMatrix.Forward.ToString();
            controller.ShowOnHUD = true;

            while (true)
            {
                Vector3D desiredVelocityChange = CalcDesiredVelocity() - controller.GetShipVelocities().LinearVelocity;

                // Calculate max available thrusts for all thrust groups in the direction of the desired change in velocity
                Dictionary<ThrustGroup, double> thrustAmounts = new Dictionary<ThrustGroup, double>();
                foreach(ThrustGroup thrustGroup in thrustGroups)
                {
                    double availableThrustAlongVelocity = thrustGroup.CalcThrustEffectiveness(velocityDifference) * thrustGroup.availableThrust;
                    
                    if(availableThrustAlongVelocity >= 0)
                    {
                        thrustAmounts.Add(thrustGroup, availableThrustAlongVelocity);
                    }

                }

                foreach (KeyValuePair<ThrustGroup, double> Entry in thrustAmounts)
                {
                    ThrustGroup thrustGroup = Entry.Key;
                    thrustGroup.ApplyThrustPercentage(thrustGroup.currentThrustPercentage + minPossibleThrust / thrustGroup.maxEffectiveThrust);
                }
                
                yield return 0;

            }*/
            

        }

/*        // Desired velocity controlled by P controller
        public Vector3D CalcDesiredVelocity()
        {
            Vector3D currVelocity = controller.GetShipVelocities().LinearVelocity;

            // Calculate desired destination vector
            Vector3D localDestVector = destinationVector.ConvertToLocalPosition(controller);

            // Calculate velocity difference between desired velocity and current velocity
            Vector3D desiredVelocity = localDestVector * velocityProportionalGain;
            if (desiredVelocity.Length() > maxSpeed)
            {
                desiredVelocity = Vector3D.Normalize(desiredVelocity) * maxSpeed;
            }

            return desiredVelocity;
        }

        public Vector3D CalcDesiredThrust(Vector3D desiredVelocityChange)
        {
            // Get thrusters and their orientations
            GetThrusters(out thrustGroups);

            // Go through each thrust group and check if it can apply thrust for this velocity
            List<ThrustGroup> effectiveThrustGroups = new List<ThrustGroup>();
            foreach(ThrustGroup thrustGroup in thrustGroups)
            {
                if (thrustGroup.CanApplyThrust(desiredVelocityChange))
                {
                    effectiveThrustGroups.Add(thrustGroup);
                }
            }


        }*/

        public IMyShipController GetMainRemoteControl()
        {
            // Get all remote control objects
            List<IMyRemoteControl> remoteControls = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(remoteControls);

            // If main controller is set then return this
            foreach(IMyRemoteControl controller in remoteControls)
            {
                if (controller.IsMainCockpit)
                {
                    return controller;
                }
            }

            // If there is no main controller return first controller in list
            return remoteControls[0];

            // If there are no remote controls throw exception
            throw new Exception("No remote controls found");
        }

        public void GetThrusters(out List<ThrustGroup> ThrustGroupArray)
        {
            // Clear any previous thruster groups
            ThrustGroupArray = new List<ThrustGroup>();

            // Get all thrusters
            List<IMyThrust> allThrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(allThrusters);

            // Adding thrusters to groups depending on their direction
            foreach (IMyThrust thruster in allThrusters)
            {
                // Get thrust direction of thruster by using worldmatrix.backwards
                Vector3D thrustDirection = thruster.WorldMatrix.Backward;

                bool thrustGroupFound = false;
                // Check if a thrust group for this direction exists, if it does then add this thruster to it and stop checking directions
                foreach(ThrustGroup thrustGroup in ThrustGroupArray)
                {
                    if(thrustGroup.thrustDirection == thrustDirection)
                    {
                        thrustGroup.AddThruster(thruster);
                        thrustGroupFound = true;
                        break;
                    }
                }

                // If there is not a thrust group for this direction, create a new one using this thruster
                if (!thrustGroupFound)
                {
                    ThrustGroupArray.Add(new ThrustGroup(thrustDirection, thruster));
                }

            }

        }

        public void CounterGravity()
        {
            // Get gravity and mass
            Vector3D gravity = controller.GetNaturalGravity();
            double shipMass = controller.CalculateShipMass().PhysicalMass;

            // Apply thrusts
            foreach (ThrustGroup thrustGroup in thrustGroups)
            {

                thrustGroup.ApplyThrustPercentage(thrustGroup.CalcThrustEffectiveness(gravity) * shipMass * gravity.Length() / thrustGroup.maxEffectiveThrust);

            }
        }

    }
}
