using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace SatCom
{
    //custom vector3 class so that toString is the same in SE
    public class Vector3
    {
        double x;
        double y;
        double z;

        public Vector3(double fX, double fY, double fZ)
        {
            x = fX;
            y = fY;
            z = fZ;
        }

        public string toString()
        {
            return "{X:" + x + " Y:" + y + " Z:" + z + "}";
        }
    }


    public class IMyBroadcastListener
    {
        string tag;
        public IMyBroadcastListener (string tagArg)
        {
            tag = tagArg;
        }
    }

    class Program
    {
        //Global vars:

        public static Dictionary<string, List<int>> bracketPos = new Dictionary<string, List<int>>();
        public static Dictionary<string, object[]> ipList = new Dictionary<string, object[]>();

        public static List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();

        //Converts a string back into a vector3
        static Vector3 StringToVector3(string sVector)
        {
            //Remove curly brackets
            if (sVector.StartsWith("{") && sVector.EndsWith("}"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            //Split the string by whitespace
            string[] sArray = sVector.Split(' ');

            //Parse values into floats and create Vector3
            Vector3 position = new Vector3(
                float.Parse(sArray[0].Substring(2, sArray[0].Length - 2)),
                float.Parse(sArray[1].Substring(2, sArray[1].Length - 2)),
                float.Parse(sArray[2].Substring(2, sArray[2].Length - 2))
            );

            return position;
        }

        //Returns square bracket pos in string
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
                } else if (packet[i] == ']')
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
                    final += objectToString((object[]) item);
                } else
                {
                    //If not, convert type to string. If its a string, add "", if vec3 use toString, etc
                    if (item.GetType() == typeof(string))
                    {
                        final += "\"" + item + "\"";
                    } else if (item.GetType() == typeof(Vector3)) {
                        Vector3 vec3 = (Vector3) item;
                        final += vec3.toString();

                    } else
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

        static string objectToString2(object[] packet)
        {
            StringBuilder final = new StringBuilder("[", 200);
            int i = 0;

            //Loop through all items in object
            foreach (object item in packet)
            {
                if (item.GetType() == typeof(object[]))
                {
                    //If it is an object array, use recursion
                    final.Append(objectToString((object[])item));
                }
                else
                {
                    //If not, convert type to string. If its a string, add "", if vec3 use toString, etc
                    if (item.GetType() == typeof(string))
                    {
                        final.Append("\"" + item + "\"");
                    }
                    else if (item.GetType() == typeof(Vector3))
                    {
                        Vector3 vec3 = (Vector3)item;
                        final.Append(vec3.toString());

                    }
                    else
                    {
                        final.Append(item);
                    }
                }

                //Prevent adding unnecessary comma
                if (i < packet.Length - 1)
                {
                    final.Append(",");
                }

                i++;
            }

            final.Append("]");

            return final.ToString();
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
            foreach(int potChild in linked.Keys)
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
                    Vector3 temp = array[i] as Vector3;
                    output += temp.toString();
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
        static string createPacket(string source, string destination, string purpose, object[] packet)
        {
            string finalPacket = "[" + source + "," + destination + "," + purpose + "," + objectToString(packet) + "]";
            //terminal.broadcast(source, finalPacket);
            return finalPacket;
        }



        /*/These two functions are broken atm. Need to make pseudo classes. "offline" testing purposes only

        //Send data. This function might be obselete.
        public static void SendMessage(string tag, string contents)
        {
            IGC.SendBroadcastMessage<string>(tag, contents, TransmissionDistance.TransmissionDistanceMax);
        }

        //Receive data
        public static void RecieveMessage(int listener)
        {
            //Define message and bool to check if its a broadcast or not
            //bool may not be needed.
            bool isBroadcast = false;
            MyIGCMessage message;

            //Check if it's a uni or broadcast and accept msg
            if (listener == 0)
            {
                message = dirListener.AcceptMessage();
            }
            else
            {
                message = listeners[listener - 1].AcceptMessage();
                isBroadcast = true;
            }

            //Convert to object
            object[] finalMsg = stringToObject(message.ToString());


            object[] packetContent = finalMsg[3] as object[];
            switch (finalMsg[2].ToString())
            {
                //This is a bit of a hot mess atm. Need to cleanup
                case "EstCon":

                    //B: EstCon - [long source, long destination, "EstCon", [EstType, gridType, laserAntPos]]
                    //U: EstCon - [long source, long destination, "EstCon", [gridType, laserAntPos]]
                    //Add the IP to the IP list if is doesn't exist

                    if (!(ipList.ContainsKey(finalMsg[0].ToString())))
                    {
                        ipList.Add(finalMsg[0].ToString(), new object[] { packetContent[2].ToString(), packetContent[2] });
                    }
                    //Save IP to a var for readability
                    long ip = long.Parse(finalMsg[0].ToString());
                    //Send message back to sender.
                    IGC.SendUnicastMessage(ip, ip.ToString(), createPacket(pBId.ToString(), ip.ToString(), "EstCon", new object[] { gridType, laserAntPos }));
                    break;

                case "Distress":
                    break;
                default:
                    break;
            }
        }

        public static void establishConnection(string estType)
        {
            //B: EstCon - [long source, long destination, "EstCon", [EstType, gridType, laserAntPos]]
            //Creates an EstCon broadcast packet. EstType tells other grids what grid types it wants. E.g. if estType is Outpost, only outposts will return data.
            IGC.SendBroadcastMessage("EstCon", createPacket(pBId.ToString(), "All", "EstCon", new object[] { estType, gridType, laserAntPos }), TransmissionDistance.TransmissionDistanceMax);
        }

        public static void Init()
        {
            //Define uni and broadcast listeners. Direct sets are "default" tags, meaning all comms have these tags by default.
            string[] tagArr = new string[] { "EstCon", "LaserAnt", "Distress" };

            foreach (string tag in tagArr)
            {
                listeners.Add(new IMyBroadcastListener(tag));
            }


            //It seems that although unicasts require a tag, the reciever does not need the tag to read the message. It seems the tag is
            //more for grids that have multiple PBs.
            
            //dirListener = IGC.UnicastListener;
        }*/


        static void Main(string[] args)
        {
            //Init();

            //Create test object and convert to string
            object[] drone = new object[] { "43242345", "243525", new object[] { "312343", new Vector3(2, 3, 4), 23, new object[] { new Vector3(2, 4, 5), 23, "232" } }, new object[] { "test", new object[] { "brrr", 434.34, new Vector3(3, 54, 1) }, 123 } };

            Stopwatch timer = new Stopwatch();
            timer.Start();
            string msg = objectToString(drone);
            Console.WriteLine(msg);
            timer.Stop();

            Console.WriteLine("Time: " + timer.ElapsedMilliseconds.ToString());


            //Convert test string back into object
            timer.Reset();
            timer.Start();
            object[] test = stringToObject(msg);
            Console.WriteLine(displayThing(test));
            timer.Stop();
            Console.WriteLine("Time: " + timer.ElapsedMilliseconds.ToString());
        }
    }
}

/*
Packet structure:

[source, destination, [content]]

REMEMBER: While memory optimisation would be nice, performance is the most important thing, even at the cost of memory.

TODO:
 - Potentially use corountines during the encoding and decoding of the packet
 - Reduce the amount of times lists are created and instead try to reuse ones that have already been created
 - Create a function that creates a packet (source, dest, content) and decodes it
 - Use regex to find all instances of brackets in a string.


Some notes:

What's the point of including the destination in the packet when the receiver already knows it's meant for them? Currently, it's precautionary.
One possible reason could be that it lets the receiver know if the msg was only meant for itself, or a group of receivers.

It seems that either clearing a list/dictionary or creating a new one depends on some factors such as size. Due to the small size of most lists,
clearing the list is generally a better option, with it only struggling once there is over 100k items in it.
 

The code here heavily relies on recursion. If there is an error in the packet information, this could potentially cause performance issues.
 */