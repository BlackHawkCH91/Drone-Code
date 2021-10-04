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

        public static Dictionary<string, string> ipList = new Dictionary<string, string>();
        public static Dictionary<string, List<int>> bracketPos = new Dictionary<string, List<int>>();

        bool setup = false;
        //int timer = 0;

        //Listeners and display for sending and recieving data
        List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
        IMyUnicastListener dirListener;
        IMyRadioAntenna antenna;
        IMyTerminalBlock mainProgBlock;
        List<IMyTextPanel> LCD = new List<IMyTextPanel>();

        //Information used to create a packet that will be sent back to the main base.
        long gridId;
        string gridType;
        Vector3 gridPos;


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

        //Gets block health
        float GetMyTerminalBlockHealth(IMyTerminalBlock block)
        {
            IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
            float MaxIntegrity = slimblock.MaxIntegrity;
            float BuildIntegrity = slimblock.BuildIntegrity;
            float CurrentDamage = slimblock.CurrentDamage;
            return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
        }

        //Gets positions of [] in strings
        static Dictionary<string, List<int>> getBracketPos(string packet)
        {
            //Creates bracket dictionary, adds two keys for { and }

            bracketPos.Clear();

            bracketPos.Add("[", new List<int>());
            bracketPos.Add("]", new List<int>());

            //Loop through string, checking for brackets
            for (int i = 0; i < packet.Length; i++)
            {
                //Add bracket pos to its respective dictionary
                if (packet[i] == '[')
                {
                    bracketPos["["].Add(i);
                }
                else if (packet[i] == ']')
                {
                    bracketPos["]"].Add(i);
                }
            }

            return bracketPos;
        }

        //Converts object to string
        static string objectToString(object[] packet)
        {
            string final = "[";
            int i = 0;

            //Loop through all items in object
            foreach (object item in packet)
            {
                if (item.GetType() == typeof(object[]))
                {
                    //If it is an object array, use recursion
                    final += objectToString((object[])item);
                }
                else
                {
                    //If not, convert type to string. If its a string, add "", if vec3 use toString, etc
                    if (item.GetType() == typeof(string))
                    {
                        final += "\"" + item + "\"";
                    }
                    else if (item.GetType() == typeof(Vector3))
                    {
                        Vector3 vec3 = (Vector3)item;
                        final += vec3.ToString();

                    }
                    else
                    {
                        final += item;
                    }
                }

                //Prevent adding unnecessary comma
                if (i < packet.Length - 1)
                {
                    final += ",";
                }

                i++;
            }

            final += "]";

            return final;
        }

        //Converts string back into object
        static object[] stringToObject(string packet)
        {
            //Remove start and ending brackets
            packet = packet.Substring(1, packet.Length - 2);

            //Dictionaries to store bracket pos and linked brackets
            Dictionary<int, int> linked = new Dictionary<int, int>();
            Dictionary<string, List<int>> bracketPos = getBracketPos(packet);

            //Find linked brackets
            foreach (int closePos in bracketPos["]"])
            {
                linked.Add(closePos, 0);
                foreach (int openPos in bracketPos["["])
                {
                    //Loop through opening and closing brackets. If the open bracket had not been taken and is closer than the last,
                    //add/change linked
                    if (closePos > openPos && openPos > linked[closePos] && !(linked.ContainsValue(openPos)))
                    {
                        linked[closePos] = openPos;
                    }
                }
            }

            //Get fields that are parents, ignore their children
            List<int> parents = new List<int>();

            //Loop through all linked brackets
            foreach (int potChild in linked.Keys)
            {
                bool isChild = false;

                //potChild = potential child, ignore terrible var name

                //Loop through all linked brackets, checking if potChild is a child
                foreach (KeyValuePair<int, int> parent in linked)
                {
                    if (potChild < parent.Key && potChild > parent.Value)
                    {
                        isChild = true;
                        break;
                    }
                }

                //if potChild is false (not a child), add to parent list
                if (!(isChild))
                {
                    parents.Add(potChild);
                }
            }

            //Recursively run function to convert substrings (children) into objects
            int difference = 0;
            List<object[]> subObjectArr = new List<object[]>();

            foreach (int item in parents)
            {
                //Get section of string that is an object, remove it from packet, and do recursion
                string subObject = packet.Substring(linked[item] - difference, item - linked[item] + 1);
                packet = packet.Remove(linked[item] - difference, subObject.Length);
                difference += subObject.Length;

                subObjectArr.Add(stringToObject(subObject));

            }

            //
            //Creating two arrays here. Is it possible to use just one?
            //

            //Convert the string into an object array
            object[] packetArr = packet.Split(',');
            object[] finalPacketArr = new object[packetArr.Length];
            int a = 0;

            for (int i = 0; i < packetArr.Length; i++)
            {
                string item = packetArr[i] as string;

                //Convert values into correct variable types
                if (item.Length > 0)
                {
                    //Order: Vector3, String, Double, Int, Subobject
                    if (item[0] == '{')
                    {
                        finalPacketArr[i] = StringToVector3(item);
                    }
                    else if (item[0] == '"')
                    {
                        finalPacketArr[i] = item.Substring(1, item.Length - 2);
                    }
                    else if (item.Contains("."))
                    {
                        finalPacketArr[i] = Convert.ToDouble(item);
                    }
                    else
                    {
                        finalPacketArr[i] = Convert.ToInt32(item);
                    }
                }
                else
                {
                    finalPacketArr[i] = subObjectArr[a];
                    a++;
                }
            }

            return finalPacketArr;
        }

        //Debugging only, displays object arr as a string
        static string displayThing(object[] array)
        {
            string output = "";
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].GetType() == typeof(object[]))
                {
                    output += "[";
                    output += displayThing(array[i] as object[]);
                    output += "]";
                }
                else if (array[i].GetType() == typeof(Vector3))
                {
                    Vector3 temp = (Vector3) array[i];
                    output += temp.ToString();
                }
                else
                {
                    output += array[i].ToString();
                }
                if (i < (array.Length - 1))
                {
                    output += ", ";
                }
            }

            return output;
        }

        //Returns string for now, but should be void as it shouldn't return anything. Will be used to send message
        //Packet may need to be 4 parts? Src -> Dest -> purpose -> content?
        //Though content may already contain the purpose
        static string createPacket(string source, string destination, object[] packet)
        {
            string finalPacket = "[" + source + "," + destination + "," + objectToString(packet) + "]";
            //terminal.broadcast(source, finalPacket);
            return finalPacket;
        }

        //Send data
        public void SendMessage(string tag, string contents)
        {
            IGC.SendBroadcastMessage<string>(tag, contents, TransmissionDistance.TransmissionDistanceMax);
        }

        //Receive data
        public void RecieveMessage(IMyBroadcastListener listener)
        {
            MyIGCMessage Message = listener.AcceptMessage();
        }
        //---------------------------------------------------------------------------------------------------------------



        public void Init()
        {
            //Define uni and broadcast listeners. Direct sets are "default" tags, meaning all comms have these tags by default.
            listeners.Add(IGC.RegisterBroadcastListener("EstCon"));
            listeners.Add(IGC.RegisterBroadcastListener("LaserAnt"));
            listeners.Add(IGC.RegisterBroadcastListener("Distress"));

            //It seems that although unicasts require a tag, the reciever does not need the tag to read the message. It seems the tag is
            //more for grids that have multiple PBs.
            dirListener = IGC.UnicastListener;

            //Disable callback for all messages
            foreach (IMyBroadcastListener listener in listeners)
            {
                listener.DisableMessageCallback();
            }

            //Get blocks and ID
            mainProgBlock = GridTerminalSystem.GetBlockWithName("MainProgBlock");
            antenna = GridTerminalSystem.GetBlockWithName("Antenna") as IMyRadioAntenna;

            gridId = mainProgBlock.EntityId;

            setup = true;
            Echo("Active");
        }


        //Set update time
        public Program()
        {
            //Add default and known IPs to ipList
            ipList.Add("EstCon", "Default");
            ipList.Add("Distress", "Default");


            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            gridPos = Me.CubeGrid.GetPosition();

            //Initialise once
            if (!setup)
            {
                Init();
                gridType = argument;
            }

            foreach (IMyBroadcastListener listener in listeners)
            {
                if (listener.HasPendingMessage)
                {
                    RecieveMessage(listener);
                }
            }
        }

        public void Save()
        {

        }
    }
}

/*

TODO:

 - Find a way to accept messages from both uni and broadcasts
 - Create a function that sends a broadcast and returns a list of IP (tag) addresses
 - Create a function that can send a request to connect laser antennas
 - Find a way to create "anonymous" broadcasts (use laser antenna to broadcast briefly so that the ping barely shows up)
 - Distress signal (connect to nearest source. Depending on danger, connect via laser ant, use anonymous broadcast, or do a full broadcast.
   Limit range when a grid has been found)

 - Encode and create a packet, send it to a grid, and get the receiver to decode the packet
 - Get the satellite to send it's information to the test outpost


Notes:

Unicast listeners don't requrie a tag to listen to. Instead, the tag has to be manually filtered out if needed
Try to use a single PB per grid (excluding outposts)

*/