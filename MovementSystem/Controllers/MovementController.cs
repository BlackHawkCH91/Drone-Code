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
            private static Action<string> Echo;
            private static Dictionary<Base6Directions.Axis, AxisMovementController> movementControllers = new Dictionary<Base6Directions.Axis, AxisMovementController>();
            private static Vector3D desiredPosition;
            private static double maxSpeed = 100;
            private static double timeStep;
            private static Vector3D requiredMovement;
            private static IMyShipController shipController;
            private static MyShipMass shipMass;

            private class AxisMovementController
            {
                private Base6Directions.Axis movementAxis;
                private Base6Directions.Direction baseAxisDirection;
                private Base6Directions.Axis MovementAxis { get { return movementAxis; } }
                public Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();

                private DecayingIntegralPIDController velocityController = new DecayingIntegralPIDController(1, 0, 0, 0);//, 1, -5, 0.5);
                private double errorVal;
                public double maxAxisSpeed;
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
                    //Echo($"{movementAxis}\nerror:{errorVal.RoundToDp(3)}");
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
                    accel = CalcThrustForce(accelDirection) / shipMass.TotalMass;
                    decel = CalcThrustForce(Base6Directions.GetOppositeDirection(accelDirection)) / shipMass.TotalMass;
                    double maxSpeedDistanceGain = decel / 150;
                    double minMaxSpeed = decel * 2;
                    maxDesiredSpeed = Math.Min(Math.Abs(maxSpeedDistanceGain * errorVal) + minMaxSpeed, maxAxisSpeed);

                    //Echo($"accel:{accel}, decel:{decel}");
                }
                private void ApplyThrust()
                {
                    // Calc desired velocity
                    double desiredVelocity = velocityController.GetControlValue(errorVal, timeStep);
                    if(Math.Abs(desiredVelocity) >= maxDesiredSpeed)
                    {
                        desiredVelocity = maxDesiredSpeed * Math.Sign(desiredVelocity);
                    }
                    double currVelocity = shipController.GetShipVelocities().LinearVelocity.ConvertToLocalDirection(shipController).Dot(Base6Directions.GetVector(baseAxisDirection));

                    //Echo($"currVel:{currVelocity.RoundToDp(10)}\ndesiredVel:{desiredVelocity.RoundToDp(10)}\nmaxDesiredspeed:{maxDesiredSpeed.RoundToDp(3)}\nmaxSpeed:{maxAxisSpeed.RoundToDp(3)}");

                    // Calc thrust to apply and apply thrust
                    double thrustToApply = 0;

                    if(desiredVelocity > currVelocity)
                    {
                        thrustToApply = (desiredVelocity - currVelocity) / (accel * (timeStep * 2));
                    }
                    else if(desiredVelocity < currVelocity)
                    {
                        thrustToApply = (desiredVelocity - currVelocity) / (decel * (timeStep * 2));
                    }

                    if(Math.Abs(thrustToApply) > 1)
                    {
                        thrustToApply = Math.Sign(thrustToApply);
                    }

                    //Echo($"thrustToApply:{thrustToApply.RoundToDp(5)}");
                    if (thrustToApply >= 0)
                    {
                        foreach (IMyThrust thruster in thrusters[baseAxisDirection])
                        {
                            thruster.ThrustOverridePercentage = (float)Math.Abs(thrustToApply);
                        }
                        foreach (IMyThrust thruster in thrusters[Base6Directions.GetOppositeDirection(baseAxisDirection)])
                        {
                            thruster.ThrustOverridePercentage = 0;
                        }
                    }
                    else
                    {
                        foreach (IMyThrust thruster in thrusters[Base6Directions.GetOppositeDirection(baseAxisDirection)])
                        {
                            thruster.ThrustOverridePercentage = (float)Math.Abs(thrustToApply);
                        }
                        foreach (IMyThrust thruster in thrusters[baseAxisDirection])
                        {
                            thruster.ThrustOverridePercentage = 0;
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
                    directionalThrusters[Base6Directions.GetOppositeDirection(thruster.Orientation.Forward).ConvertToLocal(shipController)].Add(thruster);
                }

                return directionalThrusters;
            }
            private static IEnumerator<int> StepMovementCoroutine()
            {
                while (true)
                {
                    // Calc non-per axis stuffs
                    shipMass = shipController.CalculateShipMass();
                    timeStep = Runtime.TimeSinceLastRun.TotalSeconds;
                    requiredMovement = desiredPosition.ConvertToLocalPosition(shipController);

                    // Calc per axis stuffs
                    foreach (KeyValuePair<Base6Directions.Axis, AxisMovementController> keyValuePair in movementControllers)
                    {
                        Base6Directions.Axis axis = keyValuePair.Key;
                        AxisMovementController movementController = keyValuePair.Value;
                        movementController.maxAxisSpeed = Math.Abs(requiredMovement.Dot(Base6Directions.GetVector(Base6Directions.GetBaseAxisDirection(axis)))) * maxSpeed;
                        movementController.StepMovement();
                    }
                    yield return 0;
                }
            }
            private static void Run()
            {
                Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters = GetThrusters();
                // Create axis movement controller for all directions and assign thrusters
                foreach (Base6Directions.Axis axis in Enum.GetValues(typeof(Base6Directions.Axis)))
                {
                    AxisMovementController axisMovementController = new AxisMovementController(axis);
                    foreach (KeyValuePair<Base6Directions.Direction,List<IMyThrust>> thrustListPair in thrusters)
                    {
                        if(Base6Directions.GetAxis(thrustListPair.Key) == axis)
                        {
                            axisMovementController.thrusters.Add(thrustListPair.Key, thrustListPair.Value);
                        }
                    }
                    movementControllers.Add(axis, axisMovementController);
                }
                TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(StepMovementCoroutine));
            }
            public static void Establish(IMyGridTerminalSystem gridTerminalSystem, IMyGridProgramRuntimeInfo runtime, Action<string> echo, IMyShipController ShipController)
            {
                GridTerminalSystem = gridTerminalSystem;
                Runtime = runtime;
                Echo = echo;
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
