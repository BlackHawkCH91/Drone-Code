using IngameScript;
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
        public class ThrustController
        {
            #region fields
            private Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters;
            private IMyShipController controller;
            private Vector3 desiredPosition;
            #endregion

            #region properties
            /// <summary>
            /// Thrust in the directions forward, up, right
            /// </summary>
            public Vector3 MaxPositiveThrust 
            {
                get
                {
                    return new Vector3(GetThrustInDirection(controller.Orientation.Forward), GetThrustInDirection(controller.Orientation.Up), GetThrustInDirection(controller.Orientation.Left.OppositeDirection()));
                } 
            }

            /// <summary>
            /// Thrust in the directions backward, down, left
            /// </summary>
            public Vector3 MaxNegativeThrust
            { 
                get
                {
                    return new Vector3(GetThrustInDirection(controller.Orientation.Forward.OppositeDirection()), GetThrustInDirection(controller.Orientation.Up.OppositeDirection()), GetThrustInDirection(controller.Orientation.Left));
                } 
            }
            public Dictionary<Base6Directions.Direction, List<IMyThrust>> Thrusters { get { return thrusters; } }
            public IMyShipController Controller { get { return controller; } }
            public Vector3 DesiredPosition 
            { 
                get { return desiredPosition; }
                set { desiredPosition = value; }
            }
            #endregion

            #region constructors
            public ThrustController(IMyShipController controller, List<IMyThrust> thrusters)
            {
                this.controller = controller;
                this.thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
                this.desiredPosition = controller.WorldMatrix.Translation;

                for(int i = 0; i < thrusters.Count; i++)
                {
                    IMyThrust thruster = thrusters[i];
                    Base6Directions.Direction thrustDir = thruster.Orientation.Forward.OppositeDirection();

                    if (!this.thrusters.ContainsKey(thrustDir))
                    {
                        this.thrusters[thrustDir] = new List<IMyThrust>();
                    }

                    this.thrusters[thrustDir].Add(thruster);
                }
            }

            public ThrustController(IMyShipController controller, IMyThrust thruster)
            {
                this.controller = controller;
                this.thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
                this.desiredPosition = controller.WorldMatrix.Translation;

                Base6Directions.Direction thrustDir = thruster.Orientation.Forward.OppositeDirection();

                if (!this.thrusters.ContainsKey(thrustDir))
                {
                    this.thrusters[thrustDir] = new List<IMyThrust>();
                }

                this.thrusters[thrustDir].Add(thruster);
            }

            public ThrustController(IMyShipController controller)
            {
                this.controller = controller;
                this.thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
                this.desiredPosition = controller.WorldMatrix.Translation;
            }
            #endregion

            #region methods
            private float GetThrustInDirection(Base6Directions.Direction dir)
            {

                if (!thrusters.ContainsKey(dir))
                {
                    return 0;
                }

                float thrustSum = 0;

                for(int i = 0; i < thrusters[dir].Count; i++)
                {
                    thrustSum += thrusters[dir][i].MaxEffectiveThrust;
                }

                return thrustSum;
            }

            public void Update()
            {

            }
            #endregion
        }
    }
}
