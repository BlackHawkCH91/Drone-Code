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
        public static List<IMyTextPanel> textPanels;
        static bool debug = true;

        public static void AddLog(int screen, string message)
        {
            if (debug)
            {
                //Add message
                debugMessages[screen].Add(message);

                //Remove first message in list
                if (debugMessages[screen].Count > 17)
                {
                    debugMessages[screen].RemoveAt(0);
                }
            }
        }

        public static void display()
        {
            //Loop through dictionary, display all messages
            foreach (KeyValuePair<int, List<string>> message in debugMessages)
            {
                string output = "";

                foreach (string item in message.Value)
                {
                    output += item + "\n";
                }

                textPanels[message.Key].WriteText(output);
            }
        }
    }

    partial class Program : MyGridProgram
    {
        //Blocks
        List<IMyLaserAntenna> laserAnts;
        List<IMyRadioAntenna> antennas;
        
        IMyRemoteControl remoteControl;

        //Networking vars
        string[] tags = new[] { "all" };
        bool anonCasts = false;
        Dictionary<long, List<Vector3D>> addresses = new Dictionary<long, List<Vector3D>>();
        Queue<MyTuple<string, string, object[]>> PacketBacklog = new Queue<MyTuple<string, string, object[]>>();
        
        string gridType;

        //Debug vars


        void SetAntennas(bool status)
        {
            foreach (IMyRadioAntenna ant in antennas)
            {
                ant.Enabled = status;
            }
            Debug.AddLog(1, $"Setting antennas: {status}");
        }

        IEnumerator<int> SendMessage(string destination, string purpose, object[] content, bool anon)
        {
            bool enabled = false;
            foreach (IMyRadioAntenna antenna in antennas)
            {
                try
                {
                    enabled = antenna.Enabled;

                    if (enabled) { break; }
                }
                catch { }
            }
            //Enable antennas, wait for a tick

            //Generate Packet
            ImmutableArray<string> packet = new ImmutableArray<string>() { Me.CubeGrid.EntityId.ToString(), destination, purpose, Network.ObjectToString(content)};
            long address;

            if (long.TryParse(destination, out address))
            {
                //Check if endpoint is reachable using laser ants
                if (IGC.IsEndpointReachable(address))
                {
                    IGC.SendUnicastMessage(address, destination, packet);
                } else 
                {
                    SetAntennas(true);
                    yield return 0;

                    if (IGC.IsEndpointReachable(address))
                    {
                        IGC.SendUnicastMessage(address, destination, packet);
                    } else
                    {
                        //Queue packet if address is not reachable
                        PacketBacklog.Enqueue(new MyTuple<string, string, object[]>(destination, purpose, content)); 
                    }

                }
            } else
            {
                SetAntennas(true);
                yield return 0;

                IGC.SendBroadcastMessage(destination, packet);
            }

            //Disable once done
            if (anon) { SetAntennas(false); }
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

            switch (purpose)
            {
                case "EstCon":
                    addresses.Add(source, content.Cast<Vector3D>().ToList());
                    long temp;

                    if (!long.TryParse(destination, out temp))
                    {
                        EstCon(source.ToString());
                    }

                    break;
            }
        }

        void Init()
        {
            GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(laserAnts);
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(Debug.textPanels);

            remoteControl = GridTerminalSystem.GetBlockWithName("rc") as IMyRemoteControl;
        }

        //!Initialise some variables here
        public Program()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update100;
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

            foreach (string tag in tags)
            {
                broadcastListeners.Add(IGC.RegisterBroadcastListener(tag));
            }


            while (true)
            {
                if (gridType == "Output")
                {

                }


                //Listeners

                yield return 0;

                //Loop through broadcast listeners
                foreach (IMyBroadcastListener broadcastListener in broadcastListeners)
                {
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

                Debug.display();
                yield return 0;
            }
        }

        

        public void Save()
        {

        }
    }
}