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
        public class ThrustGroup
        {
            public List<IMyThrust> thrusters;
            public double maxThrust;

            public ThrustGroup()
            {
                thrusters = new List<IMyThrust>();
                maxThrust = 0;
            }
            public ThrustGroup(IMyThrust Thruster)
            {
                thrusters = new List<IMyThrust>();
                thrusters.Add(Thruster);
                CalcMaxEffectiveThrust();
            }

            public ThrustGroup(List<IMyThrust> ThrusterList)
            {
                thrusters = ThrusterList;
                CalcMaxEffectiveThrust();
            }

            public void AddThruster(IMyThrust Thruster)
            {
                thrusters.Add(Thruster);
            }
            private void CalcMaxEffectiveThrust()
            {
                maxThrust = 0;
                foreach(IMyThrust thruster in thrusters)
                {
                    maxThrust += thruster.MaxEffectiveThrust;
                }
            }

            public void ApplyThrustPercentage(double ThrustPercentage)
            {
                foreach(IMyThrust thruster in thrusters)
                {
                    thruster.ThrustOverridePercentage = (float)ThrustPercentage;
                }
            }
            public void ApplyThrustForce(double ThrustForce)
            {
                foreach(IMyThrust thruster in thrusters)
                {
                    thruster.ThrustOverride = (float)(ThrustForce / thrusters.Count());
                }
            }
        }
    }
}
