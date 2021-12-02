﻿using Sandbox.Game.EntityComponents;
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
            public Vector3D thrustForceDirection;
            public double maxEffectiveThrust;
            public double currentThrust;
            public double currentThrustPercentage;
            public double availableThrust;
            public double availableThrustPercentage;

            public ThrustGroup(Vector3D ThrustDirection)
            {
                thrusters = new List<IMyThrust>();
                thrustForceDirection = ThrustDirection;
                maxEffectiveThrust = 0;
            }
            public ThrustGroup(Vector3D ThrustDirection, IMyThrust Thruster)
            {
                thrusters = new List<IMyThrust>();
                thrustForceDirection = ThrustDirection;
                thrusters.Add(Thruster);
                CalcMaxEffectiveThrust();
            }

            public ThrustGroup(Vector3D ThrustDirection, List<IMyThrust> ThrusterList)
            {
                thrusters = ThrusterList;
                thrustForceDirection = ThrustDirection;
                CalcMaxEffectiveThrust();
            }

            public void AddThruster(IMyThrust Thruster)
            {
                thrusters.Add(Thruster);
                CalcMaxEffectiveThrust();
            }

            private void CalcMaxEffectiveThrust()
            {
                maxEffectiveThrust = 0;
                foreach(IMyThrust thruster in thrusters)
                {
                    maxEffectiveThrust += thruster.MaxEffectiveThrust;
                }
            }

            public double CalcThrustEffectiveness(Vector3D DirectionVector)
            {
                return -thrustForceDirection.Dot(Vector3D.Normalize(DirectionVector));
            }

            public bool CanApplyThrust(Vector3D DirectionVector)
            {
                if (thrustForceDirection.Dot(Vector3D.Normalize(DirectionVector)) > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void ApplyThrustPercentage(double ThrustPercentage)
            {
                if(ThrustPercentage > 1)
                {
                    ThrustPercentage = 1;
                }
                foreach(IMyThrust thruster in thrusters)
                {
                    thruster.ThrustOverridePercentage = (float)ThrustPercentage;
                }

                currentThrust = ThrustPercentage * maxEffectiveThrust;
                currentThrustPercentage = ThrustPercentage;
                availableThrust = maxEffectiveThrust - currentThrust;
                availableThrustPercentage = availableThrust / maxEffectiveThrust;
            }
        }
    }
}
