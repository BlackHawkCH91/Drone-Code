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
    partial class Program : MyGridProgram
    {
        double maxRotorSpeed = 60;
        double targetAngle = 180;

        IMyMotorStator rotationRot;
        IMyMotorStator RElevationRot;
        IMyMotorStator LElevationRot;

        double p = 0.5;
        double i = 0.5;
        double d = 0.5;

        //R: -vel is up, L: +vel is up

        public Program()
        {
            rotationRot = GridTerminalSystem.GetBlockWithName("Rotation") as IMyMotorStator;
            RElevationRot = GridTerminalSystem.GetBlockWithName("RElevator") as IMyMotorStator;
            LElevationRot = GridTerminalSystem.GetBlockWithName("LElevator") as IMyMotorStator;

            TaskScheduler.EstablishTaskScheduler(Runtime, Echo, true);
            TaskScheduler.ResumeCoroutine(TaskScheduler.CreateCoroutine(new Func<IEnumerator<int>>(IEnumMain)));
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            TaskScheduler.StepCoroutines(updateSource);
        }

        public IEnumerator<int> IEnumMain()
        {
            while (true)
            {
                //-10000^-x(1-x)+1
                double turretAngle = rotationRot.Angle * (180 / Math.PI);
                double difference = (targetAngle - turretAngle + 360) % 360;
                int rotDirection = difference > 180 ? 1 : -1;

                double error = difference > 180 ? (difference - 180) : difference;



                //rotationRot.TargetVelocityRPM = (float)((-Math.Pow(10, -error) * (1 - error) + 1) * maxRotorSpeed * rotDirection);

                yield return 0;
            }
        }
}
