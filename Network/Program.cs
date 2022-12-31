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

    public static class Debug
    {
        static Dictionary<int, List<string>> debugMessages = new Dictionary<int, List<string>>();
        public static List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
        static bool debug = true;

        public static void AddLog(int screen, string message)
        {
            if (debug)
            {
                if (!debugMessages.ContainsKey(screen))
                {
                    debugMessages.Add(screen, new List<string>());
                }
                //Add message
                debugMessages[screen].Add(message);

                //Remove first message in list
                if (debugMessages[screen].Count > 17)
                {
                    debugMessages[screen].RemoveAt(0);
                }
            }
        }

        public static void Display()
        {
            //Loop through dictionary, display all messages
            for (int i = 0; i < debugMessages.Count; i++)
            {
                KeyValuePair<int, List<string>> message = debugMessages.ElementAt(i);
                string output = "";

                for (int j = 0; j < message.Value.Count; j++)
                {
                    output += message.Value[i] + "\n";
                }

                textPanels[message.Key].WriteText(output);
            }
        }
    }

    partial class Program : MyGridProgram
    {
        //Blocks
        List<IMyLaserAntenna> laserAnts = new List<IMyLaserAntenna>();
        List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
        
        IMyRemoteControl remoteControl;

        //Networking vars
        List<string> tags = new List<string>() { "all" };
        bool anonCasts = true;
        Dictionary<long, List<Vector3D>> addresses = new Dictionary<long, List<Vector3D>>();
        long packetBacklogId;
        Dictionary<long, MyTuple<string, string, object[]>> packetBacklog = new Dictionary<long, MyTuple<string, string, object[]>>();

        string gridType;

        //Debug vars


        //For anon casts
        void SetAntennas(bool status)
        {
            for (int i = 0; i < antennas.Count; i++)
            {
                antennas[i].EnableBroadcasting = status;
            }
            Debug.AddLog(0, $"Setting antennas: {status}");
        }

        //Send message either direct or with broadcast
        IEnumerator<int> SendMessage(string destination, string purpose, object[] content, bool anon)
        {
            Echo($"1 {Me.CubeGrid.EntityId} {destination} {purpose}");
            //Generate Packet
            ImmutableArray<string> packet = ImmutableArray.Create(new string[] { Me.CubeGrid.EntityId.ToString(), destination, purpose, Network.ObjectToString(content) });
            long address;

            Echo("2");
            if (long.TryParse(destination, out address))
            {
                Echo("3");
                //Check if endpoint is reachable, use unicast
                if (IGC.IsEndpointReachable(address))
                {
                    Echo("4");
                    IGC.SendUnicastMessage(address, destination, packet);
                    Debug.AddLog(0, $"Sending packet: {destination}, {purpose}, {content}");
                } else 
                {
                    Echo("5");
                    //Turn on antennas, wait 1 tick
                    SetAntennas(true);
                    yield return 0;

                    IGC.SendBroadcastMessage("all" , packet);

                    if (IGC.IsEndpointReachable(address))
                    {
                        Echo("6");
                        IGC.SendUnicastMessage(address, destination, packet);
                        Debug.AddLog(0, $"Sending packet: {destination}, {purpose}, {content}");
                    } else
                    {
                        Echo("7");
                        IGC.SendBroadcastMessage(address.ToString(), packet);
                        
                        //Not sure how to go about this...
                        packetBacklog.Add(packetBacklogId, new MyTuple<string, string, object[]>(destination, purpose, content));
                        Debug.AddLog(0, $"Destination unreachable: {destination}, {purpose}, {content}");
                    }

                }
            } else
            {
                Echo("8");
                SetAntennas(true);
                yield return 0;

                IGC.SendBroadcastMessage(destination, packet);
                Debug.AddLog(0, $"Broadcasting packet: {destination}, {purpose}");
                Echo("9");
            }

            Echo("9");
            //Disable once done
            if (anon) { yield return 0; SetAntennas(false); }
        }

        void EstCon(string destination)
        {
            //Gets all laser antenna positions
            object[] antennaPos = new object[laserAnts.Count];
            for (int i = 0; i < antennaPos.Length; i++)
            {
                antennaPos[i] = Vector3DExtensions.ConvertToLocalPosition(laserAnts[i].GetPosition(), remoteControl);
            }

            TaskScheduler.SpawnCoroutine(new Func<string, string, object[], bool, IEnumerator<int>>(SendMessage), destination, "EstCon", antennaPos, anonCasts);
        }

        void ReceiveMessage(ImmutableArray<string> packet)
        {
            //Get all info from packet
            long source = long.Parse(packet[0]);
            string destination = packet[1];
            string purpose = packet[2];
            object[] content = Network.StringToObject(packet[3]);

            Debug.AddLog(1, $"Received packet from {source}: {purpose}, {content}");

            switch (purpose)
            {
                case "EstCon":
                    //If the IP already exists, don't bother processing.
                    if (!addresses.ContainsKey(source))
                    {
                        addresses.Add(source, content.Cast<Vector3D>().ToList());
                        long temp;

                        //Send info to grid that is requesting
                        if (!long.TryParse(destination, out temp))
                        {
                            EstCon(source.ToString());
                        }
                    }
                    break;

                default:
                    Debug.AddLog(1, $"ERROR: Invalid purpose - {purpose}");
                    break;
            }
        }

        void Init()
        {
            //Get all blocks
            GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(laserAnts);
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(Debug.textPanels);

            remoteControl = GridTerminalSystem.GetBlockWithName("rc") as IMyRemoteControl;

            packetBacklogId = 0;
            //Tags:
            tags.Add(Me.CubeGrid.EntityId.ToString());
        }

        //!Initialise some variables here
        public Program()
        {
            TaskScheduler.EstablishTaskScheduler(Runtime, Echo, true);
            TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(IEnumMain));
        }

        void Main(string argument, UpdateType updateSource)
        {
            //Only set gridType if argument is not empty. Prevents overwriting gridType to an empty string.
            if (!(string.IsNullOrEmpty(argument)))
            {
                gridType = argument;
            }

            Init();
            TaskScheduler.StepCoroutines(updateSource, argument);

        }

        public IEnumerator<int> IEnumMain()
        {
            //Initialise listeners
            List<IMyBroadcastListener> broadcastListeners = new List<IMyBroadcastListener>();
            IMyUnicastListener unicastListener = IGC.UnicastListener;

            for (int i = 0; i < tags.Count; i++)
            {
                broadcastListeners.Add(IGC.RegisterBroadcastListener(tags[i]));
            }

            //Intervals
            TimeInterval EstConInterval = new TimeInterval();

            while (true)
            {
                if (gridType == "Outpost")
                {
                    if (EstConInterval.waitInterval(TimeSpan.FromSeconds(5)))
                    {
                        EstCon("all");
                    }
                }


                //Listeners

                yield return 0;

                //Loop through broadcast listeners
                for (int i = 0; i < broadcastListeners.Count; i++)
                {
                    IMyBroadcastListener broadcastListener = broadcastListeners[i];
                    //If there's a message, convert to string array and run ReceiveMessage
                    if (broadcastListener.HasPendingMessage)
                    {                        
                        ReceiveMessage((ImmutableArray<string>) broadcastListener.AcceptMessage().Data);
                    }
                }

                //If there's a message, convert to string array and run ReceiveMessage
                if (unicastListener.HasPendingMessage)
                {
                    ReceiveMessage((ImmutableArray<string>) unicastListener.AcceptMessage().Data);
                }

                Debug.Display();
                yield return 0;
            }
        }

        

        public void Save()
        {

        }
    }
}