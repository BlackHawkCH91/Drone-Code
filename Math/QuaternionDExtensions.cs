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
    static class QuaternionDExtensions
    {
        public static Vector3D GetTaitBryanAnglesZYX(this QuaternionD q)
        {
            double yaw;
            double pitch;
            double roll;
            double checkVal = q.W * q.Y - q.X * q.Z;
            if (checkVal == 0.5)           // Pointing at +90 degrees pitch - causes singularity
            {
                yaw = Math.Atan2(q.X * q.Y - q.W * q.Z, q.X * q.Z + q.W * q.Y);
                pitch = -Math.PI / 2;
                roll = 0;
            } 
            else if (checkVal == -0.5)    // Pointing at -90 degrees pitch - causes singularity
            {
                yaw = -Math.Atan2(q.X * q.Y - q.W * q.Z, q.X * q.Z + q.W * q.Y);
                pitch = Math.PI / 2;
                roll = 0;
            }
            else
            {
                yaw = Math.Atan2(2.0 * (q.Y * q.Z + q.W * q.X), q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z);
                pitch = Math.Asin(-2.0 * (q.W * q.Y - q.X * q.Z));
                roll = Math.Atan2(2.0 * (q.X * q.Y + q.W * q.Z), q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z);
            }
            return new Vector3D(yaw, pitch, roll);  // Z is yaw, X is pitch, Y is roll
        }

        public static Vector3D Axis(this QuaternionD quaternion)
        {
            return new Vector3D(quaternion.X, quaternion.Y, quaternion.Z);
        }

        public static double Angle(this QuaternionD quaternion)
        {
            return quaternion.W;
        }
    }
}
