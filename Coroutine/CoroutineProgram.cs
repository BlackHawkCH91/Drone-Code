using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public static class Coroutine
    {

        static IMyGridProgramRuntimeInfo Runtime;
        public static List<IEnumerator<bool>> ActiveCoroutines = new List<IEnumerator<bool>>();

        public static void EstablishCoroutines(this IMyGridProgramRuntimeInfo GridRuntime)
        {
            Runtime = GridRuntime;
        }

        public static void StepCoroutines(UpdateType updateSource)
        {
            // Coroutine will trigger when user presses run or when it triggers itself
            if (updateSource == UpdateType.Once || updateSource == UpdateType.Terminal)
            {

                foreach (IEnumerator<bool> Coroutine in ActiveCoroutines)
                {

                    bool HasMoreSteps = Coroutine.MoveNext();

                    if (HasMoreSteps)
                    {

                        // Update frequency is changed to update once if it is not already set
                        Runtime.UpdateFrequency |= UpdateFrequency.Once;

                    }
                    else
                    {

                        ActiveCoroutines.Remove(Coroutine);
                        Coroutine.Dispose();

                    }

                }

            }

        }

        public static IEnumerator<bool> AddCoroutine(this Func<IEnumerator<bool>> CoroutineFunc)
        {
            IEnumerator<bool> Coroutine = CoroutineFunc();

            ActiveCoroutines.Add(Coroutine);
            return Coroutine;

        }

        public static void PauseCoroutine(this IEnumerator<bool> Coroutine)
        {

            ActiveCoroutines.Remove(Coroutine);

        }
    }
}
