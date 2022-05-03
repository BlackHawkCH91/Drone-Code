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

        public Program()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update100;
            TaskScheduler.EstablishTaskScheduler(Runtime, Echo, true);
            TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(IEnumMain));
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (!(string.IsNullOrEmpty(argument)))
            {
                gridType = argument;
                //ipList[Me.EntityId] = new object[] { gridType, laserAntPos };
            }

            TaskScheduler.StepCoroutines(updateSource, argument);

        }

        IEnumerator<int> IEnumMain()
        {
            List<IMyThrust> fwdThrust = new List<IMyThrust>();
            List<IMyShipMergeBlock> merge = new List<IMyShipMergeBlock>();

            IMyRemoteControl rc = null;
            IMyGyro gyro = null;

            fwdThrust.Add(GridTerminalSystem.GetBlockWithName("fwd") as IMyThrust);
            fwdThrust.Add(GridTerminalSystem.GetBlockWithName("fwd2") as IMyThrust);

            rc = GridTerminalSystem.GetBlockWithName("rc") as IMyRemoteControl;
            gyro = GridTerminalSystem.GetBlockWithName("gyro") as IMyGyro;

            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(merge);

            

            //Echo("1");

            /*if (gridType == "missile")
            {
                


            } else
            {

            }*/

            while (true)
            {
                //Echo("2");
                if (gridType == "missile")
                {
                    //Echo("3");
                    merge[0].Enabled = false;
                    yield return 0;
                    
                    foreach (IMyThrust thruster in fwdThrust)
                    {
                        thruster.ThrustOverridePercentage = 1;
                    }
                    rc.DampenersOverride = true;
                }
                else
                {

                }

                yield return 0;
            }
        }

        public void Save()
        {

        }
    }
}
