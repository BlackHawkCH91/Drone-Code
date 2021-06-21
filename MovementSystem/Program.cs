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
        double MaxSpeed = 100;
        double VelocityProportionalGain = 0.125;

        Vector3D DestinationVector = new Vector3(-37806.02, -36183.24, -36770.72);

        List<IMyThrust> AllThrusters = new List<IMyThrust>();

        float[] MaxThrustInDirections = new float[6];
        List<IMyThrust>[] ThrustersInDirections = new List<IMyThrust>[6];

        /*
        * THRUSTER INDEXES:
        * 0 - FORWARD
        * 1 - BACKWARD
        * 2 - LEFT
        * 3 - RIGHT
        * 4 - UP
        * 5 - DOWN
        */


        IMyShipController Controller;


        public Program()
        {
            Controller = (IMyShipController)GridTerminalSystem.GetBlockWithName("PrimaryController");
            Runtime.EstablishCoroutines();
        }

        public void Save()
        {



        }

        public void Main(string argument, UpdateType updateSource)
        {
            Coroutine.StepCoroutines(updateSource);


            GetThrusters();
            CalcMaxEffectiveThrusts();

            // calc local direction to destination
            Vector3D Gravity = Controller.GetNaturalGravity().ConvertToLocalDirection(Controller);
            Vector3D LocalDestVector = DestinationVector.ConvertToLocalPosition(Controller);
            Vector3D DirectionToDestination = Vector3D.Normalize(LocalDestVector);

            Echo(LocalDestVector.X.ToString());
            Echo(LocalDestVector.Y.ToString());
            Echo(LocalDestVector.Z.ToString());
            Echo(LocalDestVector.Length().ToString());

            Echo(Gravity.X.ToString());
            Echo(Gravity.Y.ToString());
            Echo(Gravity.Z.ToString());
            Echo(Gravity.Length().ToString());

            //DirectionToDestination = new Vector3D(1, 0, 0);

            // IN LOCAL SPACE:
            // -Z is forward, +Z is backward
            // +Y is Up, -Y is down
            // +X is right, -X is left

            // Use PD control to figure out what position change is wanted
            // Error value is local dest vector

            // Get current velocity
            Vector3D CurrentLinearVelocity = Controller.GetShipVelocities().LinearVelocity.ConvertToLocalDirection(Controller);

            double PossibleAcceleration = GetMinThrustForce() / Controller.CalculateShipMass().TotalMass;

            Echo(PossibleAcceleration.ToString());

            // use PD control control ship velocity
            // desired velocity is proportional to distance from the destination

            Vector3D DesiredVelocityVector = (LocalDestVector /*+ Gravity*/) * VelocityProportionalGain;

            if (DesiredVelocityVector.Length() > MaxSpeed)
            {
                DesiredVelocityVector = Vector3D.Normalize(DesiredVelocityVector) * MaxSpeed;
            }



            // Thrust vector needs to be P controlled so as the ship approaches the desired velocity thrust decreases
            Vector3D ThrustVector = DesiredVelocityVector - CurrentLinearVelocity;

            if (ThrustVector.Length() > 1)
            {
                ThrustVector = Vector3D.Normalize(ThrustVector);
            }

            ApplyThrust(ThrustVector);

        }


        public void CalcMaxEffectiveThrusts()
        {
            for (int DirectionIndex = 0; DirectionIndex < 6; DirectionIndex++)
            {

                MaxThrustInDirections[DirectionIndex] = 0;

                foreach (IMyThrust Thruster in ThrustersInDirections[DirectionIndex])
                {

                    MaxThrustInDirections[DirectionIndex] += Thruster.MaxEffectiveThrust;

                }

            }
        }

        public void GetThrusters()
        {
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(AllThrusters);

            // Create lists at each index for thrust directions array
            for (int i = 0; i < 6; i++)
            {
                ThrustersInDirections[i] = new List<IMyThrust>();
            }

            // Adding thrusters to lists depending on their direction
            foreach (IMyThrust Thruster in AllThrusters)
            {

                foreach (Base6Directions.Direction Direction in Enum.GetValues(typeof(Base6Directions.Direction)))
                {

                    if (Base6Directions.GetOppositeDirection(Thruster.Orientation.Forward) == Controller.Orientation.TransformDirection(Direction))
                    {

                        ThrustersInDirections[(int)Direction].Add(Thruster);
                        break;

                    }

                }

            }
        }

        public void ApplyThrust(Vector3D ThrustVector)
        {

            List<int> DirectionIndexesToUse = new List<int>();

            // Get direction indexes to use
            if (ThrustVector.Z > 0)
            {
                DirectionIndexesToUse.Add((int)Base6Directions.Direction.Backward);
            }
            else if ((ThrustVector.Z < 0))
            {
                DirectionIndexesToUse.Add((int)Base6Directions.Direction.Forward);
            }

            if (ThrustVector.X > 0)
            {
                DirectionIndexesToUse.Add((int)Base6Directions.Direction.Right);
            }
            else if ((ThrustVector.X < 0))
            {
                DirectionIndexesToUse.Add((int)Base6Directions.Direction.Left);
            }

            if (ThrustVector.Y > 0)
            {
                DirectionIndexesToUse.Add((int)Base6Directions.Direction.Up);
            }
            else if ((ThrustVector.Y < 0))
            {
                DirectionIndexesToUse.Add((int)Base6Directions.Direction.Down);
            }

            // Calculate minimum thrust out of all directions being used
            float MinThrustValue = float.MaxValue;
            foreach (int Index in DirectionIndexesToUse)
            {
                if (MaxThrustInDirections[Index] < MinThrustValue)
                {
                    MinThrustValue = MaxThrustInDirections[Index];
                }
            }

            // Check that minthrustvalue has been changed otherwise it must be 0
            if (MinThrustValue == float.MaxValue)
            {
                MinThrustValue = 0;
            }

            // Apply Overrides
            foreach (int DirectionIndex in DirectionIndexesToUse)
            {

                double DirectionMovePercent = 0;
                if (DirectionIndex == (int)Base6Directions.Direction.Forward || DirectionIndex == (int)Base6Directions.Direction.Backward)
                {
                    DirectionMovePercent = Math.Abs(ThrustVector.Z);
                }
                else if (DirectionIndex == (int)Base6Directions.Direction.Left || DirectionIndex == (int)Base6Directions.Direction.Right)
                {
                    DirectionMovePercent = Math.Abs(ThrustVector.X);
                }
                else if (DirectionIndex == (int)Base6Directions.Direction.Up || DirectionIndex == (int)Base6Directions.Direction.Down)
                {
                    DirectionMovePercent = Math.Abs(ThrustVector.Y);
                }

                double OverridePercentage = DirectionMovePercent * (MinThrustValue / MaxThrustInDirections[DirectionIndex]);

                foreach (IMyThrust Thruster in ThrustersInDirections[DirectionIndex])
                {

                    Thruster.ThrustOverridePercentage = (float)OverridePercentage;

                }
            }

            // Clear thruster overrides for thrusters not in use
            foreach (Base6Directions.Direction Direction in Enum.GetValues(typeof(Base6Directions.Direction)))
            {

                int DirectionIndex = (int)Direction;

                // Check if index is used
                bool IndexInUse = false;
                foreach (int Index in DirectionIndexesToUse)
                {

                    if (DirectionIndex == Index)
                    {

                        IndexInUse = true;

                    }

                }

                // If index is not in use then clear thruster overrides
                if (!IndexInUse)
                {

                    foreach (IMyThrust Thruster in ThrustersInDirections[DirectionIndex])
                    {

                        Thruster.ThrustOverridePercentage = 0;

                    }

                }

            }


        }

        public double GetMinThrustForce()
        {
            // Calculate minimum thrust out of all directions being used
            float MinThrustValue = float.MaxValue;
            foreach (Base6Directions.Direction Direction in Enum.GetValues(typeof(Base6Directions.Direction)))
            {
                int Index = (int)Direction;

                if (MaxThrustInDirections[Index] < MinThrustValue)
                {
                    MinThrustValue = MaxThrustInDirections[Index];
                }
            }

            // Check that minthrustvalue has been changed otherwise it must be 0
            if (MinThrustValue == float.MaxValue)
            {
                MinThrustValue = 0;
            }

            return MinThrustValue;
        }

    }
}
