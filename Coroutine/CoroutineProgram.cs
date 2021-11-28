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
    public static class TaskScheduler
    {
        public class Coroutine
        {
            internal int yieldTime = 0;
            internal string status = "new";         // field
            public string Status                    // property
            {
                get { return status; }
            }

            internal Delegate CoroutineMethod;
            internal object[] InitialArgs;
            internal IEnumerator<int> CoroutineEnumerator;

            // Constructor
            public Coroutine(Delegate coroutineMethod, params object[] args)
            {
                CoroutineEnumerator = (IEnumerator<int>)coroutineMethod.DynamicInvoke(args);
            }

            public void Kill()
            {
                status = "dead";
                CoroutineEnumerator.Dispose();
            }

            internal bool RunningThisTick()
            {
                if(yieldTime <= 0)
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

        public class EventConnection
        {
            internal Delegate connectedMethod;
            private EventSignal connectedSignal;
            public void Disconnect()
            {
                connectedSignal.RemoveConnection(this);
            }

            // Constructor
            public EventConnection(Delegate MethodToConnect, EventSignal SignalToConnect)
            {
                connectedMethod = MethodToConnect;
                connectedSignal = SignalToConnect;
            }
        }

        public class EventSignal
        {
            private List<EventConnection> connections = new List<EventConnection>();

            internal void RemoveConnection(EventConnection connection)
            {
                connections.Remove(connection);
            }

            public void Fire(params object[] args)
            {
                foreach (EventConnection connection in connections)
                {
                    CreateCoroutine(connection.connectedMethod, args);
                }
            }

            public EventConnection Connect(Delegate MethodToConnect)
            {
                return new EventConnection(MethodToConnect, this);
            }
        }

        public class BindableEvent
        {
            public EventSignal Event;
            public void Fire(params object[] args)
            {
                Event.Fire(args);
            }
        }

        static IMyGridProgramRuntimeInfo Runtime;

        // Coroutine Lists
        private static List<Coroutine> activeCoroutines = new List<Coroutine>();
        private static List<Coroutine> pausedCoroutines = new List<Coroutine>();
        private static List<Coroutine> yieldedCoroutines = new List<Coroutine>();

        private static List<Coroutine> coroutinesToRemove = new List<Coroutine>();
        private static List<Coroutine> coroutinesToPause = new List<Coroutine>();
        private static List<Coroutine> coroutinesToYield = new List<Coroutine>();
        private static List<Coroutine> coroutinesToResume = new List<Coroutine>();

        private static List<Coroutine> yieldedCoroutinesToRemove = new List<Coroutine>();

        public static void EstablishTaskScheduler(this IMyGridProgramRuntimeInfo GridRuntime)
        {
            Runtime = GridRuntime;
        }

        public static void StepCoroutines(UpdateType updateSource)
        {

            // Coroutine will trigger when user presses run or when it triggers itself
            if (updateSource == UpdateType.Once || updateSource == UpdateType.Terminal)
            {
                CheckYieldedCoroutines();

                foreach (Coroutine coroutine in activeCoroutines)
                {
                    bool hasMoreSteps = coroutine.CoroutineEnumerator.MoveNext();

                    if (hasMoreSteps)
                    {
                        // if yield time is 0 then coroutine must be run next tick so do not create a "paused coroutine" for it
                        if (coroutine.CoroutineEnumerator.Current > 0)
                        {
                            // Update frequency is changed to update once if it is not already set
                            Runtime.UpdateFrequency |= UpdateFrequency.Once;
                            coroutinesToYield.Add(coroutine);
                        }

                        // Update frequency is changed to update once if it is not already set
                        Runtime.UpdateFrequency |= UpdateFrequency.Once;
                    }
                    else
                    {
                        coroutinesToRemove.Add(coroutine);
                    }
                }

                // Remove all coroutines to remove
                foreach (Coroutine coroutine in coroutinesToRemove)
                {
                    InternalRemoveCoroutine(coroutine);
                }
                coroutinesToRemove.Clear();

                // Pause all coroutines that must be paused
                foreach (Coroutine coroutine in coroutinesToPause)
                {
                    InternalPauseCoroutine(coroutine);
                }
                coroutinesToPause.Clear();

                // Yield all coroutines that must be yielded
                foreach (Coroutine coroutine in coroutinesToYield)
                {
                    InternalYieldCoroutine(coroutine, coroutine.CoroutineEnumerator.Current);
                }
                coroutinesToYield.Clear();

                // Resume all coroutines that must be resumed
                foreach (Coroutine coroutine in coroutinesToResume)
                {
                    InternalResumeCoroutine(coroutine);
                }
                coroutinesToResume.Clear();
            }
        }

        public static Coroutine CreateCoroutine(Delegate CoroutineFunc, params object[] args)
        {
            Coroutine Coroutine = new Coroutine(CoroutineFunc, args);
            return Coroutine;
        }

        private static void InternalPauseCoroutine(Coroutine coroutine)
        {
            activeCoroutines.Remove(coroutine);
            yieldedCoroutines.Remove(coroutine);

            pausedCoroutines.Add(coroutine);
            coroutine.status = "paused";
        }
        public static void PauseCoroutine(Coroutine coroutine)
        {
            // Flag coroutine to be paused next coroutine step
            coroutinesToPause.Add(coroutine);
        }

        private static void InternalResumeCoroutine(Coroutine coroutine)
        {
            pausedCoroutines.Remove(coroutine);
            yieldedCoroutines.Remove(coroutine);

            // Add coroutine to active list and set yield time to 0 to execute next frame
            activeCoroutines.Add(coroutine);
            coroutine.yieldTime = 0;
            coroutine.status = "running";
        }
        public static void ResumeCoroutine(Coroutine coroutine)
        {
            coroutinesToResume.Add(coroutine);
        }

        private static void InternalRemoveCoroutine(Coroutine coroutine)
        {
            activeCoroutines.Remove(coroutine);
            pausedCoroutines.Remove(coroutine);
            yieldedCoroutines.Remove(coroutine);
            coroutinesToRemove.Remove(coroutine);
            coroutinesToPause.Remove(coroutine);
            coroutinesToYield.Remove(coroutine);
            coroutinesToResume.Remove(coroutine);
            coroutine.Kill();
        }
        public static void RemoveCoroutine(Coroutine coroutine)
        {
            coroutinesToRemove.Add(coroutine);
        }

        private static void InternalYieldCoroutine(Coroutine coroutine, int YieldTime)
        {
            activeCoroutines.Remove(coroutine);
            yieldedCoroutines.Add(coroutine);

            coroutine.yieldTime = YieldTime;
        }

        private static void CheckYieldedCoroutines()
        {
            foreach (Coroutine coroutine in yieldedCoroutines)
            {
                if (coroutine.RunningThisTick())
                {
                    activeCoroutines.Add(coroutine);
                    yieldedCoroutinesToRemove.Add(coroutine);
                }
                else
                {
                    // Update frequency is changed to update once if it is not already set to make sure that execution will continue until all coroutines resume
                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
            }

            // Remove all coroutines that have been resumed
            foreach (Coroutine coroutine in yieldedCoroutinesToRemove)
            {
                yieldedCoroutines.Remove(coroutine);
            }
            yieldedCoroutinesToRemove.Clear();

        }

    }
    

}
