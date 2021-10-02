using System;
using System.Collections.Generic;

namespace SatCom
{
    //custom vector3 class so that toString method is the same in SE
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
        //Converts a string back into a vector3
        public static Vector3 StringToVector3(string sVector)
        {
            //Remove curly brackets
            if (sVector.StartsWith("{") && sVector.EndsWith("}"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            //Split the string where there is whitespace
            string[] sArray = sVector.Split(' ');

            //Parse the values into floats and create a Vector3
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
            Dictionary<string, List<int>> bracketPos = new Dictionary<string, List<int>>();

            bracketPos.Add("[", new List<int>());
            bracketPos.Add("]", new List<int>());

            //Loop through string, checking for brackets
            for (int i = 0; i < packet.Length; i++)
            {
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
            
            //Loop through all objects
            foreach (object item in packet)
            {
                if (item.GetType() == typeof(object[]))
                {
                    //If it is an object array, use recursion
                    final += objectToString((object[]) item);
                } else
                {
                    //If not, convert type to string. If its a string, add ""
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

        static void Main(string[] args)
        {
            object[] drone = new object[] { "43242345", "243525", new object[] { "312343", new Vector3(2, 3, 4), 23, new object[] { new Vector3(2, 4, 5), 23, "232" } }, new object[] { "test", new object[] { "brrr", 434.34, new Vector3(3, 54, 1) }, 123 } };

            string msg = objectToString(drone);

            Console.WriteLine(msg);

            object[] test = stringToObject(msg);

            Console.WriteLine(displayThing(test));
        }
    }
}

/*
Packet structure:

[source, destination, [content]]

 
 */