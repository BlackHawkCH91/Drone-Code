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
        /// <summary>
        /// Controls ship movement and rotation through setting desired velocity and desired rotation
        /// </summary>
        public class ShipController
        {
            private enum RotationAxis
            {
                Yaw,
                Pitch,
                Roll
            }

            private IMyShipController controller;
            private Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
            private List<IMyGyro> gyros;
            private Vector3 desiredVelocity;
            private MatrixD desiredOrientation;

            public Vector3 DesiredVelocity {
                get { return desiredVelocity; }
                set { desiredVelocity = value; }
            }
            public MatrixD DesiredOrientation
            {
                get { return desiredOrientation; }
                set { desiredOrientation = value; }
            }

            public ShipController(IMyShipController controller, List<IMyThrust> thrusters, List<IMyGyro> gyros)
            {
                this.controller = controller;
                this.gyros = gyros;

                // Make list of thrusters and gyros for each direction
                foreach (Base6Directions.Direction direction in Enum.GetValues(typeof(Base6Directions.Direction)))
                {
                    this.thrusters.Add(direction, new List<IMyThrust>());
                }

                // Sort thrusters
                for (int i = 0; i < thrusters.Count; i++)
                {
                    IMyThrust thruster = thrusters[i];
                    this.thrusters[Base6Directions.GetOppositeDirection(thruster.Orientation.Forward).ConvertToLocal(controller)].Add(thruster);
                }

                // Start controlling
                TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(StepMovements));
            }

            public IEnumerator<int> StepMovements()
            {
                while (true)
                {

                    #region Update thrusters
                    Vector3 currVelocity = controller.GetShipVelocities().LinearVelocity.ConvertToLocalDirection(controller);
                    float mass = controller.CalculateShipMass().TotalMass;

                    // Update thrusters
                    foreach (Base6Directions.Direction direction in Base6Directions.EnumDirections)
                    {
                        float thrust = 0f;
                        foreach (IMyThrust thruster in thrusters[direction])
                        {
                            thrust += thruster.MaxEffectiveThrust;
                        }

                        // Calculate velocities for direction
                        float dirDesiredVelocity = desiredVelocity.Dot(Base6Directions.GetVector(direction));
                        float dirCurrVelocity = currVelocity.Dot(Base6Directions.GetVector(direction));

                        // Calc accel in this direction
                        float accel = thrust / mass;

                        // Calc thrust to apply and apply thrust
                        float thrustToApply = 0;

                        if (dirDesiredVelocity > dirCurrVelocity)
                        {
                            // Thrust is set to a range within 0 and 1
                            thrustToApply = Math.Min((dirDesiredVelocity - dirCurrVelocity) / (accel * ((float)TaskScheduler.TimeStep * 2)), 1);
                        }
                        else if (dirDesiredVelocity < dirCurrVelocity)
                        {
                            thrustToApply = 0;
                        }

                        // Apply thrust value to all thrusters in this direction
                        for (int i = 0; i < thrusters[direction].Count; i++)
                        {
                            thrusters[direction][i].ThrustOverridePercentage = thrustToApply;
                        }
                    }
                    #endregion

                    #region Update gyros
                    Matrix temp;
                    controller.Orientation.GetMatrix(out temp);
                    MatrixD controllerOrientation = temp;
                    MatrixD shipOrientation = controller.WorldMatrix.GetOrientation();

                    MatrixD orientationError = desiredOrientation.ConvertToLocalOrientation(controllerOrientation);

                    float yaw = (float)shipOrientation.Backward.Dot(orientationError.Backward.ProjectOnPlane(shipOrientation.Up).UnitVector());
                    float pitch = (float)shipOrientation.Up.Dot(orientationError.Up.ProjectOnPlane(shipOrientation.Right).UnitVector());
                    float roll = (float)shipOrientation.Right.Dot(orientationError.Right.ProjectOnPlane(shipOrientation.Backward).UnitVector());

                    Vector3 rotVector = new Vector3(pitch, yaw, roll);

                    TaskScheduler.Echo("RotVector");
                    TaskScheduler.Echo(rotVector.RoundToDp(2).ToString() + "\n");

                    TaskScheduler.Echo("Controller");
                    TaskScheduler.Echo(controllerOrientation.Right.RoundToDp(2).ToString());
                    TaskScheduler.Echo(controllerOrientation.Up.RoundToDp(2).ToString());
                    TaskScheduler.Echo(controllerOrientation.Backward.RoundToDp(2).ToString() + "\n");

                    TaskScheduler.Echo("ShipOrientation");
                    TaskScheduler.Echo(shipOrientation.Right.RoundToDp(2).ToString());
                    TaskScheduler.Echo(shipOrientation.Up.RoundToDp(2).ToString());
                    TaskScheduler.Echo(shipOrientation.Backward.RoundToDp(2).ToString() + "\n");

                    TaskScheduler.Echo("DesiredOrientation");
                    TaskScheduler.Echo(desiredOrientation.Right.RoundToDp(2).ToString());
                    TaskScheduler.Echo(desiredOrientation.Up.RoundToDp(2).ToString());
                    TaskScheduler.Echo(desiredOrientation.Backward.RoundToDp(2).ToString() + "\n");

                    TaskScheduler.Echo("LocalDesiredOrientation");
                    TaskScheduler.Echo(orientationError.Right.RoundToDp(2).ToString());
                    TaskScheduler.Echo(orientationError.Up.RoundToDp(2).ToString());
                    TaskScheduler.Echo(orientationError.Backward.RoundToDp(2).ToString() + "\n");

                    // Normalize rotation to ensure that shortest rotation path is followed at all times
                    Vector3 rotSpeed = rotVector.UnitVector() * rotVector.Length(); // * 3 for full gyro speed

                    // Apply gyro overrides
                    for (int i = 0; i < gyros.Count(); i++)
                    {
                        IMyGyro gyro = gyros[i];
                        
                        // Get gyro orientation
                        Matrix gyroOrientation;
                        gyro.Orientation.GetMatrix(out gyroOrientation);

                        // Convert grid control vector to local control vector
                        MatrixD localOrientation = MatrixDExtensions.ConvertToLocalOrientation(gyroOrientation, controllerOrientation);
                        Vector3 gyroControlVct = rotSpeed.ConvertToLocalDirection(localOrientation);

                        // Apply control
                        gyro.Pitch = gyroControlVct.X;
                        gyro.Yaw = gyroControlVct.Y;
                        gyro.Roll = gyroControlVct.Z;
                    }

                    #endregion

                    yield return 0;
                }
            }

        }
    }
}
