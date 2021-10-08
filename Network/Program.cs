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
        // VARS

        public static Dictionary<string, object[]> ipList = new Dictionary<string, object[]>();
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
        long pBId;
        string gridType;
        Vector3D gridPos;

        object[] laserAntPos;


        //Functions ----------------------------------------------------------------------

        //Functions are not obsolete. Can be used to load and unload saved data


        //String-to-data and data-to-string functions hidden here:
        
        //So, for whatever fucking reason, vector3.ToString() doesn't always have "{}" at the start and end, thus causing issues for type casting in the stringToObject. This function purely
        //exists to ensure all string vectors have {}.
        public static string vector3ToString(string vector)
        {
            if (!(vector.StartsWith("{") && vector.EndsWith("}")))
            {
                vector = "{" + vector + "}";
            }

            return vector;
        }

        //Converts a string back into a Vector3
        public static Vector3D StringToVector3(string sVector)
        {
            //Remove curly brackets
            if (sVector.StartsWith("{") && sVector.EndsWith("}"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }


            //Split the string where there is whitespace (commas are not used for some reason)
            string[] sArray = sVector.Split(' ');

            //Parse the values into floats and create a Vector3
            Vector3D position = new Vector3D(
                double.Parse(sArray[0].Substring(2, sArray[0].Length - 2)),
                double.Parse(sArray[1].Substring(2, sArray[1].Length - 2)),
                double.Parse(sArray[2].Substring(2, sArray[2].Length - 2))
            );

            return position;
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
                    else if (item.GetType() == typeof(Vector3D))
                    {
                        Vector3D vec3 = (Vector3D)item;
                        final += vector3ToString(vec3.ToString());

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
        public static object[] stringToObject(string packet)
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
                        finalPacketArr[i] = long.Parse(item);
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
                else if (array[i].GetType() == typeof(Vector3D))
                {
                    Vector3D temp = (Vector3D) array[i];
                    output += temp.ToString();
                } else if (array[i].GetType() == typeof(string))
                {
                    output += "\"" + array[i].ToString() + "\"";
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

        static string createPacketString(string source, string destination, string purpose, object[] packet)
        {
            string tempDest = destination;
            long temp;
            if (!(long.TryParse(destination, out temp)))
            {
                tempDest = "\"" + tempDest + "\"";
            }
            string finalPacket = "[" + source + "," + tempDest + ",\"" + purpose + "\"," + objectToString(packet) + "]";
            //terminal.broadcast(source, finalPacket);
            return finalPacket;
        }

        //-----------------------------------------------------------------------------
        


        //Gets block health
        double GetMyTerminalBlockHealth(IMyTerminalBlock block)
        {
            IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
            double MaxIntegrity = slimblock.MaxIntegrity;
            double BuildIntegrity = slimblock.BuildIntegrity;
            double CurrentDamage = slimblock.CurrentDamage;
            return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
        }

        static string createPacketObject(string source, string destination, string purpose, object[] packet)
        {
            object[] finalPacket = new object[] { source, destination, purpose, packet };
            //terminal.broadcast(source, finalPacket);
            return objectToString(finalPacket);
        }

          //Send data. This function might be obselete/not needed.
        public void SendMessage(string tag, object[] contents)
        {
            IGC.SendBroadcastMessage<object[]>(tag, contents, TransmissionDistance.TransmissionDistanceMax);
            //IGC.SendBroadcastMessage<object[]>(tag, contents, TransmissionDistance.TransmissionDistanceMax);
        }


        //Receive data
        public void RecieveMessage(int listener)
        {
            //Define message and bool to check if its a broadcast or not. Bool may not be needed.
            //bool isBroadcast = false;
            MyIGCMessage message;

            //Check if it's a uni or broadcast and accept msg
            if (listener == 0)
            {
                //Unicast
                message = dirListener.AcceptMessage();
            } else
            {
                //Broadcast
                message = listeners[listener - 1].AcceptMessage();
                //isBroadcast = true;
            }

            //Convert to object
            Echo(message.Data.ToString());
            string temp = message.Data.ToString();
            object[] finalMsg = stringToObject(temp);
            LCD[0].WriteText(displayThing(finalMsg));

            object[] packetContent = finalMsg[3] as object[];

            switch (finalMsg[2].ToString())
            {
                case "EstCon":

                    //B: EstCon - [long source, long destination, "EstCon", [EstType, gridType, laserAntPos]]
                    //U: EstCon - [long source, long destination, "EstCon", [gridType, laserAntPos]]
                    //Add the IP to the IP list if is doesn't exist

                    if (!(ipList.ContainsKey(finalMsg[0].ToString())))
                    {
                        ipList.Add(finalMsg[0].ToString(), new object[] { packetContent[2].ToString(), packetContent[2] });
                    }

                    //while (long) object is more readible, unboxing uses a lot of performace. May need to do some performance testing
                    long ip = long.Parse(finalMsg[0] as string);

                    //Send message back to sender.
                    IGC.SendUnicastMessage<string>(ip, ip.ToString(), createPacketObject(pBId.ToString(), ip.ToString(), "EstCon", new object[] { gridType, laserAntPos }));
                    //IGC.SendUnicastMessage(ip, ip.ToString(), createPacket(pBId.ToString(), ip.ToString(), "EstCon", new object[] { gridType, laserAntPos } ));
                    break;

                case "Distress":
                    break;
                default:
                    break;
            }
        }


        //Creates EstCon packet
        public void establishConnection(string estType)
        {
            //B: EstCon - [long source, long destination, "EstCon", [EstType, gridType, laserAntPos]]
            //Creates an EstCon broadcast packet. EstType tells other grids what grid types it wants. E.g. if estType is Outpost, only outposts will return data.
            IGC.SendBroadcastMessage("EstCon", createPacketObject(pBId.ToString(), "All", "EstCon", new object[] { estType, gridType, laserAntPos }));
            Echo("Sent EstCon broadcast to grid type: " + estType);
            //IGC.SendBroadcastMessage("EstCon", createPacket(pBId.ToString(), "All", "EstCon", new object[] { estType, gridType, laserAntPos } ), TransmissionDistance.TransmissionDistanceMax);
        }


        public void Init()
        {
            Echo("Retrieving LaserAnt list...");
            //Get all laser antennas and convert to object array
            List<IMyLaserAntenna> laserAnts = new List<IMyLaserAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(laserAnts);

            //Just keeps getting errors atm
            //laserAntPos = laserAnts.Select(x => x.GetPosition()).ToArray();
            laserAntPos = new object[laserAnts.Count];

            for (int i = 0; i < laserAnts.Count; i++)
            {
                laserAntPos[i] = laserAnts[i].GetPosition();
            }


            Echo("Setting listerers");
            //Define uni and broadcast listeners. Direct sets are "default" tags, meaning all comms have these tags by default.
            string[] tagArr = new string[] { "EstCon", "LaserAnt", "Distress", "All" };

            foreach (string tag in tagArr)
            {
                listeners.Add(IGC.RegisterBroadcastListener(tag));
            }


            //It seems that although unicasts require a tag, the reciever does not need the tag to read the message. It seems the tag is
            //more for grids that have multiple PBs which is strange as the address is the PBs ID.
            dirListener = IGC.UnicastListener;

            //Disable callback for all messages (until I figure out how to use them)
            foreach (IMyBroadcastListener listener in listeners)
            {
                listener.DisableMessageCallback();
            }

            Echo("Retrieving grid blocks...");
            //Get blocks and ID
            mainProgBlock = GridTerminalSystem.GetBlockWithName("MainProgBlock");
            antenna = GridTerminalSystem.GetBlockWithName("Antenna") as IMyRadioAntenna;

            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(LCD);

            pBId = mainProgBlock.EntityId;

            setup = true;
            Echo("Active");
        }



        //Default game functions ---------
        public Program()
        {
            //Add default and known IPs to ipList
            ipList.Add("EstCon", new object[] { "Default" });
            ipList.Add("Distress", new object[] { "Default" });
            ipList.Add("All", new object[] { "Default" } );


            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        bool runOnce = true;

        void Main(string argument, UpdateType updateSource)
        {

            //Argument is empty unless all the other functions have been commented/removed.

            //Initialise once
            if (!setup)
            {
                gridType = argument;
                Init();
                setup = true;
            }
            gridPos = Me.CubeGrid.GetPosition();


            //testing:

            //Test packet

            if (gridType == "Satellite")
            {
                Echo("Sending EstCon...");
                establishConnection("Outpost");
                runOnce = false;
            }

            //Delete this if it works


            //Get the satellite to send an EstCon to the outpost. 
            //Need to find a way to display debug and packet information


            //Handling messages here. Seems messy and inefficient
            //By checking all listeners on a single frame, all information can be processed quickly. Because objects can be sent instead of strings,
            //this process shouldn't be performance heavy.

            if (dirListener.HasPendingMessage)
            {
                RecieveMessage(0);
            }
            
            for (int i = 1; i <= listeners.Count; i++)
            {
                if (listeners[i - 1].HasPendingMessage)
                {
                    RecieveMessage(i);
                }
            }

            //-----------------------------------------
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

Any default tags are a broadcast function. However, the response will most likely be a unicast.

I currently don't understand how callback messages work. The API is useless and there is barely any information about it. I will either test it, or 
I will simply create an ACK packet. While not important for some protocols, it is important for commands and whatnot.

the argument may be a data string. Planning needed.


Try to have the same script for all grids. May not be efficient in terms of memory, but helps with logistics and compatibility. Performance 
shouldn't be affected as most variables and conditions will be set in the init function.

Broadcast listeners do not need to be checked if they have a message for every frame. Even if they have multiple messages, they will still
go through them in order. Coroutines may be able to be used when checking all listeners.


Testing:

1. see if data-to-string and string-to-data works. 
2. Send an EstCon packet to the satellite and decode it. Ensure LaserSat info is included.





Packet structure - [long source, long destination, string purpose, [content, content, etc]]

B: EstCon - [long source, long destination, "EstCon", [EstType, gridType, laserAntPos ]]
U: EstCon - [long source, long destination, "EstCon", [gridType, laserAntPos ]]

Distress - [long source, long destination, "Distress", [position, dangerLvl ]]
Info - [long source, long destination, "Info", [position, gridType, health, power, hydrogen, status, task ]]

Status - idle, working, attached - pBId, etc



IpList plan:

When ships are created, they will send an EstCon broadcast to an outpost. Data will be exchanged and the outpost will send their IP list.
Every so often, the main outpost will send an "All" broadcast, meaning all grids will receive the msg containing the IP list. This ensures all grids
are kept updated. This information will be lost on log off, so upon initiallisation, all grids will send an EstCon request. 

All grids will store a list of satellites + laser antenna positions

*/