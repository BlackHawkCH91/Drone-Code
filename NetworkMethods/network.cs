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
    partial class Program
    {
        public class Network
        {
            static Dictionary<string, List<int>> bracketPos = new Dictionary<string, List<int>>();
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

            //----------------------------------------------------------------------------



            //!Gets positions of [] in strings
            public static Dictionary<string, List<int>> GetBracketPos(string packet)
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
            public static string ObjectToString(object[] packet)
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

            //!Converts string back into object
            public static object[] StringToObject(string packet)
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
            public static string DisplayThing(object[] array)
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
                        output += Network.MatrixToString((MatrixD)array[i]);
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
            public static ImmutableArray<string> CreatePacketString(string source, string destination, string purpose, object[] packet)
            {
                string tempDest = destination;
                long temp;
                if (!(long.TryParse(destination, out temp)))
                {
                    tempDest = "\"" + tempDest + "\"";
                }
                //string[] fdfdsfinalPacket = "[" + source + "," + tempDest + ",\"" + purpose + "\"," + ObjectToString(packet) + "]";
                ImmutableArray<string> finalPacket = ImmutableArray.Create(source, tempDest, purpose, Network.ObjectToString(packet));
                //terminal.broadcast(source, finalPacket);
                return finalPacket;
            }

            //-----------------------------------------------------------------------------
        }
    }
}
