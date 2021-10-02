using System;
using System.Collections.Generic;


namespace JSON_Parser
{
    //Fake Vector3 class to mirror SE's vector3 system
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

            //Outputs vector3 in the same format SE does
            return "{X:" + x + " Y:" + y + " Z:" + z + "}";
        }
    }

    //Current class for the drone. More information may need to be added. Currently a template.
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
                    //If item in object array is an object array, call itself (recursive)
                    finalStr += ObjectToString(objArr[i] as object[]);

                } else if (objArr[i].GetType() == typeof(Vector3)) {
                    
                    //If it is a vector, convert to string, but don't add double quotes
                    Vector3 temp = objArr[i] as Vector3;
                    finalStr += temp.toString();
                } else
                {
                    //Add double quotes to indicate that field is a string
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
                    outputString += "\n";
                }

                counter++;
            }
            return outputString;
        }

        //Gets the positions of square brackets in string
        public static Dictionary<string, List<int>> getBracketPos(string str)
        {
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

            //Dict to store linked brackets, test to get bracket positions
            Dictionary<int, int> linked = new Dictionary<int, int>();
            string trimmedObj = strObject.Remove(0, 1);
            trimmedObj = trimmedObj.Remove(trimmedObj.Length - 1, 1);

            //Get all bracket positions
            Dictionary<string, List<int>> test = getBracketPos(trimmedObj);

            object[] objArr;


            //This finds the bracket links.
            //Loop through open brackets (start at the end of dictionary)
            for (int i = test["["].Count - 1; i >= 0; i--)
            {
                int difference = 0;
                //loop through closing brackets
                foreach (int bracketPos in test["]"])
                {
                    //Find position difference in string
                    difference = bracketPos - test["["][i];
                    //If the difference is not negative and dictionary does not contain link
                    if (difference > 0 && !(linked.ContainsValue(bracketPos)))
                    {
                        //Add position of opening and closing bracket.
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
                //Check if bracket is a "child" to the other
                foreach (KeyValuePair<int, int> parent in linked)
                {

                    if (child.Key > parent.Key && child.Key < parent.Value)
                    {
                        isChild = true;
                    }
                }

                //If not, recursive and remove any other brackets
                if (!(isChild))
                {
                    childObj.Add(StringToObject(r.Substring(child.Key, (child.Value + 1) - child.Key)));
                    r = r.Remove(child.Key, (child.Value + 1) - child.Key);
                }

                isChild = false;
            }

            //Once brackets are removed, split string into array
            string[] array = r.Split(',');
            objArr = new object[array.Length];

            //Determine which objects are strings/ints/doubles
            int counter = 0;
            int objectCounter = childObj.Count - 1;
            foreach (string item in array)
            {
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
            Dictionary<long, object[]> droneDict = new Dictionary<long, object[]>();
            //Dictionary to store drones and dictionary to store linked brackets

            //Add drone
            droneDict.Add(1, DroneToObj(new drone("miner", new Vector3(2, 3, 4), 1, 0.8f, 0.6f, "Safe", "Cobalt", "Travelling-Cobalt", new object[] { new object[] { "obj2", 34 }, 1, 4, "ree" }, new object[] { "ih8mylife", 432 })));
            droneDict.Add(3, DroneToObj(new drone("miner", new Vector3(4, 2, 7), 1, 0.4f, 0.7f, "Safe", "Iron", "Travelling-Outpost1", new object[] { new object[] { "oded", 344 }, 16, 3, "tte" }, new object[] { "Thing", 234 })));
            droneDict.Add(4, DroneToObj(new drone("combat", new Vector3(12, 43, 1), 1, 1f, 1f, "Safe", "", "Patrolling-Outpost1", new object[] { new object[] { "thing", 234 }, 154, 13, "test" }, new object[] { "idk", 1337 })));

            //Convert to string
            string strDict = DictToString(droneDict);
            Console.WriteLine("String: ");
            Console.WriteLine(strDict);

            string[] strDictArr = strDict.Split("\n");

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
            }


            string storage = DictToString(droneDict);

            //TODO:

            /*
             * Make a string database. This process is simple. Tough part is updating drone information efficiently.
             * Possible method of storing drones: If drones are stored in order, a simple forloop can loop through
             * x amount of \n, removing the currently stored string and then adding a new record to that row.
             * 
             * Method 2: The storage string will only be updated every time the game is saved. A simple object to string
             * will work here.
             * 
             * I doubt this code can really be improved. Once the universal encoding functions have been made, merge
             * to main and start transitioning code to SE. Make use of coroutines as this code is probably inefficient.
             * 
             * 
             * Ways to improve code:
             * 
             * Instead of having separate functions to find linked brackets and then running code to extract data from
             * said brackets, try to make it one process (if thats even possible. GL with this one)
             */
        }
    }
}