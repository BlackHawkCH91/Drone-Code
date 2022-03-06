using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BlockDataRetriever
{
    class Program
    {
        static List<string> componentNames = new List<string>();
        static Dictionary<string, Dictionary<string, double>> blueprintCrafting = new Dictionary<string, Dictionary<string, double>>();

        public static void GetComponentNames()
        {
            //Filepath and init
            string componentsPath = @"D:\Steam\steamapps\common\SpaceEngineers\Content\Data\Components.sbc";
            XmlDocument compReader = new XmlDocument();
            compReader.Load(componentsPath);

            XmlNode compRoot = compReader.DocumentElement;
            XmlNodeList compIds = compRoot.SelectNodes("descendant::Id");

            foreach (XmlNode compName in compIds)
            {
                componentNames.Add(compName.SelectNodes("SubtypeId")[0].InnerText);
            }
        }

        public static void GetBlueprints()
        {
            //Filepath and init
            string blueprintsPath = @"D:\Steam\steamapps\common\SpaceEngineers\Content\Data\Blueprints.sbc";
            XmlDocument bpReader = new XmlDocument();
            bpReader.Load(blueprintsPath);

            //Get blueprints
            XmlNode root = bpReader.DocumentElement;
            XmlNodeList blueprints = root.SelectNodes("descendant::Blueprint");

            //Loop through blueprints
            foreach (XmlNode blueprint in blueprints)
            {
                Dictionary<string, double> components = new Dictionary<string, double>();
                string blueprintName = blueprint.SelectNodes("descendant::Id")[0].SelectNodes("descendant::SubtypeId")[0].InnerText;

                foreach (string name in componentNames)
                {
                    if (blueprintName.Contains(name))
                    {
                        blueprintName = name;
                        break;
                    }
                }

                //Items needed to make component
                XmlNode prerequisites = blueprint.SelectNodes("descendant::Prerequisites")[0];
                XmlNodeList items = prerequisites.SelectNodes("descendant::Item");

                foreach (XmlNode item in items)
                {
                    string itemName = item.Attributes.GetNamedItem("SubtypeId").InnerText;
                    double itemAmount = double.Parse(item.Attributes.GetNamedItem("Amount").InnerText);

                    if (components.ContainsKey(itemName))
                    {
                        components[itemName] += itemAmount;
                        continue;
                    }
                    components[itemName] = itemAmount;
                }

                blueprintCrafting.Add(blueprintName, components);
            }
        }

        static void Main(string[] args)
        {
            //"D:\SE-PlanetMapping\Earthlike"
            GetComponentNames();
            GetBlueprints();

            using (StreamWriter sw = File.CreateText(@"D:\SE-PlanetMapping\Earthlike\components.txt"))
            {
                foreach (KeyValuePair<string, Dictionary<string, double>> component in blueprintCrafting)
                {
                    string writeLine = component.Key + " ";
                    foreach (KeyValuePair<string, double> amount in component.Value)
                    {
                        writeLine += $"{amount.Key} {amount.Value} ";
                    }
                    sw.WriteLine(writeLine);
                }
            }



            Console.WriteLine("Done");
        }
    }
}
