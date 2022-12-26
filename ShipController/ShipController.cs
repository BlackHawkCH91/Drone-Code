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
            private Array directions = Enum.GetValues(typeof(Base6Directions.Direction));

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

                    // Update gyros
                    // Calc rotation error
                    //MatrixD rotMatrix = desiredRotation - controller.WorldMatrix.GetOrientation();
                    //Vector3 requiredRotation = new Vector3(1, 0, 0); //rotMatrix.GetTaitBryanAnglesZYX();

                    // yaw pitch roll
                    Vector3D requiredRotation = new Vector3();/*new Vector3(
                        desiredOrientation.Forward.ConvertToLocalDirection(controller).X,
                        0,
                        0
                        );*/

                    // Calculate desired angular momentum

                    // Calc grid override
                    //Vector3 gridOverride = requiredRotation.UnitVector();

                    // Apply gyro overrides
                    for (int i = 0; i < gyros.Count(); i++)
                    {
                        IMyGyro gyro = gyros[i];
                        Matrix blockOrientation = new Matrix();
                        gyro.Orientation.GetMatrix(out blockOrientation);

                        //MatrixD inverse = controller.WorldMatrix;
                        //MatrixD.Invert(inverse);
                        //MatrixD refMatrix = gyro.WorldMatrix.GetOrientation() * desiredOrientation;

                        //requiredRotation = -refMatrix.GetTaitBryanAnglesZYX();
                        //MatrixD.GetEulerAnglesXYZ(ref refMatrix, out requiredRotation);

                        //Vector3 gyroOverride = requiredRotation;//gridOverride.ConvertToLocalDirection(orientation);

                        float yaw;
                        float pitch;
                        float roll;

                        yaw = (float) controller.WorldMatrix.Forward.Dot(desiredOrientation.Forward.ProjectOnPlane(controller.WorldMatrix.Up).UnitVector());
                        pitch = (float) controller.WorldMatrix.Up.Dot(desiredOrientation.Up.ProjectOnPlane(controller.WorldMatrix.Left).UnitVector());
                        roll = (float) controller.WorldMatrix.Left.Dot(desiredOrientation.Left.ProjectOnPlane(controller.WorldMatrix.Forward).UnitVector());


                        gyro.Yaw = yaw * 3;
                        gyro.Pitch = pitch * 3;
                        gyro.Roll = roll * 3;

                        //gyro.Yaw = (float)refMatrix.Up.Dot(gyro.WorldMatrix.Up);
                        //gyro.Pitch = (float)refMatrix.Forward.Dot(gyro.WorldMatrix.Forward);//gyroOverride.Y;
                        //gyro.Roll = 0; //gyroOverride.Z;
                    }


                    yield return 0;
                }
            }

        }
    }
}
