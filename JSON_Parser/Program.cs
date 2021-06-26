using System;
using System.Collections.Generic;


namespace JSON_Parser
{
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

    public class drone
    {
        public string droneType;
        public Vector3 position;
        public float health;
        public float energy;
        public float hydrogen;
        public string status;
        public string currentJob;
        public string currentAction;
        public object[] test;
        public object[] test2;

        public drone(string fDroneType, Vector3 fPosition, float fHealth, float fEnergy, float fHydrogen, string fStatus, string fCurrentJob, string fCurrentAction, object[] fTest, object[] fTest2)
        {
            droneType = fDroneType;
            position = fPosition;
            health = fHealth;
            energy = fEnergy;
            hydrogen = fHydrogen;
            status = fStatus;
            currentJob = fCurrentJob;
            currentAction = fCurrentAction;
            test = fTest;
            test2 = fTest2;
        }
    }

    class Program
    {
        public static Vector3 StringToVector3(string sVector)
        {
            //Remove curly brackets
            if (sVector.StartsWith("{") && sVector.EndsWith("}"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            //Split the string where there is whitespace (commas are not used for some reason)
            string[] sArray = sVector.Split(' ');

            //Parse the values into floats and create a Vector3
            Vector3 position = new Vector3(
                float.Parse(sArray[0].Substring(2, sArray[0].Length - 2)),
                float.Parse(sArray[1].Substring(2, sArray[1].Length - 2)),
                float.Parse(sArray[2].Substring(2, sArray[2].Length - 2))
            );

            return position;
        }

        //Converts drone class to object array
        public static object[] DroneToObj(drone DC)
        {
            return new object[] { DC.droneType, DC.position, DC.health, DC.energy, DC.hydrogen, DC.status, DC.currentJob, DC.currentAction, DC.test, DC.test2 };
        }

        //Converts object array to string
        public static string ObjectToString(object[] objArr)
        {
            string finalStr = "[";

            for (int i = 0; i < objArr.Length; i++) 
            {
                if (objArr[i].GetType() == typeof(object[]))
                {
                    //If item in object array is an object array, call itself (recursive(
                    finalStr += ObjectToString(objArr[i] as object[]);
                } else if (objArr[i].GetType() == typeof(Vector3)) {
                    Vector3 temp = objArr[i] as Vector3;
                    finalStr += temp.toString();
                } else
                {
                    //Add to final string
                    if (objArr[i].GetType() == typeof(string))
                    {
                        finalStr += '"';
                    }

                    finalStr += objArr[i].ToString();

                    if (objArr[i].GetType() == typeof(string))
                    {
                        finalStr += '"';
                    }
                }

                //Add commas except to the last item
                if ((i + 1) < objArr.Length)
                {
                    finalStr += ",";
                }
            }
            finalStr += "]";
            return finalStr;
        }

        //Convert dictionary to string (may be replaced to only work on object arrays to make it more
        //universal. If done, dictionaries will need to be converted to an object array).
        public static string DictToString(Dictionary<long, object[]> dictionary)
        {
            string outputString = "";

            int counter = 1;
            foreach (KeyValuePair<long, object[]> dict in dictionary)
            {
                //Start off with bracket and adding the key
                outputString += "[" + dict.Key.ToString() + ",";

                //Key value is an array, loop through it
                for (int i = 0; i < dict.Value.Length; i++)
                {
                    //If value at index is an array, do recursion
                    if (dict.Value[i].GetType() == typeof(object[]))
                    {
                        outputString += ObjectToString(dict.Value[i] as object[]);
                    } else if (dict.Value[i].GetType() == typeof(Vector3))
                    {
                        Vector3 temp = dict.Value[i] as Vector3;
                        outputString += temp.toString();
                    }
                    else
                    {
                        //Add value to end of string
                        if (dict.Value[i].GetType() == typeof(string))
                        {
                            outputString += '"';
                        }

                        outputString += dict.Value[i].ToString();

                        if (dict.Value[i].GetType() == typeof(string))
                        {
                            outputString += '"';
                        }
                    }

                    //If item is not the last item, add a comma
                    if ((i + 1) < dict.Value.Length)
                    {
                        outputString += ",";
                    }
                }
                outputString += "]";

                //If the object is not the last item, add this to make separating objects easier
                if (counter < dictionary.Count)
                {
                    outputString += "|";
                }

                counter++;
            }
            return outputString;
        }

        //Gets the positions of square brackets in string
        public static Dictionary<string, List<int>> getBracketPos(string str)
        {
            //string trimmedStr = str.Remove(0, 1);
            //trimmedStr = trimmedStr.Remove(trimmedStr.Length - 1, 1);
            //Define dictionary to store positions of brackets in strings
            Dictionary<string, List<int>> bracketDict = new Dictionary<string, List<int>>();
            bracketDict.Add("[", new List<int>());
            bracketDict.Add("]", new List<int>());

            //Loop through string
            for (int i = 0; i < str.Length; i++)
            {
                //Check if it is a [ or a ] and add its position to 
                //respective dictionary
                if (str[i] == '[')
                {
                    bracketDict["["].Add(i);
                }
                if (str[i] == ']')
                {
                    bracketDict["]"].Add(i);
                }
            }

            return bracketDict;
        }

        static object[] StringToObject(string strObject)
        {
            //Console.WriteLine("1: " + strObject);

            //Dict to store linked brackets, test to get bracket positions
            Dictionary<int, int> linked = new Dictionary<int, int>();
            string trimmedObj = strObject.Remove(0, 1);
            trimmedObj = trimmedObj.Remove(trimmedObj.Length - 1, 1);

            //Console.WriteLine("Trimmed:" + trimmedObj);

            Dictionary<string, List<int>> test = getBracketPos(trimmedObj);

            object[] objArr;


            //This finds the bracket links.
            for (int i = test["["].Count - 1; i >= 0; i--)
            {
                int difference = 0;
                foreach (int bracketPos in test["]"])
                {
                    difference = bracketPos - test["["][i];
                    if (difference > 0 && !(linked.ContainsValue(bracketPos)))
                    {
                        linked.Add(test["["][i], bracketPos);
                        break;
                    }
                }
            }

            //Loop through linked list to find children and parents.


            bool isChild = false;
            string r = trimmedObj;
            List<object[]> childObj = new List<object[]>();

            //Loop through all brackets
            foreach (KeyValuePair<int, int> child in linked)
            {
                //Console.WriteLine("Thing: " + child.Key);
                //Check if bracket is a "child" to the other
                foreach (KeyValuePair<int, int> parent in linked)
                {
                    //Console.WriteLine(child.Key + " > " + parent.Key + " | " + child.Key + " < " + parent.Value);

                    if (child.Key > parent.Key && child.Key < parent.Value)
                    {
                        isChild = true;
                    }

                    //Console.WriteLine(child.Key + " > " + parent.Key + " | " + (child.Key > parent.Key));
                }

                //If not, recursive and remove any other brackets
                if (!(isChild))
                {
                    //Console.WriteLine("Sub: " + r.Substring(child.Key, (child.Value + 1) - child.Key));
                    childObj.Add(StringToObject(r.Substring(child.Key, (child.Value + 1) - child.Key)));
                    r = r.Remove(child.Key, (child.Value + 1) - child.Key);
                }

                isChild = false;
            }

            //Console.WriteLine("Child: " + childObj[childObj.Count - 1]);

            //Once brackets are removed, split string into array
            string[] array = r.Split(',');
            objArr = new object[array.Length];

            //Determine which objects are strings/ints/doubles
            int counter = 0;
            int objectCounter = childObj.Count - 1;
            foreach (string item in array)
            {
                //Console.WriteLine("For: " + item);
                if (item.Length != 0)
                {
                    if (item[0] == '"')
                    {
                        string temp = item;
                        temp = temp.Remove(0, 1);
                        temp = temp.Remove(temp.Length - 1, 1);
                        objArr[counter] = temp;
                    }
                    else if (item[0] == '{')
                    {
                        objArr[counter] = StringToVector3(item);
                    }
                    else if (item.Contains('.'))
                    {
                        double temp = Convert.ToDouble(item);
                        objArr[counter] = temp;
                    }
                    else
                    {
                        int temp = Convert.ToInt32(item);
                        objArr[counter] = temp;
                    }
                } else {
                    objArr[counter] = childObj[objectCounter];
                    objectCounter--;
                }
                
                counter++;
            }

            //Add object arrays back in
            int arrCounter = 0;
            for (int i = objArr.Length - 1; i >= 0; i--)
            {
                if (objArr[i] == null)
                {
                    objArr[i] = childObj[arrCounter];
                    arrCounter++;
                }
            }

            return objArr;
        }

        static string displayThing(object[] array)
        {
            string output = "";
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].GetType() == typeof(object[])) {
                    output += "[";
                    output += displayThing(array[i] as object[]);
                    output += "]";
                } else if (array[i].GetType() == typeof(Vector3))
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
            //Dictionary to store drones and dictionary to store linked brackets
            Dictionary<long, object[]> testDict = new Dictionary<long, object[]>();
            

            //Add drone
            testDict.Add(1, DroneToObj(new drone("miner", new Vector3(2, 3, 4), 1, 0.8f, 0.6f, "Safe", "Cobalt", "Travelling-Cobalt", new object[] { new object[] { "obj2", 34 }, 1, 4, "ree" }, new object[] { "ih8mylife", 432 })));
            testDict.Add(3, DroneToObj(new drone("miner", new Vector3(4, 2, 7), 1, 0.4f, 0.7f, "Safe", "Iron", "Travelling-Outpost1", new object[] { new object[] { "oded", 344 }, 16, 3, "tte" }, new object[] { "Thing", 234 })));

            //Convert to string
            string strDict = DictToString(testDict);
            Console.WriteLine("String: ");
            Console.WriteLine(strDict);

            string[] strDictArr = strDict.Split("|");

            //Gets the positions of brackets and "links" them, but may not work in terms of
            //extracting the data

            string testString = strDictArr[0];
            testString = testString.Remove(0, 1);
            testString = testString.Remove(testString.Length - 1, 1);

            List<object[]> decoded = new List<object[]>();
            foreach (string drone in strDictArr)
            {
                decoded.Add(StringToObject(drone));
            }

            Console.WriteLine("\nObject array contents: ");


            foreach (object[] item in decoded)
            {
                Console.WriteLine(displayThing(item));
                /*foreach (object obj in item as object[])
                {
                    Console.WriteLine(displayThing(obj));
                }*/
            }
        }
    }
}