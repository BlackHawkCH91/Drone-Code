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

        public Program()
        {
            Runtime.EstablishCoroutines();
            Coroutine.AddCoroutine(new someshit(TestThing), "hello");
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            Coroutine.StepCoroutines(updateSource);

        }

        public delegate IEnumerator<int> someshit(string shit1);
        public IEnumerator<int> TestThing(string text1)
        {
            while (true)
            {
                Echo(text1);
                yield return 10;
                //Echo(text2);
                yield return 10;
            }
            
        }

    }
}
