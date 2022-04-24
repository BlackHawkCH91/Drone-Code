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
        }
    }
}
