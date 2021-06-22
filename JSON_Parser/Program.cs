using System;
using System.Collections.Generic;
using System.Numerics;


namespace JSON_Parser
{
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

        public drone(string fDroneType, Vector3 fPosition, float fHealth, float fEnergy, float fHydrogen, string fStatus, string fCurrentJob, string fCurrentAction, object[] fTest)
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
        }
    }

    class Program
    {
        public static object[] DroneToObj(drone DC)
        {
            return new object[] { DC.droneType, DC.position, DC.health, DC.energy, DC.hydrogen, DC.status, DC.currentJob, DC.currentAction, DC.test };
        }

        public static string ObjectToString(object[] objArr)
        {
            string finalStr = "[";

            for (int i = 0; i < objArr.Length; i++) 
            {
                if (objArr[i].GetType() == typeof(object[]))
                {
                    finalStr += ObjectToString(objArr[i] as object[]);
                } else
                {
                    finalStr += objArr[i].ToString();

                    if ((i + 1) < objArr.Length) 
                    {
                        finalStr += ",";
                    }
                }
            }
            finalStr += "],";
            return finalStr;
        }

        public static string DictToString(Dictionary<long, object[]> dictionary)
        {
            string outputString = "";

            foreach (KeyValuePair<long, object[]> dict in dictionary)
            {
                outputString += "[" + dict.Key.ToString() + ",";

                foreach (var arr in dict.Value)
                {
                    if (arr.GetType() == typeof(object[]))
                    {
                        outputString += ObjectToString(arr as object[]);
                    }
                    else
                    {
                        outputString += arr.ToString() + ",";
                    }
                }
            }

            return outputString;
        }

        static void Main(string[] args)
        {
            Dictionary<long, object[]> testDict = new Dictionary<long, object[]>();

            testDict.Add(1, DroneToObj(new drone("miner", new Vector3(2, 3, 4), 1, 0.8f, 0.6f, "Safe", "Cobalt", "Travelling | Cobalt", new object[] { new object[] { "obj2", 34 }, 1, 4, "ree" })));

            //DroneToObj(new drone("miner", new Vector3(2, 3, 4), 1, 0.8f, 0.6f, "Safe", "Cobalt", "Travelling | Cobalt", new object[] { new object[] { "obj2", 34 }, 1, 4, "ree" }));

            Console.WriteLine(DictToString(testDict));
        }
    }
}
