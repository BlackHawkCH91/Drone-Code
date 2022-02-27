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
        public static class MovementController
        {
            private static IMyGridTerminalSystem GridTerminalSystem;
            private static IMyGridProgramRuntimeInfo Runtime;
            private static List<AxisMovementController> movementControllers = new List<AxisMovementController>();
            private static Vector3D desiredPosition;
            private static double maxSpeed = 500;
            private static double timeStep;
            private static Vector3D requiredMovement;
            private static IMyShipController shipController;

            private class AxisMovementController
            {
                private Base6Directions.Axis movementAxis;
                private Base6Directions.Direction baseAxisDirection;
                private Base6Directions.Axis MovementAxis { get { return movementAxis; } }
                private Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();

                private DecayingIntegralPIDController velocityController = new DecayingIntegralPIDController(2, 1, -10, 0.5);
                private double errorVal;
                private double maxDesiredSpeed;
                private double accel;
                private double decel;
                private Base6Directions.Direction accelDirection;

                private double CalcThrustForce(Base6Directions.Direction direction)
                {
                    double thrustForceSum = 0;
                    foreach (IMyThrust thruster in thrusters[direction])
                    {
                        thrustForceSum += thruster.MaxEffectiveThrust;
                    }

                    return thrustForceSum;
                }
                private void RecalcError()
                {
                    errorVal = requiredMovement.Dot(Base6Directions.GetVector(baseAxisDirection));
                }
                private void RecalcGains()
                {
                    
                    if (errorVal >= 0)
                    {
                        accelDirection = baseAxisDirection;
                    }
                    else
                    {
                        accelDirection = Base6Directions.GetOppositeDirection(baseAxisDirection);
                    }
                    accel = CalcThrustForce(accelDirection) / shipController.CalculateShipMass().TotalMass;
                    decel = CalcThrustForce(Base6Directions.GetOppositeDirection(accelDirection)) / shipController.CalculateShipMass().TotalMass;
                    double maxSpeedDistanceGain = decel / 300;
                    double minMaxSpeed = decel / 1.5;
                    maxDesiredSpeed = Math.Min(Math.Abs(maxSpeedDistanceGain * errorVal) + minMaxSpeed, maxSpeed);
                }
                private void ApplyThrust()
                {
                    // Calc desired velocity
                    double desiredVelocity = velocityController.GetControlValue(errorVal, timeStep);
                    if(Math.Abs(desiredVelocity) >= maxDesiredSpeed)
                    {
                        desiredVelocity = maxDesiredSpeed * Math.Sign(desiredVelocity);
                    }
                    double currVelocity = shipController.GetShipVelocities().LinearVelocity.Dot(Base6Directions.GetVector(baseAxisDirection));

                    // Calc thrust to apply and apply thrust
                    double thrustToApply = 0;
                    if(desiredVelocity - currVelocity >= 0)
                    {
                        thrustToApply = Math.Min(Math.Abs((desiredVelocity - currVelocity) / accel * timeStep), 1);
                        foreach(IMyThrust thruster in thrusters[accelDirection])
                        {
                            thruster.ThrustOverridePercentage = (float)thrustToApply;
                        }
                    }
                    else
                    {
                        thrustToApply = Math.Min(Math.Abs((desiredVelocity - currVelocity) / decel * timeStep), -1);
                        foreach (IMyThrust thruster in thrusters[Base6Directions.GetOppositeDirection(accelDirection)])
                        {
                            thruster.ThrustOverridePercentage = (float)thrustToApply;
                        }
                    }
                }
                public void StepMovement()
                {
                    RecalcError();
                    RecalcGains();
                    ApplyThrust();
                }

                public AxisMovementController(Base6Directions.Axis MovementAxis)
                {
                    movementAxis = MovementAxis;
                    baseAxisDirection = Base6Directions.GetBaseAxisDirection(MovementAxis);
                }
            }
            private static Dictionary<Base6Directions.Direction, List<IMyThrust>> GetThrusters()
            {
                // Get all thrusters
                List<IMyThrust> allThrusters = new List<IMyThrust>();
                GridTerminalSystem.GetBlocksOfType<IMyThrust>(allThrusters);

                // Create dictionary for thrusters to be organised into depending on which way they are facing
                Dictionary<Base6Directions.Direction, List<IMyThrust>> directionalThrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
                foreach (Base6Directions.Direction direction in Enum.GetValues(typeof(Base6Directions.Direction)))
                {
                    directionalThrusters.Add(direction, new List<IMyThrust>());
                }

                // Organise each thruster into the dictionary
                foreach(IMyThrust thruster in allThrusters)
                {
                    // Using opposite direction as thrust direction is opposite to thruster block orientation (thrust comes out back of thruster)
                    directionalThrusters[Base6Directions.GetOppositeDirection(thruster.Orientation.Forward)].Add(thruster);
                }

                return directionalThrusters;
            }
            private static IEnumerator<int> StepMovementCoroutine()
            {
                timeStep = Runtime.TimeSinceLastRun.TotalSeconds;
                while (true)
                {
                    requiredMovement = desiredPosition.ConvertToLocalPosition(shipController);
                    foreach (AxisMovementController controller in movementControllers)
                    {
                        controller.StepMovement();
                    }
                    yield return 0;
                }
            }
            private static void Run()
            {
                // Create axis movement controller for all directions
                foreach (Base6Directions.Axis axis in Enum.GetValues(typeof(Base6Directions.Axis)))
                {
                    movementControllers.Add(new AxisMovementController(axis));
                }
                TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(StepMovementCoroutine));
            }
            public static void Establish(IMyGridTerminalSystem gridTerminalSystem, IMyGridProgramRuntimeInfo runtime, IMyShipController ShipController)
            {
                GridTerminalSystem = gridTerminalSystem;
                Runtime = runtime;
                shipController = ShipController;
                desiredPosition = ShipController.WorldMatrix.Translation;
                Run();
            }
            public static void MoveTo(Vector3D worldPosition)
            {
                desiredPosition = worldPosition;
            }
        }
    }
}
