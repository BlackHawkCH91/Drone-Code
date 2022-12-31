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
            private Dictionary<Matrix, List<IMyGyro>> gyros;
            private Vector3 desiredVelocity;
            private MatrixD desiredOrientation;
            private const float speedCoefficient = 0.0001f;

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

                this.gyros = new Dictionary<Matrix, List<IMyGyro>>();
                foreach (IMyGyro gyro in gyros)
                {
                    Matrix gyroOrientation;
                    gyro.Orientation.GetMatrix(out gyroOrientation);

                    if (!this.gyros.ContainsKey(gyroOrientation))
                    {
                        this.gyros[gyroOrientation] = new List<IMyGyro>();
                    }

                    this.gyros[gyroOrientation].Add(gyro);
                }

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

            private float GetGyroSpeedPercentage(float val)
            {
                //return (float)(Math.Sign(val) * Math.Pow(Math.Abs(val), speedCoefficient));
                return (float)(Math.Sign(val) * -Math.Pow(speedCoefficient, Math.Abs(val)) + Math.Sign(val));
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

                    float pitch = -(float)shipOrientation.Backward.Dot(desiredOrientation.Up.ProjectOnPlane(shipOrientation.Right).UnitVector());
                    if(shipOrientation.Backward.Dot(desiredOrientation.Backward.ProjectOnPlane(shipOrientation.Right).UnitVector()) < 0)
                    {
                        int sign = Math.Sign(pitch);
                        if(sign == 0)
                        {
                            pitch = 1;
                        }
                        else
                        {
                            pitch = sign;
                        }
                    }

                    float yaw = -(float)shipOrientation.Right.Dot(desiredOrientation.Backward.ProjectOnPlane(shipOrientation.Up).UnitVector());
                    if (shipOrientation.Backward.Dot(desiredOrientation.Backward.ProjectOnPlane(shipOrientation.Right).UnitVector()) < 0)
                    {
                        int sign = Math.Sign(yaw);
                        if (sign == 0)
                        {
                            yaw = 1;
                        }
                        else
                        {
                            yaw = sign;
                        }
                    }

                    float roll = -(float)shipOrientation.Up.Dot(desiredOrientation.Right.ProjectOnPlane(shipOrientation.Backward).UnitVector());
                    if (shipOrientation.Backward.Dot(desiredOrientation.Backward.ProjectOnPlane(shipOrientation.Right).UnitVector()) < 0)
                    {
                        int sign = Math.Sign(roll);
                        if (roll == 0)
                        {
                            roll = 1;
                        }
                        else
                        {
                            roll = sign;
                        }
                    }

                    Vector3 rotVector = new Vector3(pitch, yaw, roll);

                    TaskScheduler.Echo("RotVector");
                    TaskScheduler.Echo(rotVector.RoundToDp(2).ToString() + "\n");

                    Vector3 rotDirection = rotVector;//.UnitVector();

                    Vector3 rotSpeed = new Vector3(GetGyroSpeedPercentage(rotDirection.X), GetGyroSpeedPercentage(rotDirection.Y), GetGyroSpeedPercentage(rotDirection.Z)); // * 3 for full gyro speed

                    TaskScheduler.Echo("RotSpeed");
                    TaskScheduler.Echo(rotSpeed.RoundToDp(2).ToString() + "\n");

                    // Apply gyro overrides
                    for(int keyPairIndex = 0; keyPairIndex < gyros.Count(); keyPairIndex++)
                    {
                        KeyValuePair<Matrix, List<IMyGyro>> keyValuePair = gyros.ElementAt(keyPairIndex);
                        Matrix gyroOrientation = keyValuePair.Key;
                        List<IMyGyro> groupedGyros = keyValuePair.Value;
                        // Convert grid control vector to local control vector
                        MatrixD localOrientation = MatrixDExtensions.ConvertToLocalOrientation(gyroOrientation, controllerOrientation);
                        Vector3 gyroControlVct = rotSpeed.ConvertToLocalDirection(localOrientation);

                        // Apply control
                        for(int i = 0; i < groupedGyros.Count(); i++)
                        {
                            IMyGyro gyro = groupedGyros[i];
                            gyro.Pitch = gyroControlVct.X;
                            gyro.Yaw = gyroControlVct.Y;
                            gyro.Roll = gyroControlVct.Z;
                        }
                    }

                    #endregion

                    yield return 0;
                }
            }

        }
    }
}
