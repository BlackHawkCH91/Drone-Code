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
    partial class Program : MyGridProgram
    {

        //Define variables

        bool setup = false;

        //Variables for cargo containers

        //              ---- NOTICE ---- 
        // Format: Name, {Item AMOUNT, Item MINIMUM AMOUNT}


        //Ore counts:
        // Longest 11

        Dictionary<string, int[]> ores = new Dictionary<string, int[]>() {
        { "Stone", new int[] {0, 0} },
        { "Scrap Metal", new int[] {0, 0} },
        { "Iron", new int[] {0, 0} },
        { "Nickel", new int[] {0, 0} },
        { "Silicon", new int[] {0, 0} },
        { "Cobalt", new int[] {0, 0} },
        { "Magnesium", new int[] {0, 0} },
        { "Silver", new int[] {0, 0} },
        { "Gold", new int[] {0, 0} },
        { "Uranium", new int[] {0, 0} },
        { "Platinum", new int[] {0, 0} },
        { "Ice", new int[] {0, 0} }
        };


        //Refined material counts:
        // Longest 9

        Dictionary<string, int[]> ingots = new Dictionary<string, int[]>() {
        { "Stone", new int[] {0, 0} },
        { "Iron", new int[] {0, 0} },
        { "Nickel", new int[] {0, 0} },
        { "Silicon", new int[] {0, 0} },
        { "Cobalt", new int[] {0, 0} },
        { "Magnesium", new int[] {0, 0} },
        { "Silver", new int[] {0, 0} },
        { "Gold", new int[] {0, 0} },
        { "Uranium", new int[] {0, 0} },
        { "Platinum", new int[] {0, 0} }
        };



        //Component counts:
        // Longest 18

        Dictionary<string, int[]> components = new Dictionary<string, int[]>() {
        { "BulletproofGlass", new int[] {0, 0} },
        { "Canvas", new int[] {0, 0} },
        { "Computer", new int[] {0, 0} },
        { "Construction", new int[] {0, 0} },
        { "Detector", new int[] {0, 0} },
        { "Display", new int[] {0, 0} },
        { "Explosives", new int[] {0, 0} },
        { "Girder", new int[] {0, 0} },
        { "GravityGenerator", new int[] {0, 0} },
        { "InteriorPlate", new int[] {0, 0} },
        { "LargeTube", new int[] {0, 0} },
        { "Medical", new int[] {0, 0} },
        { "MetalGrid", new int[] {0, 0} },
        { "Motor", new int[] {0, 0} },
        { "PowerCell", new int[] {0, 0} },
        { "RadioCommunication", new int[] {0, 0} },
        { "Reactor", new int[] {0, 0} },
        { "SmallTube", new int[] {0, 0} },
        { "SolarCell", new int[] {0, 0} },
        { "SteelPlate", new int[] {0, 0} },
        { "Superconductor", new int[] {0, 0} },
        { "Thrust", new int[] {0, 0} },
        { "ZoneChip", new int[] {0, 0} }
        };


        //Ammo
        //Longest 14

        //Currently not working. Works fine with missiles, however, bullets do not. Names may be wrong.
        Dictionary<string, int[]> ammo = new Dictionary<string, int[]>()
        {
            { "Missile200mm", new int[] {0, 0} },
            { "NATO 25x184mm", new int[] {0, 0} },
            { "NATO 5p56x45mm", new int[] {0, 0} }
        };

        List<IMyTerminalBlock> cargo = new List<IMyTerminalBlock>();

        //Returns count for all items
        public IEnumerator<int> displayInventory()
        {
            //Get all blocks that can hold items

            while (true)
            {
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargo);

                //Loop through list
                for (int i = 0; i < cargo.Count; i++)
                {
                    if (cargo[i].HasInventory)
                    {
                        //Loop through amount of different items
                        for (int a = 0; a < cargo[i].GetInventory(0).ItemCount; a++)
                        {
                            //Check if the item is an ore, etc...
                            if (cargo[i].GetInventory(0).GetItemAt(a).Value.ToString().Contains("Ore"))
                            {
                                //Add count of item to its respective key in dictionary
                                ores[cargo[i].GetInventory(0).GetItemAt(a).Value.Type.SubtypeId.ToString()][0] += cargo[i].GetInventory(0).GetItemAt(a).Value.Amount.ToIntSafe();
                            }
                            else if (cargo[i].GetInventory(0).GetItemAt(a).Value.ToString().Contains("Ingot"))
                            {
                                ingots[cargo[i].GetInventory(0).GetItemAt(a).Value.Type.SubtypeId.ToString()][0] += cargo[i].GetInventory(0).GetItemAt(a).Value.Amount.ToIntSafe();
                            }
                            else if (cargo[i].GetInventory(0).GetItemAt(a).Value.ToString().Contains("Component"))
                            {
                                components[cargo[i].GetInventory(0).GetItemAt(a).Value.Type.SubtypeId.ToString()][0] += cargo[i].GetInventory(0).GetItemAt(a).Value.Amount.ToIntSafe();
                            }
                            else if (cargo[i].GetInventory(0).GetItemAt(a).Value.ToString().Contains("Ammo"))
                            {
                                ammo[cargo[i].GetInventory(0).GetItemAt(a).Value.Type.SubtypeId.ToString()][0] += cargo[i].GetInventory(0).GetItemAt(a).Value.Amount.ToIntSafe();
                            }
                            yield return 0;
                        }
                    }
                    //Echo(cargo[i].ToString());
                }
                yield return 0;

                string oreDisplayString = "";
                string ingotDisplayString = "";
                string compDisplayString = "";
                string ammoDisplayString = "";

                foreach (KeyValuePair<string, int[]> itemDisplay in ores)
                {
                    oreDisplayString += itemDisplay.Key + " " + itemDisplay.Value[0].ToString() + " " + itemDisplay.Value[1].ToString() + "\n";
                    ores[itemDisplay.Key][0] = 0;
                }
                foreach (KeyValuePair<string, int[]> itemDisplay in ingots)
                {
                    ingotDisplayString += itemDisplay.Key + " " + itemDisplay.Value[0].ToString() + " " + itemDisplay.Value[1].ToString() + "\n";
                    ingots[itemDisplay.Key][0] = 0;
                }
                foreach (KeyValuePair<string, int[]> itemDisplay in components)
                {
                    compDisplayString += itemDisplay.Key + " " + itemDisplay.Value[0].ToString() + " " + itemDisplay.Value[1].ToString() + "\n";
                    components[itemDisplay.Key][0] = 0;
                }
                foreach (KeyValuePair<string, int[]> itemDisplay in ammo)
                {
                    ammoDisplayString += itemDisplay.Key + " " + itemDisplay.Value[0].ToString() + " " + itemDisplay.Value[1].ToString() + "\n";
                    ammo[itemDisplay.Key][0] = 0;
                }

                displayPanels[0].WriteText(oreDisplayString);
                displayPanels[1].WriteText(ingotDisplayString);
                displayPanels[2].WriteText(compDisplayString);
                displayPanels[3].WriteText(ammoDisplayString);

            }
        }


        List<IMyTextPanel> displayPanels = new List<IMyTextPanel>();
        //Initialise things in script
        public void init()
        {
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargo);
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(displayPanels);

            foreach (IMyTextPanel panel in displayPanels)
            {
                panel.Font = "Monospace";
                panel.FontSize = 1.3f;
            }
        }


        //Set runtime frequency
        public Program()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Runtime.EstablishCoroutines();
            Coroutine.AddCoroutine(displayInventory);
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            Coroutine.StepCoroutines(updateSource);


            //Initialise
            if (!(setup))
            {
                init();
                setup = true;
            }
        }
    }
}
