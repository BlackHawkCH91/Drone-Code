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
        public class GyroController
        {

            #region fields
            private List<IControlledGyro> gyros;
            private readonly IMyShipController controller;
            private double maxForce;
            private MatrixD desiredOrientation;
            private QuaternionD rotationAxisAngle;
            #endregion

            #region properties
            public List<IControlledGyro> Gyros { get { return gyros; } }
            public double MaxForce { get { return maxForce; } }
            public double AngularAcceleration { get { return MaxForce / controller.CalculateShipMass().TotalMass; } }
            public Vector3D AngularVelocity { get { return controller.GetShipVelocities().AngularVelocity.ConvertToLocalDirection(controller); } }
            public QuaternionD RotationAxisAngle { get { return rotationAxisAngle; } }
            public Vector3D RequiredRotation { get { return rotationAxisAngle.Axis() * rotationAxisAngle.Angle(); } }
            public Vector3D TimeToTarget { get { return RequiredRotation / AngularVelocity; } }
            public Vector3D BrakingTime { get { return -AngularVelocity / AngularAcceleration; } }

            public MatrixD DesiredOrientation
            {
                get { return desiredOrientation; }
                set { desiredOrientation = value; rotationAxisAngle = QuaternionD.CreateFromRotationMatrix(value); }
            }
            #endregion

            #region constructors
            public GyroController(IMyShipController controller)
            {
                this.controller = controller;
            }
            public GyroController(IMyShipController controller, IMyGyro gyro)
            {
                this.controller = controller;
                AddGyro(gyro);
            }
            public GyroController(IMyShipController controller, List<IMyGyro> gyros)
            {
                this.controller = controller;
                for(int i = 0; i < gyros.Count; i++)
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
                TaskScheduler.Echo(RequiredRotation.ToString());
            }
            #endregion

        }
    }
}
