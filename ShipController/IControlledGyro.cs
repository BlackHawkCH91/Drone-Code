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
        public interface IControlledGyro
        {
            IMyGyro Gyro { get; }
            IMyShipController Controller { get; }
            float MaxForce { get; }
            /// <summary>
            /// Override in pitch, yaw, roll format in gyro space
            /// </summary>
            Vector3 Override { get; set; }
            /// <summary>
            /// Override in pitch, yaw, roll format in grid space
            /// </summary>
            Vector3 GridOverride { get; set; }
            /// <summary>
            /// Override in pitch, yaw, roll format in gyro space
            /// </summary>
            Vector3 OverridePercentage { get; set; }
            /// <summary>
            /// Override in pitch, yaw, roll format in grid space
            /// </summary>
            Vector3 GridOverridePercentage { get; set; }

            /// <summary>
            /// The force the gyro will apply under the given circumstances
            /// </summary>
            /// <param name="angularVelocityDifference"></param>
            /// <returns></returns>
            Vector3 GetForceValue(Vector3 angularVelocityDifference);
            Vector3 GetForceValue(Vector3 angularVelocity, Vector3 desiredAngularVelocity);
        }

        const float LARGE_GYRO_FORCE = 3.36E+07f / 1000;
        const float SMALL_GYRO_FORCE = 448000f / 1000;
        const float LARGE_GYRO_MAX_RPM = 3;
        const float SMALL_GYRO_MAX_RPM = 6;

        public class ControlledGyro : IControlledGyro
        {
            #region fields
            private readonly IMyGyro gyro;
            private readonly IMyShipController controller;
            private readonly Matrix gridOrientationMap;
            private readonly float maxForce;
            private readonly float maxRPM;
            #endregion

            #region properties
            public IMyGyro Gyro { get { return gyro; } }
            public IMyShipController Controller { get { return controller; } }
            public float MaxForce { get { return maxForce; } }
            public Vector3 Override
            { 
                get { return new Vector3(gyro.Pitch, gyro.Yaw, gyro.Roll); }
                set { gyro.Pitch = value.X; gyro.Yaw = value.Y; gyro.Roll = value.Z; }
            }
            public Vector3 GridOverride
            {
                get { return Override.ConvertToWorldDirection(gridOrientationMap); }
                set { Override = value.ConvertToLocalDirection(gridOrientationMap); }
            }
            public Vector3 OverridePercentage
            {
                get { return Override * maxRPM; }
                set
                {
                    value.ClampComponents(-1, 1);
                    value *= maxRPM;
                    gyro.Pitch = value.X; gyro.Yaw = value.Y; gyro.Roll = value.Z;
                }
            }
            public Vector3 GridOverridePercentage
            {
                get { return Override.ConvertToWorldDirection(gridOrientationMap) * maxRPM; }
                set
                {
                    value.ClampComponents(-1, 1);
                    value *= maxRPM;
                    Override = value.ConvertToLocalDirection(gridOrientationMap);
                }
            }
            #endregion

            #region constructors
            public ControlledGyro(IMyGyro gyro, IMyShipController controller)
            {
                this.gyro = gyro;
                this.controller = controller;

                Matrix controllerOrientation;
                controller.Orientation.GetMatrix(out controllerOrientation);

                Matrix gyroOrientationMatrix;
                gyro.Orientation.GetMatrix(out gyroOrientationMatrix);
                this.gridOrientationMap = MatrixDExtensions.ConvertToLocalOrientation(gyroOrientationMatrix, controllerOrientation);

                switch (gyro.CubeGrid.GridSizeEnum)
                {
                    case MyCubeSize.Small:
                        maxForce = SMALL_GYRO_FORCE;
                        maxRPM = SMALL_GYRO_MAX_RPM;
                        break;

                    case MyCubeSize.Large:
                        maxForce = LARGE_GYRO_FORCE;
                        maxRPM = LARGE_GYRO_MAX_RPM;
                        break;
                }
            }
            #endregion

            #region methods
            public Vector3 GetForceValue(Vector3 angularVelocityDifference)
            {
                return Vector3.ClampToSphere(angularVelocityDifference, maxForce);
            }

            public Vector3 GetForceValue(Vector3 angularVelocity, Vector3 desiredAngularVelocity)
            {
                Vector3 velocityDiff = desiredAngularVelocity - angularVelocity;
                return Vector3.ClampToSphere(velocityDiff, maxForce);
            }
            #endregion
        }
    }
}
