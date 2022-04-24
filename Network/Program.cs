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
        //Network Stuff
        Dictionary<long, ImmutableArray<string>> packetBacklog = new Dictionary<long, ImmutableArray<string>>();
        Dictionary<long, object[]> ipList = new Dictionary<long, object[]>();
        List<object[]> updatedIpList = new List<object[]>();
        List<string> tagList = new List<string>();
        

        //Components and cubeblocks
        static Dictionary<string, MyTuple<MyDefinitionId, Dictionary<string, double>>> blueprints = new Dictionary<string, MyTuple<MyDefinitionId, Dictionary<string, double>>>();
        static Dictionary<string, Dictionary<string, double>> cubeBlocks = new Dictionary<string, Dictionary<string, double>>();
        static List<string> errorList = new List<string>();


        //[gridType, groupId, worldMatrix, movementVector, health, status, command, lastUpdate]
        Dictionary<long, drone> droneTable = new Dictionary<long, drone>();

        //Max health data will be stored in the Storage variable. The list count is useless if the drone is damaged and the server restarts.
        double maxTerminalHealth;
        double maxArmourHealth;

        double gridHealth = 0;

        Dictionary<IMySlimBlock, BoundingBox> terminalBlocks = new Dictionary<IMySlimBlock, BoundingBox>();
        List<Vector3I> armourBlocks = new List<Vector3I>();

        //Listeners and display for sending and recieving data
        List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
        IMyUnicastListener dirListener;
        IMyRadioAntenna antenna;
        IMyTerminalBlock mainProgBlock;
        IMyRemoteControl rc;
        static IMyAssembler assembler;
        List<IMyTextPanel> LCD = new List<IMyTextPanel>();

        //!Information used to create a packet that will be sent back to the main base.
        long pBId;
        string gridType;
        MatrixD gridMatrix;
        Vector3D linearVelocity;
        Vector3D angularVelocity;
        object[] laserAntPos;
        int[] ticks = new int[] { 0 };

        bool anonCast = true;

        //!3D printing stuff
        static IMyProjector miningProj;


        //!Gets block health
        double GetBlockHealth(IMySlimBlock block)
        {
            double MaxIntegrity = block.MaxIntegrity;
            double BuildIntegrity = block.BuildIntegrity;
            double CurrentDamage = block.CurrentDamage;
            return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
        }

        IEnumerator<int> GetGridHealth()
        {
            int counter = 0;

            //Get health of terminal blocks.
            double terminalHealth = 0;
            foreach (IMySlimBlock block in terminalBlocks.Keys)
            {
                counter++;
                terminalHealth += GetBlockHealth(block);

                if (counter >= 200)
                {
                    counter = 0;
                    yield return ticks[0];
                }
            }

            counter = 0;
            double armourHealth = 0;
            //Get health of armour blocks
            foreach (Vector3I armourBlock in armourBlocks)
            {
                counter++;
                if (Me.CubeGrid.CubeExists(armourBlock))
                {
                    armourHealth++;
                }

                if (counter >= 200)
                {
                    counter = 0;
                    yield return ticks[0];
                }
            }

            //Return health of grid with percentage.
            gridHealth = (armourHealth / maxArmourHealth) * 0.25f + (terminalHealth / maxTerminalHealth) * 0.75f;

            yield return ticks[0];
        }


        //!Send packet
        void SendMessage(bool isUni, string destination, ImmutableArray<string> contents)
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
        void SendBackLog()
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

        //!Creates EstCon packet
        void EstablishConnection(string estType)
        {
            //B: EstCon - [long source, long destination, "EstCon", [EstType, gridType, laserAntPos]]
            //Creates an EstCon broadcast packet. EstType tells other grids what grid types it wants. E.g. if estType is Outpost, only outposts will return data.
            SendMessage(false, estType, Network.CreatePacketString(pBId.ToString(), estType, "EstCon", new object[] { gridType, laserAntPos }));
            Echo("Sent EstCon broadcast to grid type: " + estType);
        }


        //!Broadcast to request info packets
        void RequestInfo(string tag)
        {
            SendMessage(false, tag, Network.CreatePacketString(pBId.ToString(), tag, "Info", new object[] { "placeholder" }));
            Echo("Broadcasting info request.");
        }


        //!Receive data
        void RecieveMessage(int listener)
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

            finalMsg = new object[] { source, temp[1], temp[2], Network.StringToObject(temp[3]) };
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
                LCD[0].WriteText(Network.DisplayThing(finalMsg));
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
                        SendMessage(true, source, Network.CreatePacketString(pBId.ToString(), source, "EstCon", new object[] { gridType, laserAntPos }));
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
                    string output = Network.DisplayThing(packetContent);

                    break;
                case "Info":
                    //[gridType, groupId, worldMatrix, linearVelocity, angularVelocity, health, status, command, lastUpdate]
                    if (isBroadcast)
                    {
                        Echo("sending info");
                        SendMessage(true, source, Network.CreatePacketString(pBId.ToString(), source, "Info", new object[] { gridType, 0, gridMatrix, linearVelocity, angularVelocity, gridHealth, "Idle", "Mine" }));
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




        //!Initialliser, sets vars and listeners.
        void Init()
        {
            Echo("Retrieving LaserAnt list...");
            //Get all laser antennas and convert to object array
            List<IMyLaserAntenna> laserAnts = new List<IMyLaserAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(laserAnts);

            //Just keeps getting errors atm
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

            //!Components and cube blocks
            string[] customData = Me.CustomData.Split('|');
            Network.ReadCompFile(customData[0]);
            Network.ReadCubeFile(customData[1]);

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

            TaskScheduler.StepCoroutines(updateSource, argument);

        }

        //object[] testThing = new object[] { "43242345", "243525", new object[] { "312343", new Vector3(2, 3, 4), 23, new object[] { new Vector3(2, 4, 5), 23, "232" } }, new object[] { "test", new object[] { "brrr", 434.34, new Vector3(3, 54, 1) }, 123 } };
        object[] testThing = new object[] { new Vector3D(2, 3, 4), "hello", 123, true };

        public IEnumerator<int> IEnumMain()
        {
            //!Grid block caching
            Vector3I gridMax = Me.CubeGrid.Max;
            Vector3I gridMin = Me.CubeGrid.Min;
            List<IMyTerminalBlock> tempBlocks = new List<IMyTerminalBlock>();

            int counter = 0;
            //Gets bounding box from all terminal blocks.
            GridTerminalSystem.GetBlocks(tempBlocks);

            foreach (IMyTerminalBlock block in tempBlocks)
            {
                if (Me.CubeGrid.IsSameConstructAs(block.CubeGrid))
                {
                    counter++;

                    try
                    {
                        terminalBlocks.Add(Me.CubeGrid.GetCubeBlock(block.Position), new BoundingBox(block.Min, block.Max));
                    } catch
                    {

                    }
                

                    if (counter >= 75)
                    {
                        counter = 0;
                        yield return ticks[0];
                    }
                }
            }

            //Loop through all points on grid
            counter = 0;
            for (int x = gridMin.X - 1; x <= gridMax.X + 1; x++)
            {
                for (int y = gridMin.Y - 1; y <= gridMax.Y + 1; y++)
                {
                    for (int z = gridMin.Z - 1; z <= gridMax.Z + 1; z++)
                    {
                        //Check if point is in any of the terminal blocks
                        bool nextPoint = false;
                        Vector3I point = new Vector3I(x, y, z);

                        if (!Me.CubeGrid.CubeExists(point))
                        {
                            continue;
                        }

                            int newCounter = 0;
                        foreach (BoundingBox boundingBox in terminalBlocks.Values)
                        {
                            newCounter++;
                            if (ContainmentType.Contains == boundingBox.Contains(new Vector3D(point)))
                            {
                                nextPoint = true;
                                break;
                            }

                            if (newCounter >= 20)
                            {
                                newCounter = 0;
                                yield return ticks[0];
                            }
                        }

                        if (nextPoint)
                        {
                            continue;
                        }

                        //If there is an armour block, add it to list.
                        
                        armourBlocks.Add(point);

                        if (counter >= 60)
                        {
                            counter = 0;
                            yield return ticks[0];
                        }
                    }
                }
            }

            //This needs to change when I finally use the storage var
            maxTerminalHealth = terminalBlocks.Count;
            maxArmourHealth = armourBlocks.Count;
            rc = GridTerminalSystem.GetBlockWithName("rc") as IMyRemoteControl;

            TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(GetGridHealth));

            TimeInterval infoInterval = new TimeInterval();
            TimeInterval estInterval = new TimeInterval();

            string testThing = "";

            if (gridType == "Outpost")
            {
                miningProj = GridTerminalSystem.GetBlockWithName("MiningProjector") as IMyProjector;
                testThing = miningProj.RemainingBlocksPerType.ElementAt(0).Key.ToString();
                assembler = GridTerminalSystem.GetBlockWithName("Assembler") as IMyAssembler;
            }

            Init();

            Network.PrintShip();

            while (true)
            {
                Echo(miningProj.RemainingBlocksPerType.Count.ToString());
                /*foreach (KeyValuePair<MyDefinitionId, int> thing in compNeeded)
                {
                    Echo(thing.Key.ToString() + " " + thing.Value.ToString());
                }*/

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

                    if (infoInterval.waitInterval(new TimeSpan(0, 0, 5)))
                    {
                        antenna.EnableBroadcasting = true;
                        yield return 0;
                        RequestInfo("All");
                    }

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

                if (gridType == "Satellite" && estInterval.waitInterval(new TimeSpan(0, 0, 15)))
                {
                    Echo("Sending EstCon...");
                    //EstablishConnection("All");
                }

                //Handling messages here. Seems messy and inefficient
                //Checking all listeners on a single frame. If they all have messages, string-to-data will be running
                //multiple times on a tick. Use coroutines

                Echo($"{terminalBlocks.Count} | {armourBlocks.Count}");

                //Check uni cast
                if (dirListener.HasPendingMessage)
                {
                    //string test = dirListener.AcceptMessage().ToString();
                    antenna.EnableBroadcasting = true;
                    yield return 0;
                    RecieveMessage(0);
                }

                //Check all broadcast listeners
                string displayListener = "";
                for (int i = 1; i <= listeners.Count; i++)
                {
                    displayListener += "Broad" + i + "\n";
                    if (listeners[i - 1].HasPendingMessage)
                    {
                        antenna.EnableBroadcasting = true;
                        yield return 0;
                        displayListener += " true";
                        RecieveMessage(i);
                    }
                }
                if (anonCast) { antenna.EnableBroadcasting = false; }
                
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

 - Create debug screen which shows status of connections between satellites/relays
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

 - Test if backlog is working. Can be tested by making broadcaster range larger than reciever range.


Packet structure - [long source, long destination, string purpose, [content, content, etc]]

B: EstCon - [long source, long destination, "EstCon", [EstType, gridType, laserAntPos ]]
U: EstCon - [long source, long destination, "EstCon", [gridType, laserAntPos ]]

Status - idle, working, attached - pBId, etc
*/