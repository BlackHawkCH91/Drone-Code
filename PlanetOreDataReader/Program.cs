using System;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PlanetOreDataReader
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "JUST SHUT UP WILL ONLY BE RUN ON WINDOWS")]
    class Program
    {

        
        static Bitmap bmp = new Bitmap("testmap.bmp");
        static StreamWriter sw = File.CreateText("result.txt");

        static byte[,] pixelData = new byte[bmp.Width, bmp.Height];
        static StringBuilder stringBuilder = new StringBuilder();

        static void Main(string[] args)
        {

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            var depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8; //bytes per pixel

            var buffer = new byte[data.Width * data.Height * depth];

            //copy pixels to buffer
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            Console.WriteLine("Starting");
            stringBuilder.Append($"{bmp.Height}{bmp.Width}$");

            Parallel.For(0, data.Width, x => {         
                Parallel.For(0, data.Height, y =>
                {
                    // Grabs data from blue channel (every 3rd byte)
                    pixelData[x, y] = buffer[data.Stride * y + depth * x];    
                }); 
            });

            byte? prevByte = null;
            int numSameBytes = 0;
            for(int y = 0; y < pixelData.GetUpperBound(1); y++)
            {
                for (int x = 0; x < pixelData.GetUpperBound(0); x++)
                {
                    if (pixelData[x,y] == prevByte)
                    {
                        numSameBytes++;
                    }
                    else if (prevByte != null)
                    {
                        stringBuilder.Append($",{numSameBytes},{prevByte.ToString().PadLeft(3, '0')}");
                        prevByte = pixelData[x, y];
                        numSameBytes = 1;
                    }
                    else
                    {
                        prevByte = pixelData[x, y];
                        numSameBytes = 1;
                    }
                }
            }

            bmp.UnlockBits(data);
            sw.Write(stringBuilder.ToString());
            Console.WriteLine("Complete, result path: ");
            Console.WriteLine(Directory.GetCurrentDirectory() + @"\result.txt");
        }


    }
}
