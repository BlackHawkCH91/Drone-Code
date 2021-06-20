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
    partial class Program : MyGridProgram
    {
        // VARS

        bool setup = false;
        //int timer = 0;

        //Listeners and display for sending and recieving data
        IMyBroadcastListener Listener;
        IMyTerminalBlock mainProgBlock;
        List<IMyTextPanel> LCD = new List<IMyTextPanel>();

        //Information used to create a packet that will be sent back to the main base.
        long droneId;
        string droneType = "test";
        Vector3 dronePos;
        float droneHealth = 100;
        float power = 100;
        float hydrogen = 100;
        string droneAction = "idle";

        int terminalBlockCount;

        string packet;

        int blockCount;


        //Functions ----------------------------------------------------------------------

        //Converts a string back into a Vector3
        public static Vector3 StringToVector3(string sVector)
        {
            //Remove curly brackets
            if (sVector.StartsWith("{") && sVector.EndsWith("}"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            //Split the string where there is whitespace (commas are not used for some reason)
            string[] sArray = sVector.Split(' ');

            //Parse the values into floats and create a Vector3
            Vector3 position = new Vector3(
                float.Parse(sArray[0].Substring(2, sArray[0].Length - 2)),
                float.Parse(sArray[1].Substring(2, sArray[1].Length - 2)),
                float.Parse(sArray[2].Substring(2, sArray[2].Length - 2))
            );

            return position;
        }

        //Function to send data
        public void SendMessage(string Contents)
        {
            IGC.SendBroadcastMessage<string>("WaypointCom", Contents, TransmissionDistance.TransmissionDistanceMax);
        }

        //function to receive data.
        public void RecieveMessage()
        {
            //Receive message and write it to the LCD.
            MyIGCMessage Message = Listener.AcceptMessage();
            LCD[0].WriteText(Message.Data.ToString());
        }

        //---------------------------------------------------------------------------------------------------------------



        public void Init()
        {
            //Initialise and prepare variables to send and receive data.
            Listener = IGC.RegisterBroadcastListener("WaypointCom");
            Listener.DisableMessageCallback();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(LCD);
            mainProgBlock = GridTerminalSystem.GetBlockWithName("MainProgBlock");


            droneId = mainProgBlock.EntityId;

            packet = droneId.ToString() + "|" + droneType + "|" + dronePos.ToString() + "|" + droneHealth.ToString() + "|" + power.ToString() + "|" + hydrogen.ToString() + "|" + droneAction;

            setup = true;
            Echo("Active");
        }


        //Set update time
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            dronePos = Me.CubeGrid.GetPosition();

            //Initialise once
            if (!setup)
            {
                Init();
            }

            SendMessage(packet);

            //Check and receive data
            if (Listener.HasPendingMessage)
            {
                RecieveMessage();
            }
        }

        public void Save()
        {

        }
    }
}
