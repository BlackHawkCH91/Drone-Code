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
        public string gridType;

        //!Matrix conversion stuff
        public string MatrixToString(MatrixD matrix)
        {
            string strMatrix = "M" + matrix.M11 + "|" + matrix.M12 + "|" + matrix.M13 + "|" + matrix.M14 + "|" +
                            matrix.M21 + "|" + matrix.M22 + "|" + matrix.M23 + "|" + matrix.M24 + "|" +
                            matrix.M31 + "|" + matrix.M32 + "|" + matrix.M33 + "|" + matrix.M34 + "|" +
                            matrix.M41 + "|" + matrix.M42 + "|" + matrix.M43 + "|" + matrix.M44;

            return strMatrix;
        }

        //!Convert string to matrixD
        public MatrixD StringToMatrix(string strMatrix)
        {
            string temp = strMatrix.Substring(1);

            //Get all matrix cells
            string[] strNum = temp.Split('|');

            //Create array and convert to double
            double[] num = new double[16];

            for (int i = 0; i < 16; i++)
            {
                num[i] = double.Parse(strNum[i]);
            }

            //Painfully insert each value
            MatrixD matrix = new MatrixD(num[0], num[1], num[2], num[3],
                                num[4], num[5], num[6], num[7],
                                num[8], num[9], num[10], num[11],
                                num[12], num[13], num[14], num[15]);

            return matrix;
        }

        public static Vector3D ConvertToLocalPosition(Vector3D WorldPosition, MatrixD reference)
        {

            Vector3D referenceWorldPosition = reference.Translation; // block.WorldMatrix.Translation is the same as block.GetPosition() btw

            // Convert worldPosition into a world direction
            Vector3D worldDirection = WorldPosition - referenceWorldPosition; // This is a vector starting at the reference block pointing at your desired position

            // Convert worldDirection into a local direction
            return Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(reference));

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
                gridType = argument;
                //ipList[Me.EntityId] = new object[] { gridType, laserAntPos };
            }

            TaskScheduler.StepCoroutines(updateSource, argument);

        }

        IEnumerator<int> IEnumMain()
        {
            //Vars
            List<IMyThrust> fwdThrust = new List<IMyThrust>();
            List<IMyShipMergeBlock> merge = new List<IMyShipMergeBlock>();

            IMyRemoteControl rc = null;
            IMyGyro gyro = null;
            IMyCameraBlock camera = null;
            IMyBroadcastListener listener = null;



            if (gridType == "missile")
            {
                //Get necessary blocks
                fwdThrust.Add(GridTerminalSystem.GetBlockWithName("fwd") as IMyThrust);
                fwdThrust.Add(GridTerminalSystem.GetBlockWithName("fwd2") as IMyThrust);
                rc = GridTerminalSystem.GetBlockWithName("rc") as IMyRemoteControl;
                gyro = GridTerminalSystem.GetBlockWithName("gyro") as IMyGyro;
                GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(merge);

                listener = IGC.RegisterBroadcastListener("atgm");

                yield return 0;

                merge[0].Enabled = false;
                yield return 0;

                //Set thrust to max
                foreach (IMyThrust thruster in fwdThrust)
                {
                    thruster.ThrustOverridePercentage = 1;
                }
                rc.DampenersOverride = true;
            }
            else
            {
                camera = GridTerminalSystem.GetBlockWithName("Camera") as IMyCameraBlock;
            }

            while (true)
            {
                if (gridType == "missile")
                {
                    MatrixD msg;
                    if (listener.HasPendingMessage)
                    {
                        msg = StringToMatrix(listener.AcceptMessage().ToString());

                        Vector3D position = ConvertToLocalPosition(Me.CubeGrid.GetPosition(), msg);
                        gyro.Pitch = (float) -position.Y / 2;
                        gyro.Yaw = (float) -position.X / 2;
                    }
                }
                else
                {
                    IGC.SendBroadcastMessage("atgm", MatrixToString(camera.WorldMatrix));
                }

                yield return 0;
            }
        }

        public void Save()
        {

        }
    }
}
