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

        Vector3D destinationVector = new Vector3(31273.08, -1882.8, 52451.49);
        List<ThrustGroup> thrustGroups = new List<ThrustGroup>();
        IMyShipController controller;

        public Program()
        {

            controller = GetMainRemoteControl();
            Runtime.EstablishCoroutines();
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
            // Display controller direction vector
            controller.CustomName = controller.WorldMatrix.Forward.ToString();
            controller.ShowOnHUD = true;

            while (true)
            {
                // Get thrusters and their orientations
                GetThrusters(out thrustGroups);

                // Counter any gravity influencing the ship
                CounterGravity();

                // Calculate desired destination vector
                Vector3D localDestVector = destinationVector.ConvertToLocalPosition(controller);

                // Calculate desired velocity
                Vector3D desiredVelocity = localDestVector * velocityProportionalGain;
                if (desiredVelocity.Length() > maxSpeed)
                {
                    desiredVelocity = Vector3D.Normalize(desiredVelocity) * maxSpeed;
                }

                // Apply thrust to reach desired velocity



                yield return 0;

            }
            

        }

        public IEnumerator<int> temp()
        {

            while (true)
            {
                // Calculate desired destination vector
                Vector3D localDestVector = destinationVector.ConvertToLocalPosition(controller);

                // Get thrusters
                GetThrusters(out thrustGroups);

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

                double thrustToApply = thrustGroup.CalcThrustEffectiveness(gravity) * shipMass * gravity.Length() / thrustGroup.maxEffectiveThrust;
                foreach (IMyThrust thruster in thrustGroup.thrusters)
                {
                    thruster.CustomName = thrustToApply.ToString();
                    thruster.ShowOnHUD = true;
                }

                thrustGroup.ApplyThrustPercentage(thrustToApply);

            }
        }
    }
}
