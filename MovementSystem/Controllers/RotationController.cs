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
        public static class RotationController
        {
            private static IMyGridTerminalSystem GridTerminalSystem;
            private static IMyGridProgramRuntimeInfo Runtime;
            private static Action<string> Echo;
            private static IMyShipController shipController;

            private static IEnumerator<int> RunCoroutine()
            {
                while (true)
                {
                    MatrixD refMatrix = shipController.WorldMatrix;
                    QuaternionD quaternion = QuaternionD.CreateFromForwardUp(refMatrix.Forward, refMatrix.Up);

                    IMyTextSurface textSurface = (GridTerminalSystem.GetBlockWithName("Cockpit") as IMyTextSurfaceProvider).GetSurface(0);
                    textSurface.WriteText($"X:{quaternion.X}\nY:{quaternion.Y}\nZ:{quaternion.Z}\nW:{quaternion.W}");
                    Echo(QuaternionD.CreateFromRotationMatrix(refMatrix).ToString());
                    yield return 0;
                }
            }

            private static void Run()
            {
                TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(RunCoroutine));
            }

            public static void Establish(IMyGridTerminalSystem gridTerminalSystem, IMyGridProgramRuntimeInfo runtime, Action<string> echo, IMyShipController ShipController)
            {
                GridTerminalSystem = gridTerminalSystem;
                Runtime = runtime;
                Echo = echo;
                shipController = ShipController;
                Run();
            }
        }
    }
}
