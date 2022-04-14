﻿using Sandbox.Game.EntityComponents;
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
    partial class Program : MyGridProgram
    {
        // General use vars
        IMyShipController controller;
        Vector3D destinationVector = new Vector3D(97828.09, -27530.95, 22508.69);

        public Program()
        {
            controller = GetMainRemoteControl();
            
            // Establish task scheduler
            TaskScheduler.EstablishTaskScheduler(Runtime, Echo, true);
            
            // Establish controllers
            MovementController.Establish(GridTerminalSystem, Runtime, Echo, controller);
            RotationController.Establish(GridTerminalSystem, Runtime, Echo, controller);

            // Set controller positions
            MovementController.MoveTo(destinationVector);
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            TaskScheduler.StepCoroutines(updateSource, argument);
        }

        public IMyShipController GetMainRemoteControl()
        {
            // Get all remote control objects
            List<IMyRemoteControl> remoteControls = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(remoteControls);

            // If main controller is set then return this
            foreach (IMyRemoteControl controller in remoteControls)
            {
                if (controller.IsMainCockpit)
                {
                    return controller;
                }
            }

            // If there is no main controller return first controller in list
            return remoteControls[0];

            // If there are no remote controls throw exception
            throw new Exception("No remote controls found");
        }
    }
}
