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
            public Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
            private List<IMyGyro> gyros;
            private Vector3 desiredVelocity;
            private Quaternion desiredRotation;

            public Vector3 DesiredVelocity {
                get { return desiredVelocity; }
                set { desiredVelocity = value; }
            }
            public Quaternion DesiredRotation
            {
                get { return desiredRotation; }
                set { desiredRotation = value; }
            }

            public ShipController(IMyShipController controller, List<IMyThrust> thrusters, List<IMyGyro> gyros)
            {
                this.controller = controller;
                this.gyros = gyros;

                // Make list of thrusters for each direction
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
                Vector3 currVelocity = (Vector3)controller.GetShipVelocities().LinearVelocity.ConvertToLocalDirection(controller);
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
                    for(int i = 0; i < thrusters[direction].Count; i++)
                    {
                        thrusters[direction][i].ThrustOverridePercentage = thrustToApply;
                    }
                }

                // Update gyros
                
                yield return 0;
            }

        }
    }
}
