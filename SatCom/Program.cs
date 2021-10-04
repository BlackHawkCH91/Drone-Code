using System;
using System.Collections.Generic;

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

    class Program
    {
        //Global vars:

        public static Dictionary<string, List<int>> bracketPos = new Dictionary<string, List<int>>();


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
        static string createPacket(string source, string destination, object[] packet)
        {
            string finalPacket = "[" + source + "," + destination + "," + objectToString(packet) + "]";
            //terminal.broadcast(source, finalPacket);
            return finalPacket;
        }



        static void Main(string[] args)
        {
            //Create test object and convert to string
            object[] drone = new object[] { "43242345", "243525", new object[] { "312343", new Vector3(2, 3, 4), 23, new object[] { new Vector3(2, 4, 5), 23, "232" } }, new object[] { "test", new object[] { "brrr", 434.34, new Vector3(3, 54, 1) }, 123 } };
            string msg = objectToString(drone);

            Console.WriteLine(msg);

            //Convert test string back into object
            object[] test = stringToObject(msg);
            Console.WriteLine(displayThing(test));
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