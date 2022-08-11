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

            private static MatrixD desiredMatrix = MatrixD.CreateFromAxisAngle(new Vector3D(0,0,1), Math.PI / 2);

            private static IMyGridTerminalSystem GridTerminalSystem;
            private static IMyGridProgramRuntimeInfo Runtime;
            private static Action<string> Echo;
            private static IMyShipController shipController;

            private static IEnumerator<int> RunCoroutine()
            {
                while (true)
                {
                    MatrixD worldMatrix = shipController.WorldMatrix;
                    MatrixD differenceMatrix = desiredMatrix - worldMatrix;
                    QuaternionD quaternion = QuaternionD.CreateFromRotationMatrix(worldMatrix);
                    Vector3D angles = worldMatrix.GetTaitBryanAnglesZYX();
                    //MatrixD.GetEulerAnglesXYZ(ref worldMatrix, out angles);
                    //angles += Math.PI;

                    IMyTextSurface textSurface0 = (GridTerminalSystem.GetBlockWithName("Cockpit") as IMyTextSurfaceProvider).GetSurface(0);
                    IMyTextSurface textSurface1 = (GridTerminalSystem.GetBlockWithName("Cockpit") as IMyTextSurfaceProvider).GetSurface(1);
                    IMyTextSurface textSurface2 = (GridTerminalSystem.GetBlockWithName("Cockpit") as IMyTextSurfaceProvider).GetSurface(2);
                    textSurface0.WriteText($"X (PITCH):{angles.X.RoundToDp(2)}\nY (YAW):{angles.Y.RoundToDp(2)}\nZ (ROLL):{angles.Z.RoundToDp(2)}");
                    textSurface1.WriteText($"{worldMatrix.M11.RoundToDp(2)},{worldMatrix.M12.RoundToDp(2)},{worldMatrix.M13.RoundToDp(2)}\n{worldMatrix.M21.RoundToDp(2)},{worldMatrix.M22.RoundToDp(2)},{worldMatrix.M23.RoundToDp(2)}\n{worldMatrix.M31.RoundToDp(2)},{worldMatrix.M32.RoundToDp(2)},{worldMatrix.M33.RoundToDp(2)}");
                    textSurface2.WriteText($"X:{quaternion.X.RoundToDp(2)}\nY:{quaternion.Y.RoundToDp(2)}\nZ:{quaternion.Z.RoundToDp(2)}\nW:{quaternion.W.RoundToDp(2)}");
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
