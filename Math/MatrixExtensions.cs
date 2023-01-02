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
    static class MatrixDExtensions
    {
        /// <summary>
        /// Returns the tait bryan angles that represent this matrix in the order of yaw, pitch, roll
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Vector3 GetTaitBryanAnglesZYX(this MatrixD matrix)
        { 
            double yaw;
            double pitch;
            double roll;
            if (matrix.M31 == -1)
            {
                pitch = Math.PI / 2;
                roll = Math.Atan2(matrix.M12, matrix.M13);
                yaw = 0;
            }
            else if (matrix.M31 == 1)
            {
                pitch = -Math.PI / 2;
                roll = Math.Atan2(-matrix.M12, -matrix.M13);
                yaw = 0;
            }
            else
            {
                pitch = Math.Asin(-matrix.M31);
                roll = Math.Atan2(matrix.M32, matrix.M33);
                yaw = Math.Atan2(matrix.M21, matrix.M11);
            }
            return new Vector3D(yaw, pitch, roll);  
        }

        public static MatrixD ConvertToLocalOrientation(this MatrixD matrix, MatrixD WorldMatrix)
        {
            return new MatrixD
            {
                Backward = matrix.Backward.ConvertToLocalDirection(WorldMatrix),
                Up = matrix.Up.ConvertToLocalDirection(WorldMatrix),
                Right = matrix.Right.ConvertToLocalDirection(WorldMatrix)
            };
        }

        public static MatrixD ConvertToWorldOrientation(this MatrixD matrix, MatrixD WorldMatrix)
        {
            return new MatrixD
            {
                Backward = matrix.Backward.ConvertToWorldDirection(WorldMatrix),
                Up = matrix.Up.ConvertToWorldDirection(WorldMatrix),
                Right = matrix.Right.ConvertToWorldDirection(WorldMatrix)
            };
        }

        public static double RotationMagnitude(this MatrixD matrix)
        {
            return Math.PI - Math.Acos((1 - (matrix.M11 + matrix.M22 + matrix.M33)) / 2);
        }

        /// <summary>
        /// Returns the axis that this matrix rotates around
        /// If there is no rotation, a zero vector is returned
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Vector3D RotationAxis(this MatrixD matrix)
        {
            Vector3D axis = Vector3D.Zero;
            double angle = matrix.RotationMagnitude();
            double angleFactor = 2 * Math.Sin(angle);

            if (angleFactor != 0)
            {
                axis.X = (matrix.M32 - matrix.M23) / angleFactor;
                axis.Y = (matrix.M13 - matrix.M31) / angleFactor;
                axis.Z = (matrix.M21 - matrix.M12) / angleFactor;
            }

            return axis;
        }
    }
}
