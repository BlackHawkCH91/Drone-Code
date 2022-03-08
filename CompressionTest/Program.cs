using System;
using System.IO;
using System.Collections.Generic;

namespace CompressionTest
{
    class Program
    {
        static Dictionary<string, string> colourKeys = new Dictionary<string, string>();
        static List<string> availableChars = new List<string>(new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "g", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "-", "_", "=", "+", "[", "{", "]", "}", "|", ";", ":", "'", ",", "<", "." });


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
                        if ((int.Parse(texts[i]) >= 10 && int.Parse(texts[i]) <= 255) || int.Parse(texts[i]) >= 1000)
                        {
                            sw.Write(int.Parse(texts[i]).ToString("X"));
                        }
                        else
                        {
                            sw.Write(texts[i]);
                        }
                    }


                    //sw.Write(int.Parse(texts[i]).ToString("X"));

                    if (!colourKeys.ContainsKey(texts[i + 1]))
                    {
                        Console.WriteLine(texts[i + 1]);
                        colourKeys.Add(texts[i + 1], availableChars[character]);
                        character++;
                    }
                    sw.Write(colourKeys[texts[i + 1]]);
                }
            }
        }

        static void uncompressFile()
        {
            string result2 = File.ReadAllText(@"D:\SE-PlanetMapping\result2.txt");

            string start = result2.Substring(0, 9);
            string contents = result2.Substring(9, result2.Length - 9);

            using (StreamWriter sw = File.CreateText(@"D:\SE-PlanetMapping\result3.txt"))
            {

            }
        }

        static void Main(string[] args)
        {
            compressFile();


            //string result2 = File.ReadAllText(@"D:\SE-PlanetMapping\result2.txt");
            //Console.WriteLine($"{result.Length} characters to {result2.Length} characters. {result.Length - result2.Length} difference.");
        }
    }
}
