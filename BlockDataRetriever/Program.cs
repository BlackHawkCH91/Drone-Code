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
        static Dictionary<string, Tuple<string, Dictionary<string, double>>> blueprintCrafting = new Dictionary<string, Tuple<string, Dictionary<string, double>>>();
        static Dictionary<string, Dictionary<string, double>> cubeBlocks = new Dictionary<string, Dictionary<string, double>>();

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
                string originalName = "MyObjectBuilder_BlueprintDefinition/" + blueprint.SelectNodes("descendant::Id")[0].SelectNodes("descendant::SubtypeId")[0].InnerText;

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

                blueprintCrafting.Add(blueprintName, Tuple.Create(originalName, components));
            }
        }

        public static void GetBlockComponents()
        {
            string[] cubeBlockPaths = Directory.GetFiles(@"D:\Steam\steamapps\common\SpaceEngineers\Content\Data\CubeBlocks\");
            bool skipFirst = true;
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            foreach (string path in cubeBlockPaths)
            {
                if (skipFirst)
                {
                    skipFirst = false;
                    continue;
                }

                string xmlContent = File.ReadAllText(path);
                //xmlContent.Replace("xsi:type", "type");
                //Console.WriteLine(xmlContent);

                XmlDocument cbReader = new XmlDocument();
                //cbReader.LoadXml(xmlContent);
                cbReader.Load(path);

                //Get blueprints
                XmlNode root = cbReader.DocumentElement;
                XmlNodeList XCubeBlocks = root.SelectNodes("descendant::Definition");

                foreach (XmlNode cubeBlock in XCubeBlocks)
                {
                    Dictionary<string, double> componentDict = new Dictionary<string, double>();

                    string cubeBlockName;

                    if (cubeBlock.Attributes.Count == 0)
                    {
                        cubeBlockName = "MyObjectBuilder_CubeBlock/" + cubeBlock.SelectNodes("descendant::SubtypeId")[0].InnerText;
                    } else
                    {
                        //Console.WriteLine(xmlContent);
                        string definitionAttribute = cubeBlock.Attributes[0].InnerText;
                        definitionAttribute = definitionAttribute.Replace("Definition", "");
                        cubeBlockName = definitionAttribute + "/" + cubeBlock.SelectNodes("descendant::SubtypeId")[0].InnerText;
                    }


                    if (String.IsNullOrEmpty(cubeBlockName))
                    {
                        continue;
                    }

                    XmlNode componentsList = cubeBlock.SelectNodes("descendant::Components")[0];
                    XmlNodeList components = componentsList.SelectNodes("descendant::Component");

                    foreach (XmlNode component in components)
                    {
                        string componentName = component.Attributes.GetNamedItem("Subtype").InnerText;
                        double itemAmount = double.Parse(component.Attributes.GetNamedItem("Count").InnerText);

                        if (componentDict.ContainsKey(componentName))
                        {
                            componentDict[componentName] += itemAmount;
                            continue;
                        }
                        componentDict[componentName] = itemAmount;
                    }

                    try
                    {
                        cubeBlocks.Add(cubeBlockName, componentDict);
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine("Duplicate: " + cubeBlockName);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            //"D:\SE-PlanetMapping\Earthlike"
            GetComponentNames();
            GetBlueprints();

            using (StreamWriter sw = File.CreateText(@"D:\SE-PlanetMapping\Earthlike\components.txt"))
            {
                foreach (KeyValuePair<string, Tuple<string, Dictionary<string, double>>> component in blueprintCrafting)
                {
                    string writeLine = component.Key + " " + component.Value.Item1 + " ";
                    foreach (KeyValuePair<string, double> amount in component.Value.Item2)
                    {
                        writeLine += $"{amount.Key} {amount.Value} ";
                    }
                    sw.WriteLine(writeLine);
                }
            }

            GetBlockComponents();

            using (StreamWriter sw = File.CreateText(@"D:\SE-PlanetMapping\Earthlike\cubeblocks.txt"))
            {
                foreach (KeyValuePair<string, Dictionary<string, double>> cubeBlock in cubeBlocks)
                {
                    string writeLine = cubeBlock.Key + " ";
                    foreach (KeyValuePair<string, double> amount in cubeBlock.Value)
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
