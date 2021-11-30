﻿using Sandbox.Game.EntityComponents;
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
        TaskScheduler.BindableEvent TestEvent = new TaskScheduler.BindableEvent();
        public Program()
        {
            Runtime.EstablishTaskScheduler(Echo);
            TaskScheduler.Coroutine NewCoroutine = TaskScheduler.CreateCoroutine(new Func<string, string, IEnumerator<int>>(testCoroutine), "hello", "world");
            TaskScheduler.ResumeCoroutine(NewCoroutine);
            TestEvent.Connect(new Func<IEnumerator<int>>(TestEventMethod));
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            TaskScheduler.StepCoroutines(updateSource);
        }

        public IEnumerator<int> TestEventMethod()
        {
                yield return 0;
                TestEvent.Fire();
        }

        public IEnumerator<int> testCoroutine(string text1, string text2)
        {
            TestEvent.Fire();
            yield return 0;
            /*IMyTextSurface TextSurface = Me.GetSurface(0);

            while (true)
            {
                TextSurface.WriteText("hello");
                yield return 5;
                TextSurface.WriteText("world");
                yield return 10;
                TestEvent.Fire();
                yield return 10;
                TestEvent.Fire();
                yield return 5;
            }*/

        }
        public delegate IEnumerator<int> testCoroutineAdder(string text1, string text2);
    }
}
