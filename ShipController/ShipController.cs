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
            #region fields
            private IMyShipController shipController;
            private GyroController gyroController;
            private ThrustController thrustController;
            private MatrixD desiredWorldMatrix;
            #endregion

            #region properties
            public MatrixD DesiredWorldMatrix
            {
                get { return desiredWorldMatrix; }
                set 
                { 
                    desiredWorldMatrix = value;
                    gyroController.DesiredOrientation = value.GetOrientation();
                    thrustController.DesiredPosition = value.Translation;
                }
            }
            #endregion

            #region constructors
            public ShipController(IMyShipController controller, List<IMyThrust> thrusters, List<IMyGyro> gyros)
            {
                this.shipController = controller;
                this.gyroController = new GyroController(controller, gyros);
                this.thrustController = new ThrustController(controller, thrusters);

                TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(Update));
            }
            #endregion

            #region methods
            public IEnumerator<int> Update()
            {
                while (true)
                {
                    gyroController.Update();
                    thrustController.Update();
                    yield return 0;
                }
            }
            #endregion


            #region OLD
            /*
            private enum RotationAxis
            {
                Yaw,
                Pitch,
                Roll
            }

            private IMyShipController controller;
            private MatrixD controllerOrientation;
            private Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
            private Dictionary<Matrix, List<IMyGyro>> gyros;
            private Vector3 desiredVelocity;
            private MatrixD desiredOrientation;
            private const float significantDistanceThreshold = .1f;

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
                Matrix temp;
                controller.Orientation.GetMatrix(out temp);
                this.controllerOrientation = temp;

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
                    MatrixD shipOrientation = controller.WorldMatrix.GetOrientation();

                    float pitch = -(float)shipOrientation.Backward.Dot(desiredOrientation.Up.ProjectOnPlane(shipOrientation.Right).UnitVector());
                    float yaw = -(float)shipOrientation.Right.Dot(desiredOrientation.Backward.ProjectOnPlane(shipOrientation.Up).UnitVector());
                    float roll = -(float)shipOrientation.Up.Dot(desiredOrientation.Right.ProjectOnPlane(shipOrientation.Backward).UnitVector());
                    Vector3 rotVector = new Vector3(pitch, yaw, roll);

                    // Check if there are any insignificant rotations and if so, remove them
                    float max = Math.Abs(rotVector.AbsMax());
                    if (Math.Abs(rotVector.X) < max * significantDistanceThreshold)
                    {
                        rotVector.X = 0;
                    }
                    if (Math.Abs(rotVector.Y) < max * significantDistanceThreshold)
                    {
                        rotVector.Y = 0;
                    }
                    if (Math.Abs(rotVector.Z) < max * significantDistanceThreshold)
                    {
                        rotVector.Z = 0;
                    }

                    TaskScheduler.Echo("RotVector");
                    TaskScheduler.Echo(rotVector.RoundToDp(2).ToString() + "\n");
                    
                    double vectorDistance = shipOrientation.Backward.Dot(desiredOrientation.Backward);
                    if(Math.Abs(vectorDistance) > 1)
                    {
                        vectorDistance = Math.Sign(vectorDistance);
                    }

                    // If facing the wrong half of the sphere then rotate at full speed
                    float rotDistance = (float)(Math.Acos(vectorDistance) / Math.PI);
                    if(rotDistance > 0.5)
                    {
                        rotVector = rotVector.UnitVector();
                    }

                    Vector3 rotSpeed = rotVector * 3; // * 3 for full gyro speed

                    TaskScheduler.Echo("RotSpeed");
                    TaskScheduler.Echo(rotSpeed.RoundToDp(2).ToString() + "\n");

                    TaskScheduler.Echo("AngularVelocity");
                    TaskScheduler.Echo(controller.GetShipVelocities().AngularVelocity.ConvertToLocalDirection(controller).RoundToDp(2).ToString() + "\n");

                    // Apply gyro overrides
                    for (int keyPairIndex = 0; keyPairIndex < gyros.Count(); keyPairIndex++)
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
            */
            #endregion
        }
    }
}
