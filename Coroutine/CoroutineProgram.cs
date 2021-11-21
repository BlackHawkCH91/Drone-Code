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
    /*
     * 
     * NOTES:
     * - Coroutines execute in the order that they are added into the system
     * - There are 60 ticks per ingame second
     * 
     */
    public static class Coroutine
    {
        private class PausedCoroutine
        {
            public IEnumerator<int> coroutine;
            int yieldTime;

            public PausedCoroutine(IEnumerator<int> givenCoroutine, int givenYieldTime)
            {
                coroutine = givenCoroutine;
                yieldTime = givenYieldTime;
            }

            public bool ResumingNow()
            {
                if (yieldTime <= 0)
                {

                    return true;

                }
                else
                {

                    yieldTime--;
                    return false;

                }
            }
        }

        static IMyGridProgramRuntimeInfo Runtime;

        private static List<IEnumerator<int>> activeCoroutines = new List<IEnumerator<int>>();
        private static List<PausedCoroutine> pausedCoroutines = new List<PausedCoroutine>();

        private static List<IEnumerator<int>> coroutinesToRemove = new List<IEnumerator<int>>();
        private static List<PausedCoroutine> pausedCoroutinesToRemove = new List<PausedCoroutine>();

        public static void EstablishCoroutines(this IMyGridProgramRuntimeInfo GridRuntime)
        {
            Runtime = GridRuntime;
        }

        public static void StepCoroutines(UpdateType updateSource)
        {

            // Coroutine will trigger when user presses run or when it triggers itself
            if (updateSource == UpdateType.Once || updateSource == UpdateType.Terminal)
            {

                CheckPausedCoroutines();

                foreach (IEnumerator<int> coroutine in activeCoroutines)
                {

                    bool hasMoreSteps = coroutine.MoveNext();

                    if (hasMoreSteps)
                    {

                        // if yield time is 0 then coroutine must be run next tick so do not create a "paused coroutine" for it
                        if (coroutine.Current > 0)
                        {
                            // Update frequency is changed to update once if it is not already set
                            Runtime.UpdateFrequency |= UpdateFrequency.Once;
                            coroutine.PauseCoroutine();
                        }

                        // Update frequency is changed to update once if it is not already set
                        Runtime.UpdateFrequency |= UpdateFrequency.Once;

                    }
                    else
                    {

                        coroutinesToRemove.Add(coroutine);
                        coroutine.Dispose();

                    }

                }

                // Remove all coroutines that are finished
                foreach (IEnumerator<int> coroutine in coroutinesToRemove)
                {

                    activeCoroutines.Remove(coroutine);

                }
                coroutinesToRemove.Clear();

            }

        }

        public static IEnumerator<int> AddCoroutine(Func<IEnumerator<int>> CoroutineFunc)
        {
            IEnumerator<int> Coroutine = CoroutineFunc();

            activeCoroutines.Add(Coroutine);
            return Coroutine;
        }

        public static void PauseCoroutine(this IEnumerator<int> coroutine)
        {

            activeCoroutines.Remove(coroutine);
            pausedCoroutines.Add( new PausedCoroutine(coroutine, coroutine.Current) );

        }

        public static void PauseCoroutine(this IEnumerator<int> coroutine, int yieldTime)
        {

            activeCoroutines.Remove(coroutine);
            pausedCoroutines.Add(new PausedCoroutine(coroutine, yieldTime));

        }

        private static void CheckPausedCoroutines()
        {
            foreach(PausedCoroutine pausedCoroutine in pausedCoroutines)
            {

                if (pausedCoroutine.ResumingNow())
                {
                    
                    activeCoroutines.Add(pausedCoroutine.coroutine);
                    pausedCoroutinesToRemove.Add(pausedCoroutine);

                } else
                {
                    // Update frequency is changed to update once if it is not already set to make sure that execution will continue until all coroutines resume
                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }

            }

            // Remove all coroutines that have been resumed
            foreach (PausedCoroutine pausedCoroutine in pausedCoroutinesToRemove)
            {

                pausedCoroutines.Remove(pausedCoroutine);

            }
            pausedCoroutinesToRemove.Clear();

        }

    }

}
