using System;
using System.Numerics;

namespace ConsoleTest
{
    internal enum Faces : byte
    {
        XPositive,
        XNegative,
        YPositive,
        YNegative,
        ZPositive,
        ZNegative,
    }

    class Program
    {
        public static void CalculateSamplePosition(ref Vector3 localPos, out Vector3 samplePosition, ref Vector2 texCoord, int resolution)
        {
            Vector3 abs = Vector3.Abs(localPos);

            if (abs.X > abs.Y)
            {
                if (abs.X > abs.Z)
                {
                    localPos /= abs.X;
                    texCoord.Y = -localPos.Y;

                    if (localPos.X > 0.0f)
                    {
                        texCoord.X = -localPos.Z;
                        samplePosition.X = (int)Faces.XPositive;
                    }
                    else
                    {
                        texCoord.X = localPos.Z;
                        samplePosition.X = (int)Faces.XNegative;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texCoord.Y = -localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texCoord.X = localPos.X;
                        samplePosition.X = (int)Faces.ZPositive;
                    }
                    else
                    {
                        texCoord.X = -localPos.X;
                        samplePosition.X = (int)Faces.ZNegative;
                    }
                }
            }
            else
            {
                if (abs.Y > abs.Z)
                {
                    localPos /= abs.Y;
                    texCoord.Y = -localPos.Z;
                    if (localPos.Y > 0.0f)
                    {
                        texCoord.X = -localPos.X;
                        samplePosition.X = (int)Faces.YPositive;
                    }
                    else
                    {
                        texCoord.X = localPos.X;
                        samplePosition.X = (int)Faces.YNegative;
                    }
                }
                else
                {
                    localPos /= abs.Z;
                    texCoord.Y = -localPos.Y;
                    if (localPos.Z > 0.0f)
                    {
                        texCoord.X = localPos.X;
                        samplePosition.X = (int)Faces.ZPositive;
                    }
                    else
                    {
                        texCoord.X = -localPos.X;
                        samplePosition.X = (int)Faces.ZNegative;
                    }
                }
            }

            texCoord = ((texCoord + new Vector2(1, 1)) * .5f * resolution);

            samplePosition.Y = (int)Math.Round(texCoord.X);
            samplePosition.Z = (int)Math.Round(texCoord.Y);
        }

        static void Main(string[] args)
        {
            Vector3 localPos = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 samplePos = Vector3.Zero;
            Vector2 texCoord = new Vector2(1452, 392);

            //CalculateSamplePosition(ref localPos, out samplePos, ref texCoord, 2048);
            CalculateSamplePosition(ref localPos, out samplePos, ref texCoord, 2048);
            Console.WriteLine(samplePos);
            //Console.WriteLine("Hello World!");
        }
    }
}
