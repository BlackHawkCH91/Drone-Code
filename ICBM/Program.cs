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
        public string arg;
        //public int orbitAltitude = 63000;
        double toDeg = 180 / Math.PI;

        //Basic PID controller
        public class PIDController
        {
            public double p;
            public double i;
            public double d;
            public double lastError = 0;
            public double lastErrorInt = 0;

            public double PID(double error, double timestep)
            {
                lastErrorInt += error * timestep;
                double result = (error * p) + (lastErrorInt * i) + ((error - lastError) / timestep * d);
                lastError = error;

                return result;
            }

            public PIDController(double _p, double _i, double _d)
            {
                p = _p;
                i = _i;
                d = _d;
            }
        }

        //Setup corountines
        public Program()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update100;
            TaskScheduler.EstablishTaskScheduler(Runtime, Echo, true);
            TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(IEnumMain));
        }

        public void Main(string argument, UpdateType updateSource)
        {
            //Get arg
            if (!(string.IsNullOrEmpty(argument)))
            {
                arg = argument;
                //ipList[Me.EntityId] = new object[] { gridType, laserAntPos };
            }

            TaskScheduler.StepCoroutines(updateSource, argument);

        }

        public double Clamp(double a, double b, double c)
        {
            //If value is less than min
            if (a < b)
            {
                return b;
            }
            //value greater than max
            if (a > c)
            {
                return c;
            }
            return a;
        }

        public double AngleBetweenVectors(Vector3D a, Vector3D b)
        {
            Vector3D vectorDiff = b - a;
            double dotProd = Vector3D.Dot(a, b);
            return Math.Acos(dotProd / (a.Length() * b.Length())) * toDeg;
        }

        IEnumerator<int> IEnumMain()
        {
            double orbitAltitude = 68000;
            //Get necessary components
            //List<IMyThrust> fwdThrust = new List<IMyThrust>();
            IMyThrust fwdThrust = GridTerminalSystem.GetBlockWithName("fwd") as IMyThrust;
            List<IMyThrust> thrusters = new List<IMyThrust>();
            List<IMyGyro> gyros = new List<IMyGyro>();

            IMyRemoteControl rc = GridTerminalSystem.GetBlockWithName("rc") as IMyRemoteControl;
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);

            List<IMyShipMergeBlock> mergeBlocks = new List<IMyShipMergeBlock>();
            List<IMyWarhead> warheads = new List<IMyWarhead>();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(mergeBlocks);
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(warheads);



            //Enable override
            foreach (IMyGyro gyro in gyros)
            {
                gyro.GyroOverride = true;
            }

            yield return 0;

            fwdThrust.ThrustOverridePercentage = 1;

            //Test1: 51031.15, -30501.37, 13645.47
            //Test2: 53355.63, -26745.46, 12692.21
            //GPS:BlackHawkCH91 #1:51706.22:-18929.43:24834.6:#FF75C9F1:

            //Vector3D targetPostion = new Vector3D(53355.63, -26745.46, 12692.21);
            //Vector3D targetPostion = new Vector3D(51706.22, -18929.43, 24834.6);
            Vector3D targetPostion = new Vector3D(51031.15, -30501.37, 13645.47);
            Vector3D planetPosition = new Vector3D(.5, .5, .5);

            //Vector3D targetPostion = new Vector3D(53420.24, -26688.61, 12551.08);

            int state = 0;

            PIDController pitch = new PIDController(5, 0, 1);
            PIDController yaw = new PIDController(5, 0, 1);
            PIDController roll = new PIDController(5, 0, 1);


            //Launch
            while ((rc.GetPosition() - planetPosition).LengthSquared() < Math.Pow(orbitAltitude - 700, 2) && state == 0)
            {
                Vector3D PlanetToICBM = planetPosition - rc.GetPosition();

                Vector3D verticalDirection = -Vector3D.Normalize(Vector3DExtensions.ConvertToLocalPosition(PlanetToICBM, rc));

                //Echo($"test: {Vector3DExtensions.ConvertToLocalPosition(PlanetToICBM, rc)}");
                //Echo($"Vert Dir: {verticalDirection}");
                //Echo(Runtime.TimeSinceLastRun.TotalSeconds.ToString());

                Vector3D targetDirection = Vector3D.Normalize(Vector3DExtensions.ConvertToLocalPosition(targetPostion, rc));

                double timestep = Runtime.TimeSinceLastRun.TotalSeconds;

                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Pitch = (float)pitch.PID(verticalDirection.Y, timestep);
                    gyro.Yaw = (float)yaw.PID(verticalDirection.X, timestep);
                    gyro.Roll = (float)roll.PID(targetDirection.X, timestep);
                }

                //double minAlt = Math.Pow(orbitAltitude, 2);
                //Echo(PlanetToICBM.Length().ToString());
                //Echo(orbitAltitude.ToString());


                yield return 0;
            }

            //state++;
            //(rc.GetPosition() - planetPosition).LengthSquared() >= 1089000000 && 

            pitch = new PIDController(0.05, 0, 0.01);
            yaw = new PIDController(0.1, 0, 0.02);
            roll = new PIDController(5, 0, 1);

            //Orbit
            while (true)
            {
                Vector3D altitudePos = Vector3DExtensions.ConvertToLocalPosition(Vector3D.Normalize(rc.GetPosition() - planetPosition) * 61500, rc);
                //altitudePos.Z += 500;
                Vector3D targetDirection = Vector3D.Normalize(Vector3DExtensions.ConvertToLocalPosition(targetPostion, rc));
                Vector3D altitudeDirection = -Vector3D.Normalize(Vector3DExtensions.ConvertToLocalPosition(altitudePos - rc.GetPosition(), rc));
                Vector3D planetDirection = Vector3D.Normalize(Vector3DExtensions.ConvertToLocalPosition(planetPosition, rc));


                double pitchAngle = AngleBetweenVectors(new Vector3D(0, 0, 1), Vector3DExtensions.ConvertToLocalPosition(Vector3D.Normalize(rc.GetPosition() - planetPosition), rc));

                double altError = Clamp(orbitAltitude - (rc.GetPosition() - planetPosition).Length(), -300, 300) / 300;
                double angle = -(60 * altError) + 90;

                //Echo(pitchAngle.ToString());
                Echo(altError.ToString());
                Echo(angle.ToString());
                Echo((orbitAltitude - (rc.GetPosition() - planetPosition).Length()).ToString());

                //Echo($"Pos: {Vector3D.Normalize(rc.GetPosition() - planetPosition) * 61500}\n");
                //Echo(altitudeDirection.ToString());
                //Echo(targetDirection.ToString());
                //Echo(planetDirection.ToString());

                double timestep = Runtime.TimeSinceLastRun.TotalSeconds;
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Pitch = (float)pitch.PID(pitchAngle - angle, timestep);
                    gyro.Yaw = (float)yaw.PID(targetDirection.X, timestep);
                    gyro.Roll = (float)roll.PID(planetDirection.X, timestep);
                }

                if ((Vector3D.Normalize(targetPostion) * orbitAltitude - rc.GetPosition()).LengthSquared() < 1000000)
                {
                    break;
                }

                yield return 0;
            }

            //Alignment
            pitch = new PIDController(0.05, 0, 0.01);
            yaw = new PIDController(0.05, 0, 0.01);

            int j = 0;
            int i = warheads.Count - 1;
            while (true)
            {
                Vector3D alignedPos = Vector3DExtensions.ConvertToLocalPosition(Vector3D.Normalize(targetPostion) * ((rc.GetPosition() - planetPosition) - 200).Length(), rc) * 0.35;
                double angleDiff = AngleBetweenVectors(new Vector3D(0, 0, 1), alignedPos);

                Echo(alignedPos.ToString());

                double timestep = Runtime.TimeSinceLastRun.TotalSeconds;
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Pitch = (float)pitch.PID(alignedPos.Y, timestep);
                    gyro.Yaw = (float)yaw.PID(alignedPos.X, timestep);
                    if ((rc.GetPosition() - targetPostion).LengthSquared() < 6250000)
                    {
                        gyro.Roll = 25;
                    }
                }



                if ((rc.GetPosition() - targetPostion).LengthSquared() < 6250000 && j <= 0 && i >= 0)
                {
                    warheads[i].IsArmed = true;
                    yield return 0;
                    mergeBlocks[i * 2].Enabled = false;
                    i--;
                    j = 7;
                }

                j--;

                yield return 0;
            }

            //Deploy

            yield return 0;
        }

        public void Save()
        {

        }
    }
}
