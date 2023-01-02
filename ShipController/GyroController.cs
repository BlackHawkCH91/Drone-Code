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
        const double RPM_CONV_FACTOR = 9.549;

        public class GyroController
        {

            #region fields
            private List<IControlledGyro> gyros;
            private readonly IMyShipController controller;
            private double maxForce;
            private MatrixD desiredOrientation;
            private const float usefulTimeThreshold = 60;  // If time is smaller than 1 min
            private const float acceptableAngleError = 0.001f;
            private bool braking = false;
            #endregion

            #region properties
            public List<IControlledGyro> Gyros { get { return gyros; } }
            public double MaxForce { get { return maxForce; } }
            /// <summary>
            /// Angular acceleration in radians per second
            /// </summary>
            public double AngularAcceleration { get { return MaxForce / controller.CalculateShipMass().TotalMass; } }
            public Vector3D AngularVelocity { get { return controller.GetShipVelocities().AngularVelocity.ConvertToLocalDirection(controller); } }
            public MatrixD LocalDesiredOrientation { get { return desiredOrientation.ConvertToLocalOrientation(controller.WorldMatrix); } }
            public double RotationAngle { get { return LocalDesiredOrientation.RotationMagnitude(); } }
            public Vector3D RotationAxis { get { return LocalDesiredOrientation.RotationAxis(); } }
            public Vector3D RequiredRotation
            { 
                get
                {
                    Vector3D rotationError = RotationAxis * RotationAngle;
                    // Check which side of the sphere each axis is on, if it is on the wrong side of the sphere it should be added to .5
                    // If an axis will pass the oposite side of the sphere before reversing, continue rotation past the back of the sphere to take the quickest path

                    // Returns radians
                    return rotationError;
                }
            }  // multiply by -angle due to wanting to correct for angle, not make it grow
            public double TimeToTarget {
                get
                {
                    double val = Math.Abs(RequiredRotation.Length() / AngularVelocity.Length());
                    return val < usefulTimeThreshold ? val : double.PositiveInfinity;
                }
            }
            public double BrakingTime { get { return AngularVelocity.Length() / AngularAcceleration; } }
            public Vector3D BrakingDistance { get { return AngularVelocity * BrakingTime - AngularVelocity.Sign() * 0.5 * AngularAcceleration * Math.Pow(BrakingTime, 2); } }

            public MatrixD DesiredOrientation
            {
                get { return desiredOrientation; }
                set { desiredOrientation = value; }
            }
            #endregion

            #region constructors
            public GyroController(IMyShipController controller)
            {
                this.controller = controller;
                gyros = new List<IControlledGyro>();
            }
            public GyroController(IMyShipController controller, IMyGyro gyro)
            {
                this.controller = controller;
                gyros = new List<IControlledGyro>();
                AddGyro(gyro);
            }
            public GyroController(IMyShipController controller, List<IMyGyro> gyros)
            {
                this.controller = controller;
                this.gyros = new List<IControlledGyro>();
                for (int i = 0; i < gyros.Count; i++)
                {
                    AddGyro(gyros[i]);
                }
            }
            #endregion

            #region methods
            public void AddGyro(IMyGyro gyro)
            {
                IControlledGyro controlledGyro = new ControlledGyro(gyro, controller);
                maxForce += controlledGyro.MaxForce;
                gyros.Add(controlledGyro);
            }

            public void Update()
            {
                if (TimeToTarget > BrakingTime && RotationAngle >= acceptableAngleError)
                {
                    ApplyGridOverride(1);
                }
                else
                {
                    ApplyGridOverride(0);
                }

                TaskScheduler.Echo("Angle: " + RotationAngle.RoundToDp(2).ToString());
                TaskScheduler.Echo("Rotation Error: " + RequiredRotation.RoundToDp(2).ToString());

                TaskScheduler.Echo("TimeToTarget: " + TimeToTarget.RoundToDp(2).ToString());
                TaskScheduler.Echo("Braking time: " + BrakingTime.RoundToDp(2).ToString());
                TaskScheduler.Echo((BrakingTime > TimeToTarget).ToString());
            }

            private void ApplyGridOverride(double val)
            {
                for(int i = 0; i < gyros.Count; i++)
                {
                    gyros[i].GridOverridePercentage = RotationAxis * val;
                }
            }
            #endregion

        }
    }
}
