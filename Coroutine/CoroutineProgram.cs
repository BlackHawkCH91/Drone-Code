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
    partial class Program
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
            #region Enums
            public enum CoroutineStatus
            {
                Running,
                Paused,
                Dead
            }
            #endregion

            #region Interfaces
            private interface ICoroutine
            {
                #region Properties
                CoroutineStatus Status { get; }
                IEnumerator<int> Enumerator { get; }
                ulong Id { get; }
                #endregion

                #region Methods
                void Pause();
                void Resume();
                void Kill();
                void Step();
                #endregion
            }
            #endregion

            #region State Classes

            internal class CoroutineRunning : ICoroutine
            {
                #region Fields
                private readonly IEnumerator<int> enumerator;
                private const CoroutineStatus status = CoroutineStatus.Running;
                private readonly ulong id;
                #endregion

                #region Properties
                public CoroutineStatus Status { get { return status; } }
                public IEnumerator<int> Enumerator { get { return enumerator; } }
                public ulong Id { get { return id; } }
                #endregion

                #region Constructors
                public CoroutineRunning(IEnumerator<int> enumerator, ulong id)
                {
                    this.enumerator = enumerator;
                    this.id = id;
                }
                #endregion

                #region Methods
                public void Pause()
                {
                    // Change to paused coroutine
                    coroutines[GetCoroutineIndex(id)] = new CoroutinePaused(enumerator, id);
                }

                public void Resume() { }

                public void Kill()
                {
                    // Change to dead coroutine
                    deadCoroutines.Add(id);
                }

                public void Step()
                {
                    bool stepped = enumerator.MoveNext();

                    if (stepped)
                    {
                        // Queue next update
                        Runtime.UpdateFrequency = UpdateFrequency.Once;

                        int yieldTime = enumerator.Current;
                        if(yieldTime > 0)
                        {
                            // Change to yielded coroutine
                            coroutines[GetCoroutineIndex(id)] = new CoroutineYielded(enumerator, id, yieldTime);
                        }
                    }
                    else
                    {
                        // Change to dead coroutine
                        deadCoroutines.Add(id);
                    }
                }
                #endregion
            }

            internal class CoroutineYielded : ICoroutine
            {
                #region Fields
                private readonly IEnumerator<int> enumerator;
                public const CoroutineStatus status = CoroutineStatus.Running;
                private readonly ulong id;
                private int yieldTime;
                #endregion

                #region Properties
                public CoroutineStatus Status { get { return status; } }
                public IEnumerator<int> Enumerator { get { return enumerator; } }
                public ulong Id { get { return id; } }
                #endregion

                #region Constructors
                public CoroutineYielded(IEnumerator<int> enumerator, ulong id, int yieldTime)
                {
                    this.enumerator = enumerator;
                    this.id = id;
                    this.yieldTime = yieldTime;
                }
                #endregion

                #region Methods
                public void Pause()
                {
                    // Change to paused coroutine
                    coroutines[GetCoroutineIndex(id)] = new CoroutinePaused(enumerator, id);
                }

                public void Resume() { }

                public void Kill()
                {
                    // Change to dead coroutine
                    deadCoroutines.Add(id);
                }

                public void Step()
                {
                    yieldTime -= 1;

                    if(yieldTime <= 0)
                    {
                        // Change to running coroutine
                        coroutines[GetCoroutineIndex(id)] = new CoroutineRunning(enumerator, id);
                    }

                    // Queue next update
                    Runtime.UpdateFrequency = UpdateFrequency.Once;
                }
                #endregion
            }

            internal class CoroutinePaused : ICoroutine
            {
                #region Fields
                private readonly IEnumerator<int> enumerator;
                public const CoroutineStatus status = CoroutineStatus.Paused;
                private readonly ulong id;
                #endregion

                #region Properties
                public CoroutineStatus Status { get { return status; } }
                public IEnumerator<int> Enumerator { get { return enumerator; } }
                public ulong Id { get { return id; } }
                #endregion

                #region Constructors
                public CoroutinePaused(IEnumerator<int> enumerator, ulong id)
                {
                    this.enumerator = enumerator;
                    this.id = id;
                }
                #endregion

                #region Methods
                public void Pause() { }

                public void Resume()
                {
                    // Change to running coroutine
                    coroutines[GetCoroutineIndex(id)] = new CoroutineRunning(enumerator, id);
                }

                public void Kill()
                {
                    // Change to dead coroutine
                    deadCoroutines.Add(id);
                }

                public void Step() { }
                #endregion
            }

            #endregion

            #region Fields
            private static string lastArgument = "";

            private static IMyGridProgramRuntimeInfo Runtime;
            private static MovingAverage averageRuntime = new MovingAverage(120);
            private static Action<string> echo;
            private static bool echoStatus = false;
            private static List<ICoroutine> coroutines = new List<ICoroutine>();
            private static List<ulong> deadCoroutines = new List<ulong>();
            
            #endregion

            #region Properties
            /// <summary>
            /// Seconds since the last step
            /// </summary>
            public static double TimeStep { get { return Runtime.TimeSinceLastRun.TotalMilliseconds / 1000; } }

            public static string LastArgument { get { return lastArgument; } }

            public static double AverageRunTime { get { return averageRuntime.GetAverageValue();} }

            public static Action<string> Echo { get { return echo; } }
            #endregion

            #region Methods

            #region Public
            public static void EstablishTaskScheduler(IMyGridProgramRuntimeInfo GridRuntime, Action<string> Echo, bool EchoStatus)
            {
                Runtime = GridRuntime;
                echo = Echo;
                echoStatus = EchoStatus;
            }

            public static void EstablishTaskScheduler(IMyGridProgramRuntimeInfo GridRuntime, Action<string> GridEcho)
            {
                Runtime = GridRuntime;
                echo = GridEcho;
                echoStatus = false;
            }

            public static CoroutineStatus GetCoroutineStatus(ulong CoroutineId)
            {
                if(GetCoroutine(CoroutineId) != null)
                {
                    return GetCoroutine(CoroutineId).Status;
                }
                else
                {
                    return CoroutineStatus.Dead;
                }
            }

            public static void PauseCoroutine(ulong CoroutineId)
            {
                GetCoroutine(CoroutineId).Pause();
            }

            public static void KillCoroutine(ulong CoroutineId)
            {
                GetCoroutine(CoroutineId).Kill();
            }

            public static void StepCoroutines(UpdateType updateSource, string argument)
            {
                // Set argument if it has been changed
                if (argument != "")
                {
                    lastArgument = argument;
                }

                // Update average timestep
                averageRuntime.AddValue(Runtime.LastRunTimeMs); // Add last runtime in ms

#if !NO_LOGS
                // Log status
                if (echoStatus)
                {
                    Echo(
                        "Num Coroutines: " + coroutines.Count() + "\n" +
                        "Max Instruction Count: " + Runtime.MaxInstructionCount.ToString() + "\n" +
                        "Current Instruction Count: " + Runtime.CurrentInstructionCount.ToString() + "\n" +
                        "Max Call Chain Depth: " + Runtime.MaxCallChainDepth.ToString() + "\n" +
                        "Current Call Chain Depth: " + Runtime.CurrentCallChainDepth.ToString() + "\n" +
                        "Last Run Time ms: " + Runtime.LastRunTimeMs.ToString() + "\n" +
                        "Average Runtime ms: " + AverageRunTime.RoundToDp(3) + "\n" +
                        "Time Since Last Run ms: " + Runtime.TimeSinceLastRun.TotalMilliseconds.ToString()
                        );
                }

#endif

                // Coroutine will trigger when user presses run or when it triggers itself
                if (updateSource == UpdateType.Once || updateSource == UpdateType.Terminal)
                {

                    // Remove dead coroutines
                    coroutines.RemoveAll(coroutine => deadCoroutines.Contains(coroutine.Id));
                    deadCoroutines.Clear();

                    // Run coroutines
                    for (int i = 0; i < coroutines.Count(); i++)
                    {
                        ICoroutine coroutine = coroutines[i];
                        coroutine.Step();
                    }
                }
            }

            /// <summary>
            /// Creates a coroutine that is paused by default
            /// </summary>
            /// <param name="CoroutineFunc"></param>
            /// <param name="args"></param>
            /// <returns></returns>
            public static ulong CreateCoroutine(Delegate CoroutineFunc, params object[] args)
            {
                IEnumerator<int> enumerator = (IEnumerator<int>)CoroutineFunc.DynamicInvoke(args);
                ulong coroutineId = UIDGenerator.GenerateUID();
                coroutines.Add(new CoroutinePaused(enumerator, coroutineId));
                return coroutineId;
            }
            
            /// <summary>
            /// Creates a coroutine that is running by default
            /// </summary>
            /// <param name="CoroutineFunc"></param>
            /// <param name="args"></param>
            /// <returns></returns>
            public static ulong SpawnCoroutine(Delegate CoroutineFunc, params object[] args)
            {
                IEnumerator<int> enumerator = (IEnumerator<int>)CoroutineFunc.DynamicInvoke(args);
                ulong coroutineId = UIDGenerator.GenerateUID();
                coroutines.Add(new CoroutineRunning(enumerator, coroutineId));
                return coroutineId;
            }
            #endregion

            #region Private
            private static ICoroutine GetCoroutine(ulong id)
            {
                return coroutines.Find(coroutine => coroutine.Id == id);
            }

            private static int GetCoroutineIndex(ulong id)
            {
                return coroutines.FindIndex(coroutine => coroutine.Id == id);
            }
            #endregion
            
            #endregion
        }


    }
}
