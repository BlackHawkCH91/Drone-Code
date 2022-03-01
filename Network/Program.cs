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
using System.Text.RegularExpressions;
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
    public class TimeInterval
    {
        public DateTime intervalTime = DateTime.Now;

        public bool waitUntil(DateTime timeToWait)
        {
            return DateTime.Now >= timeToWait;
        }

        public bool waitInterval(TimeSpan interval)
        {
            if (DateTime.Now >= intervalTime)
            {
                //intervalTime.Add(interval);
                intervalTime = intervalTime.AddSeconds(5);
                return true;
            }
            return false;
        }
    }

    public struct drone
    {
        //[gridType, groupId, worldMatrix, linearVelocity, angularVelocity, health, status, command, lastUpdate]
        public string gridType { get; set; }
        public long groupId { get; set; }
        public MatrixD gridMatrix { get; set; }
        public Vector3D linVel { get; set; }
        public Vector3D angVel { get; set; }
        public double health { get; set; }
        public string status { get; set; }
        public string command { get; set; }
        public DateTime lastUpdate { get; set; }

        public drone(string _gridType, long _groupID, MatrixD _gridMatrix, Vector3D _linVel, Vector3D _angVel, double _health, string _status, string _command, DateTime _lastUpdate)
        {
            gridType = _gridType;
            groupId = _groupID;
            gridMatrix = _gridMatrix;
            linVel = _linVel;
            angVel = _angVel;
            health = _health;
            status = _status;
            command = _command;
            lastUpdate = _lastUpdate;
        }
    }

    partial class Program : MyGridProgram
    {
        //!VARS

        Dictionary<long, ImmutableArray<string>> packetBacklog = new Dictionary<long, ImmutableArray<string>>();
        Dictionary<long, object[]> ipList = new Dictionary<long, object[]>();
        List<object[]> updatedIpList = new List<object[]>();
        List<string> tagList = new List<string>();
        Dictionary<string, List<int>> bracketPos = new Dictionary<string, List<int>>();

        //[gridType, groupId, worldMatrix, movementVector, health, status, command, lastUpdate]
        Dictionary<long, drone> droneTable = new Dictionary<long, drone>();

        //!Health stuff
        Vector3I gridMin;
        Vector3I gridMax;

        bool setup = false;

        double terminalHealth;
        double armourHealth;
        double maxTerminalHealth;
        double maxArmourHealth;

        double gridHealth = 0;

        Dictionary<IMySlimBlock, BoundingBox> terminalBlocks = new Dictionary<IMySlimBlock, BoundingBox>();
        List<Vector3I> armourBlocks = new List<Vector3I>();

        bool blocksCached;

        //Listeners and display for sending and recieving data
        List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
        IMyUnicastListener dirListener;
        IMyRadioAntenna antenna;
        IMyTerminalBlock mainProgBlock;
        IMyRemoteControl rc;
        public List<IMyTextPanel> LCD = new List<IMyTextPanel>();

        //!Information used to create a packet that will be sent back to the main base.
        long pBId;
        string gridType;
        MatrixD gridMatrix;
        Vector3D linearVelocity;
        Vector3D angularVelocity;
        object[] laserAntPos;
        int[] ticks = new int[] { 0 };
        bool showOnce = true;


        //?Functions ----------------------------------------------------------------------


        //!String-to-data and data-to-string functions hidden here:

        //!Convert MatrixD to string... I'm sorry, but there is no way to loop through properties
        public static string MatrixToString(MatrixD matrix)
        {
            string strMatrix = "M" + matrix.M11 + "|" + matrix.M12 + "|" + matrix.M13 + "|" + matrix.M14 + "|" +
                            matrix.M21 + "|" + matrix.M22 + "|" + matrix.M23 + "|" + matrix.M24 + "|" +
                            matrix.M31 + "|" + matrix.M32 + "|" + matrix.M33 + "|" + matrix.M34 + "|" +
                            matrix.M41 + "|" + matrix.M42 + "|" + matrix.M43 + "|" + matrix.M44;

            return strMatrix;
        }

        //!Convert string to matrixD
        public static MatrixD StringToMatrix(string strMatrix)
        {
            string temp = strMatrix.Substring(1);

            //Get all matrix cells
            string[] strNum = temp.Split('|');

            //Create array and convert to double
            double[] num = new double[16];

            for (int i = 0; i < 16; i++)
            {
                num[i] = double.Parse(strNum[i]);
            }

            //Painfully insert each value
            MatrixD matrix = new MatrixD(num[0], num[1], num[2], num[3],
                                num[4], num[5], num[6], num[7],
                                num[8], num[9], num[10], num[11],
                                num[12], num[13], num[14], num[15]);

            return matrix;
        }

        //!Converts a string back into a Vector3
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

        //!Gets positions of [] in strings
        Dictionary<string, List<int>> GetBracketPos(string packet)
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


        //!Converts object to string
        //Probably not worth converting to coroutine
        string ObjectToString(object[] packet)
        {
            //objToStrConverter(packet, ref final);

            string final = "[";
            int i = 0;

            //Loop through all items in object
            foreach (object item in packet)
            {
                if (item.GetType() == typeof(object[]))
                {
                    //If it is an object array, use recursion
                    final += ObjectToString((object[])item);
                }
                else
                {
                    //If not, convert type to string. If its a string, add "", if vec3 use toString, etc
                    switch (item.GetType().ToString())
                    {
                        case "System.String":
                            final += "\"" + item + "\"";
                            break;
                        case "System.DateTime":
                            final += "D" + item.ToString();
                            break;
                        case "System.Boolean":
                            final += item.ToString();
                            break;

                        case "VRageMath.Vector3D":
                            Vector3D vec3 = (Vector3D)item;
                            final += vec3.ToString();
                            break;

                        case "VRageMath.MatrixD":
                            final += MatrixToString((MatrixD)item);
                            break;

                        default:
                            final += item;
                            break;
                    }
                }

                //Prevent adding unnecessary comma
                if (i < packet.Length - 1)
                {
                    final += ",";
                }

                i++;
                //TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(yieldThing)));
            }

            final += "]";

            return final;
        }
        //--------------------------------------------------------------------------------------------


        //!Converts string back into object
        public object[] StringToObject(string packet)
        {
            //Remove start and ending brackets
            packet = packet.Substring(1, packet.Length - 2);

            //Dictionaries to store bracket pos and linked brackets
            Dictionary<int, int> linked = new Dictionary<int, int>();
            Dictionary<string, List<int>> bracketPos = GetBracketPos(packet);

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

                subObjectArr.Add(StringToObject(subObject));

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
                    if (item[0] == '{' || item[0] == 'X')
                    {
                        finalPacketArr[i] = StringToVector3(item);
                    }
                    else if (item[0] == '"')
                    {
                        finalPacketArr[i] = item.Substring(1, item.Length - 2);
                    }
                    else if (item.StartsWith("T") || item.StartsWith("F")) //New thing. Needs testing
                    {
                        finalPacketArr[i] = item.StartsWith("T"); //
                    }
                    else if (item[0] == 'M') // Another new thing
                    {
                        finalPacketArr[i] = StringToMatrix(item);
                    }
                    else if (item[0] == 'D')
                    {
                        finalPacketArr[i] = DateTime.Parse(item);
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


        //!Debugging only, displays object arr as a string
        static string DisplayThing(object[] array)
        {
            string output = "";
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].GetType() == typeof(object[]))
                {
                    output += "[";
                    output += DisplayThing(array[i] as object[]);
                    output += "]";
                }
                else if (array[i].GetType() == typeof(Vector3D))
                {
                    Vector3D temp = (Vector3D)array[i];
                    output += temp.ToString();
                }
                else if (array[i].GetType() == typeof(string))
                {
                    output += "\"" + array[i].ToString() + "\"";
                }
                else if (array[i].GetType() == typeof(MatrixD))
                {
                    output += MatrixToString((MatrixD)array[i]);
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

        //!Creates a valid packet to send through antennas
        ImmutableArray<string> CreatePacketString(string source, string destination, string purpose, object[] packet)
        {
            string tempDest = destination;
            long temp;
            if (!(long.TryParse(destination, out temp)))
            {
                tempDest = "\"" + tempDest + "\"";
            }
            //string[] fdfdsfinalPacket = "[" + source + "," + tempDest + ",\"" + purpose + "\"," + ObjectToString(packet) + "]";
            ImmutableArray<string> finalPacket = ImmutableArray.Create(source, tempDest, purpose, ObjectToString(packet));
            //terminal.broadcast(source, finalPacket);
            return finalPacket;
        }

        //-----------------------------------------------------------------------------



        //!Gets block health
        double GetMyTerminalBlockHealth(IMyTerminalBlock block)
        {
            IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
            double MaxIntegrity = slimblock.MaxIntegrity;
            double BuildIntegrity = slimblock.BuildIntegrity;
            double CurrentDamage = slimblock.CurrentDamage;
            return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
        }


        //!Send packet
        public void SendMessage(bool isUni, string destination, ImmutableArray<string> contents)
        {
            //First check if it's a uni or broadcast
            if (isUni)
            {
                //If reachable, send packet, otherwise, add packet to backlog
                if (IGC.IsEndpointReachable(long.Parse(destination), TransmissionDistance.TransmissionDistanceMax))
                {
                    IGC.SendUnicastMessage<ImmutableArray<string>>(long.Parse(destination), destination, contents);
                }
                else
                {
                    packetBacklog.Add(long.Parse(destination), contents);
                }
            }
            else
            {
                //Send broadcast
                IGC.SendBroadcastMessage<ImmutableArray<string>>(destination, contents, TransmissionDistance.TransmissionDistanceMax);
            }

            //IGC.SendBroadcastMessage<object[]>(tag, contents, TransmissionDistance.TransmissionDistanceMax);
        }


        //!Sends packets in backlog every so often
        public void SendBackLog()
        {
            //Set count to var. This is because the length of dict will change
            int packetCount = packetBacklog.Count;

            for (int i = 0; i < packetCount; i++)
            {
                //Get current packet
                KeyValuePair<long, ImmutableArray<string>> packet = packetBacklog.ElementAt(i);

                //First check if point is reachable
                if (IGC.IsEndpointReachable(packet.Key))
                {
                    //If it is, send message, remove key from dict and subtract 1 from i and count
                    IGC.SendUnicastMessage(packet.Key, packet.Key.ToString(), packet.Value);
                    packetBacklog.Remove(packet.Key);
                    i--;
                    packetCount--;
                }
            }
        }


        //!Receive data
        public void RecieveMessage(int listener)
        {
            Echo("recieving");
            //Define message and bool to check if its a broadcast or not. Bool may not be needed.
            bool isBroadcast = false;
            MyIGCMessage message;

            //Check if it's a uni or broadcast and accept msg
            if (listener == 0)
            {
                //Unicast
                message = dirListener.AcceptMessage();
            }
            else
            {
                //Broadcast
                message = listeners[listener - 1].AcceptMessage();
                isBroadcast = true;
            }

            //Convert to object
            ImmutableArray<string> temp = (ImmutableArray<string>)message.Data;

            object[] finalMsg;
            string source = temp[0];

            finalMsg = new object[] { source, temp[1], temp[2], StringToObject(temp[3]) };
            //Check if destination is a string or long. This may cause an error if dest is a long. Need to test this.
            if (!(finalMsg[1].ToString().StartsWith("\"")))
            {
                long test;
                long.TryParse(temp[1], out test);
                finalMsg[1] = test;
            }
            else
            {
                string dest = temp[1].ToString();
                finalMsg[1] = dest.Substring(1, dest.Length - 2);
            }

            //DEBUG check. Delete later:

            if (gridType == "Outpost" && listener == 0)
            {
                LCD[0].WriteText(DisplayThing(finalMsg));
            }

            object[] packetContent = finalMsg[3] as object[];
            finalMsg[0] = long.Parse(finalMsg[0].ToString());
            switch (finalMsg[2].ToString())
            {
                case "EstCon":
                    //EstCon - [long source, long destination, "EstCon", [gridType, laserAntPos]]

                    //Add the IP to the IP list if is doesn't exist
                    //Echo(test.ToString());
                    if (!(ipList.ContainsKey((long)finalMsg[0])))
                    {
                        ipList.Add((long)finalMsg[0], new object[] { packetContent[0].ToString(), packetContent[1] });
                    }

                    //Will probably change this to a broadcast that sends all drones an updated ip list.
                    if (isBroadcast && (packetContent[0].ToString() == gridType || finalMsg[1].ToString() == "All"))
                    {
                        //Send unicast back to sender.
                        Echo("Sending uni");
                        SendMessage(true, source, CreatePacketString(pBId.ToString(), source, "EstCon", new object[] { gridType, laserAntPos }));
                    }
                    else if (!(isBroadcast))
                    {
                        //?This entire thing is a mess, gonna rewrite it. Commenting for now.
                        //If its a uni (got response), send an ipList update to everyone.
                        //object[] updatedIPList = new object[ipList.Count];
                        updatedIpList.Clear();

                        /*foreach (KeyValuePair<long, object[]> ip in ipList)
                        {
                            if (ip.Key != (long) finalMsg[0])
                            {
                                //updatedIpList[ip.Key] = ip.Value;
                                updatedIpList.Add(new object[] { ip.Key, ip.Value });
                            }
                        }*/

                        //SendMessage(false, "All", CreatePacketString(pBId.ToString(), "All", "IpUpdate", updatedIpList.ToArray()));
                    }

                    break;

                case "IpUpdate":
                    //?Rewrite IP Update in general
                    string output = DisplayThing(packetContent);

                    break;
                case "Info":
                    //[gridType, groupId, worldMatrix, linearVelocity, angularVelocity, health, status, command, lastUpdate]
                    if (isBroadcast)
                    {
                        Echo("sending info");
                        SendMessage(true, source, CreatePacketString(pBId.ToString(), source, "Info", new object[] { gridType, 0, gridMatrix, linearVelocity, angularVelocity, 100, "Idle", "Mine" }));
                    }
                    else
                    {
                        //This is a lot of boxing/unboxing which might cause performance issues. Def needs to run on a coroutine
                        object[] p = packetContent;
                        droneTable[long.Parse(source)] = new drone((string)p[0], long.Parse(p[1].ToString()), (MatrixD)p[2], (Vector3D)p[3], (Vector3D)p[4], long.Parse(p[5].ToString()), (string)p[6], (string)p[7], DateTime.Now);
                    }

                    break;
                default:
                    break;
            }
        }


        //!Creates EstCon packet
        public void EstablishConnection(string estType)
        {
            //B: EstCon - [long source, long destination, "EstCon", [EstType, gridType, laserAntPos]]
            //Creates an EstCon broadcast packet. EstType tells other grids what grid types it wants. E.g. if estType is Outpost, only outposts will return data.
            SendMessage(false, estType, CreatePacketString(pBId.ToString(), estType, "EstCon", new object[] { gridType, laserAntPos }));
            Echo("Sent EstCon broadcast to grid type: " + estType);
        }


        //!Broadcast to request info packets
        public void RequestInfo(string tag)
        {
            SendMessage(false, tag, CreatePacketString(pBId.ToString(), tag, "Info", new object[] { "placeholder" }));
            Echo("Broadcasting info request.");
        }




        //!Initialliser, sets vars and listeners.
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

            tagList.Add("All");
            foreach (string tag in tagList)
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



        //?Default game functions ---------

        //!Initialise some variables here
        public Program()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update100;
            TaskScheduler.EstablishTaskScheduler(Runtime, Echo, true);
            TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(IEnumMain));
        }

        int newLines = 1;

        void Main(string argument, UpdateType updateSource)
        {
            //Only set gridType if argument is not empty. Prevents overwriting gridType to an empty string.
            //This may be used instead of a bool (it seems bools are inconsistent). 
            if (!(string.IsNullOrEmpty(argument)))
            {
                gridType = argument;
                //ipList[Me.EntityId] = new object[] { gridType, laserAntPos };
            }

            //Initialise once
            if (!setup)
            {
                Init();

                //String-to-data and data-to-string testing. Only use when adding new data types
                //
                /*object[] testObject = new object[] { mainProgBlock.WorldMatrix, mainProgBlock.GetPosition(), "Hello", 123, 123.456, true, false, new object[] { "more", true, false } };
                string testString = ObjectToString(testObject);

                object[] testObject2 = StringToObject(testString);
                string testString2 = DisplayThing(testObject2);

                LCD[3].WriteText(MatrixToString(mainProgBlock.WorldMatrix));
                LCD[3].WriteText(testString + "\n" + testString2);*/


                setup = true;
                //Remove this and set update frequency in program
                //Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }

            object[] test = new object[] { "hello", 123 };


            TaskScheduler.StepCoroutines(updateSource, argument);

        }

        //object[] testThing = new object[] { "43242345", "243525", new object[] { "312343", new Vector3(2, 3, 4), 23, new object[] { new Vector3(2, 4, 5), 23, "232" } }, new object[] { "test", new object[] { "brrr", 434.34, new Vector3(3, 54, 1) }, 123 } };
        object[] testThing = new object[] { new Vector3D(2, 3, 4), "hello", 123, true };

        public IEnumerator<int> IEnumMain()
        {
            rc = GridTerminalSystem.GetBlockWithName("rc") as IMyRemoteControl;
            while (true)
            {
                MyShipVelocities vel = rc.GetShipVelocities();
                linearVelocity = vel.LinearVelocity;
                angularVelocity = vel.AngularVelocity;

                gridMatrix = Me.CubeGrid.WorldMatrix;

                if (gridType == "Outpost")
                {
                    Echo("Running outpost code.");
                    string displayString = "";
                    foreach (KeyValuePair<long, object[]> item in ipList)
                    {
                        displayString += item.Key + ", ";

                        if (displayString.Length >= (42 * newLines))
                        {
                            displayString += "\n";
                            newLines++;
                        }
                    }

                    LCD[1].WriteText(displayString);


                    displayString = "";

                    foreach (KeyValuePair<long, ImmutableArray<string>> item in packetBacklog)
                    {
                        displayString += item.Key + ", ";

                        if (displayString.Length >= (42 * newLines))
                        {
                            displayString += "\n";
                            newLines++;
                        }
                    }

                    RequestInfo("All");

                    //LCD[2].WriteText(displayString);

                    //[gridType, groupId, worldMatrix, linearVelocity, angularVelocity, health, status, command, lastUpdate]


                    foreach (KeyValuePair<long, drone> drone in droneTable)
                    {
                        drone item = drone.Value;
                        string pos = $"{item.gridMatrix.M41}, {item.gridMatrix.M42}, {item.gridMatrix.M43}";
                        string output = $"{drone.Key} | {item.gridType} | {pos} | {item.lastUpdate}";
                        Echo("drone stuff");
                        LCD[2].WriteText(output);
                    }
                }

                //testing:

                //Test packet

                if (gridType == "Satellite")
                {
                    Echo("Sending EstCon...");
                    EstablishConnection("All");
                }

                //Handling messages here. Seems messy and inefficient
                //Checking all listeners on a single frame. If they all have messages, string-to-data will be running
                //multiple times on a tick. Use coroutines

                //Check uni cast
                if (dirListener.HasPendingMessage)
                {
                    RecieveMessage(0);
                }

                //Chec all broadcast listeners
                string displayListener = "";
                for (int i = 1; i <= listeners.Count; i++)
                {
                    displayListener += "Broad" + i + "\n";
                    if (listeners[i - 1].HasPendingMessage)
                    {
                        displayListener += " true";
                        RecieveMessage(i);
                    }
                }

                yield return 0;
            }
        }

        public void Save()
        {

        }
    }
}

/*

 
TODO:

 - Create an "Info" packet where grids can request info from other grids and those grids will respond with information about them.
 - Create debug screen which shows status of connections between satellites/relays
 - Use corountines when checking listeners. Calling ObjectToString multiple times can be costly.
 - Find a way to create "anonymous" broadcasts (use laser antenna to broadcast briefly so that the ping barely shows up)
 - Create a universal function that can encode and decode all date into/from a string. This will be stored on the "Storage" variable.


 - EstCon broadcast will only run every couple seconds.

   
   Info packet format: [source, destination, purpose, [gridType, groupId, worldMatrix, linearVelocity, angularVelocity, health, status, command, lastUpdate]]
   Status - "working" "idle" "danger" "docked/landed"

   Storage formate: [gridType, groupId, worldMatrix, movementVector, health, status, command, lastUpdate]

   Command Packet format: [source, destination, purpose, [priority, commandType, depends on command type]]
   Command types: "Move" "Group" "Attack" "Mine" "Construct" "Power" "Dock"
    - Move: [..., position]
    - Group: [..., groupId]
    - Attack: [..., targetPos]
    - Mine: Depends on who receives command. Motherships receive bounding box, drones receive a position - [..., bounding box OR position]
    - Construct: Same as Mine
    - Power: [..., true/false] - Tells ship to land and power off. Used for when there's combat and server resources are important.
    - Dock: [..., position, true/false] - Not sure what structure the packet should be. Tells ship to dock at a position.

   Grid network strucute will use a basic hierachry: Outpost -> Group Leader/flagship -> Grids 
   Commands can still be sent directly to grids if needed. 

   Flagships may store data on their group if needed.
   

Testing:

 - Switch EstCon around. Make outpost send EstCon broadcast and see if response from satellite is being sent properly or is being sent to
   backlog
 - Test if backlog is working. Can be tested by making broadcaster range larger than reciever range.


Packet structure - [long source, long destination, string purpose, [content, content, etc]]

B: EstCon - [long source, long destination, "EstCon", [EstType, gridType, laserAntPos ]]
U: EstCon - [long source, long destination, "EstCon", [gridType, laserAntPos ]]

Status - idle, working, attached - pBId, etc
*/