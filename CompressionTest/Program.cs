using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompressionTest
{
    class Program
    {
        static Dictionary<string, string> colourKeys = new Dictionary<string, string>();
        static List<string> availableChars = new List<string>(new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "-", "_", "=", "+", "[", "{", "]", "}", "|", ";", ":", "'", ",", "<", "." });


        static void compressFile()
        {
            int character = 0;

            string result = File.ReadAllText(@"D:\SE-PlanetMapping\result.txt");
            string[] texts = result.Split(',');

            using (StreamWriter sw = File.CreateText(@"D:\SE-PlanetMapping\result2.txt"))
            {
                sw.Write(texts[0]);

                for (int i = 1; i < texts.Length; i += 2)
                {
                    if (texts[i] != "1")
                    {
                        if (int.Parse(texts[i]) >= 10 && int.Parse(texts[i]) <= 255)
                        {
                            sw.Write(int.Parse(texts[i]).ToString("X"));
                        } 
                        else if (int.Parse(texts[i]) >= 1000)
                        {
                            sw.Write(int.Parse(texts[i]).ToString("X4"));
                        } 
                        else
                        {
                            sw.Write(texts[i]);
                        }
                    }


                    //sw.Write(int.Parse(texts[i]).ToString("X"));

                    if (!colourKeys.ContainsKey(texts[i + 1]))
                    {
                        Console.WriteLine(texts[i + 1] + " | " + availableChars[character]);
                        colourKeys.Add(texts[i + 1], availableChars[character]);
                        character++;
                    }
                    sw.Write(colourKeys[texts[i + 1]]);
                }
            }
            //inverseColourKeys = new Dictionary<string, string>(colourKeys.Values, colourKeys.Keys);
        }

        static void compressFile2()
        {
            StringBuilder final = new StringBuilder();
            int maxAttempt = 75;
            string result = File.ReadAllText(@"D:\SE-PlanetMapping\result2.txt");
            string repeatSection = "";
            int compressCount = 1;

            using (StreamWriter sw = File.CreateText(@"D:\SE-PlanetMapping\compressTest.txt"))
            {
                for (int i = 0; i < result.Length; i += repeatSection.Length * compressCount)
                {
                    repeatSection = "";
                    compressCount = 1;
                    for (int attempt = 0; attempt < maxAttempt && compressCount == 1 && i + attempt < result.Length; attempt++)
                    {
                        repeatSection += result[i + attempt];
                        //Console.WriteLine(repeatSection);

                        while (true)
                        {
                            try
                            {
                                if (repeatSection == result.Substring(i + repeatSection.Length * compressCount, repeatSection.Length))
                                {
                                    compressCount++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }

                    if (compressCount > 1 && (compressCount.ToString().Length + 2 + repeatSection.Length) < (repeatSection.Length * compressCount))
                    {
                        final.Append("?" + compressCount + "|" + repeatSection + "|");
                    } else
                    {
                        repeatSection = String.Concat(Enumerable.Repeat(result[i].ToString(), compressCount));
                        compressCount = 1;
                        final.Append(repeatSection);
                    }

                    //final.Append(repeatSection);
                }
                sw.Write(final.ToString());
            }
        }

        static void uncompressFile2()
        {
            string[] contents = File.ReadAllText(@"D:\SE-PlanetMapping\compressTest.txt").Split('?');
            string final = "";

            using (StreamWriter sw = File.CreateText(@"D:\SE-PlanetMapping\uncompressTest.txt"))
            {
                foreach (string items in contents)
                {
                    if (String.IsNullOrEmpty(items))
                    {
                        continue;
                    }

                    string[] item = items.Split('|');

                    final += String.Concat(Enumerable.Repeat(item[1].ToString(), int.Parse(item[0])));

                    if (item.Length > 1)
                    {
                        for (int i = 2; i < item.Length; i++)
                        {
                            final += item[i];
                        }
                    }
                }

                final = Regex.Replace(final, ".{30}", "$0\n");

                sw.Write(final);
            }
        }

        static void uncompressFile()
        {
            string result2 = File.ReadAllText(@"D:\SE-PlanetMapping\result2.txt");

            string start = result2.Substring(0, 8) + "$";
            string contents = result2.Substring(9, result2.Length - 9);

            using (StreamWriter sw = File.CreateText(@"D:\SE-PlanetMapping\result3.txt"))
            {
                sw.Write(start + ",");
                string number = "";
                foreach (char character in contents)
                {
                    if (!availableChars.Contains(character.ToString()))
                    {
                        number += character;
                    } else
                    {
                        int test = 1;
                        if (number.Length > 0)
                        {
                            test = number.Length % 2 == 0 ? int.Parse(number, System.Globalization.NumberStyles.HexNumber) : int.Parse(number);
                        }
                        sw.Write($"{test},{colourKeys.FirstOrDefault(x => x.Value == character.ToString()).Key},");

                        number = "";
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            compressFile();
            compressFile2();
            uncompressFile();
            uncompressFile2();

            using (StreamWriter sw = File.CreateText(@"D:\SE-PlanetMapping\comparison.txt"))
            {
                string thing = File.ReadAllText(@"D:\SE-PlanetMapping\result2.txt");
                thing = Regex.Replace(thing, ".{30}", "$0\n");

                sw.Write(thing);
            }

            //string result2 = File.ReadAllText(@"D:\SE-PlanetMapping\result2.txt");
            //Console.WriteLine($"{result.Length} characters to {result2.Length} characters. {result.Length - result2.Length} difference.");
        }
    }
}
