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
        

        public struct Complex : IEquatable<Complex>
        {

            // --------------SECTION: Private Data members ----------- //

            private Double m_real;
            private Double m_imaginary;


            // --------------SECTION: Public Properties -------------- //

            public Double Real
            {
                get
                {
                    return m_real;
                }
            }

            public Double Magnitude
            {
                get
                {
                    return Complex.Abs(this);
                }
            }

            public Double Phase
            {
                get
                {
                    return Math.Atan2(m_imaginary, m_real);
                }
            }

            // --------------SECTION: Attributes -------------- //

            public static readonly Complex Zero = new Complex(0.0, 0.0);
            public static readonly Complex One = new Complex(1.0, 0.0);
            public static readonly Complex ImaginaryOne = new Complex(0.0, 1.0);

            // --------------SECTION: Constructors and factory methods -------------- //

            public Complex(Double real, Double imaginary)  /* Constructor to create a complex number with rectangular co-ordinates  */
            {
                this.m_real = real;
                this.m_imaginary = imaginary;
            }

            public static Complex FromPolarCoordinates(Double magnitude, Double phase) /* Factory method to take polar inputs and create a Complex object */
            {
                return new Complex((magnitude * Math.Cos(phase)), (magnitude * Math.Sin(phase)));
            }

            // --------------SECTION: Arithmetic Operator(binary) Overloading -------------- //       
            public static Complex operator +(Complex left, Complex right)
            {
                return (new Complex((left.m_real + right.m_real), (left.m_imaginary + right.m_imaginary)));

            }

            public static Complex operator -(Complex left, Complex right)
            {
                return (new Complex((left.m_real - right.m_real), (left.m_imaginary - right.m_imaginary)));
            }

            public static Complex operator *(Complex left, Complex right)
            {
                // Multiplication:  (a + bi)(c + di) = (ac -bd) + (bc + ad)i
                Double result_Realpart = (left.m_real * right.m_real) - (left.m_imaginary * right.m_imaginary);
                Double result_Imaginarypart = (left.m_imaginary * right.m_real) + (left.m_real * right.m_imaginary);
                return (new Complex(result_Realpart, result_Imaginarypart));
            }

            public static Complex operator /(Complex left, Complex right)
            {
                // Division : Smith's formula.
                double a = left.m_real;
                double b = left.m_imaginary;
                double c = right.m_real;
                double d = right.m_imaginary;

                if (Math.Abs(d) < Math.Abs(c))
                {
                    double doc = d / c;
                    return new Complex((a + b * doc) / (c + d * doc), (b - a * doc) / (c + d * doc));
                }
                else
                {
                    double cod = c / d;
                    return new Complex((b + a * cod) / (d + c * cod), (-a + b * cod) / (d + c * cod));
                }
            }


            // --------------SECTION: Other arithmetic operations  -------------- //

            public static Double Abs(Complex value)
            {

                if (Double.IsInfinity(value.m_real) || Double.IsInfinity(value.m_imaginary))
                {
                    return double.PositiveInfinity;
                }

                // |value| == sqrt(a^2 + b^2)
                // sqrt(a^2 + b^2) == a/a * sqrt(a^2 + b^2) = a * sqrt(a^2/a^2 + b^2/a^2)
                // Using the above we can factor out the square of the larger component to dodge overflow.


                double c = Math.Abs(value.m_real);
                double d = Math.Abs(value.m_imaginary);

                if (c > d)
                {
                    double r = d / c;
                    return c * Math.Sqrt(1.0 + r * r);
                }
                else if (d == 0.0)
                {
                    return c;  // c is either 0.0 or NaN
                }
                else
                {
                    double r = c / d;
                    return d * Math.Sqrt(1.0 + r * r);
                }
            }

            // --------------SECTION: Comparison Operator(binary) Overloading -------------- //

            public static bool operator ==(Complex left, Complex right)
            {
                return ((left.m_real == right.m_real) && (left.m_imaginary == right.m_imaginary));


            }
            public static bool operator !=(Complex left, Complex right)
            {
                return ((left.m_real != right.m_real) || (left.m_imaginary != right.m_imaginary));

            }

            // --------------SECTION: Comparison operations (methods implementing IEquatable<ComplexNumber>,IComparable<ComplexNumber>) -------------- //

            public override bool Equals(object obj)
            {
                if (!(obj is Complex)) return false;
                return this == ((Complex)obj);
            }
            public bool Equals(Complex value)
            {
                return ((this.m_real.Equals(value.m_real)) && (this.m_imaginary.Equals(value.m_imaginary)));

            }

            // --------------SECTION: Type-casting basic numeric data-types to ComplexNumber  -------------- //

            public static implicit operator Complex(Int32 value)
            {
                return (new Complex(value, 0.0));
            }
            public static implicit operator Complex(Double value)
            {
                return (new Complex(value, 0.0));
            }

            public override Int32 GetHashCode()
            {
                Int32 n1 = 99999997;
                Int32 hash_real = this.m_real.GetHashCode() % n1;
                Int32 hash_imaginary = this.m_imaginary.GetHashCode();
                Int32 final_hashcode = hash_real ^ hash_imaginary;
                return (final_hashcode);
            }
            public static Complex Sqrt(Complex value) /* Square root ot the complex number */
            {
                return Complex.FromPolarCoordinates(Math.Sqrt(value.Magnitude), value.Phase / 2.0);
            }
            public static Complex Pow(Complex value, Complex power) /* A complex number raised to another complex number */
            {

                if (power == Complex.Zero)
                {
                    return Complex.One;
                }

                if (value == Complex.Zero)
                {
                    return Complex.Zero;
                }

                double a = value.m_real;
                double b = value.m_imaginary;
                double c = power.m_real;
                double d = power.m_imaginary;

                double rho = Complex.Abs(value);
                double theta = Math.Atan2(b, a);
                double newRho = c * theta + d * Math.Log(rho);

                double t = Math.Pow(rho, c) * Math.Pow(Math.E, -d * theta);

                return new Complex(t * Math.Cos(newRho), t * Math.Sin(newRho));
            }
        }
        IMyTurretControlBlock radar;



        public Program()
        {
            radar = GridTerminalSystem.GetBlockWithName("radar") as IMyTurretControlBlock;
            TaskScheduler.EstablishTaskScheduler(Runtime, Echo, true);
            TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(IEnumMain));
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            TaskScheduler.StepCoroutines(updateSource, argument);
        }

        Vector3D targetVel1 = Vector3D.Zero;
        Vector3D targetVel2 = Vector3D.Zero;

        Vector3D targetAccel = Vector3D.Zero;
        Vector3D targetAccel1 = Vector3D.Zero;
        Vector3D targetAccel2 = Vector3D.Zero;

        Vector3D targetJerk = Vector3D.Zero;

        List<Vector3> accelerations = new List<Vector3>();
        List<Vector3> jerks = new List<Vector3>();

        public IEnumerator<int> IEnumMain()
        {
            while (true)
            {
                MyDetectedEntityInfo enemy = radar.GetTargetedEntity();

                if (enemy.Position != Vector3D.Zero)
                {
                    targetVel2 = targetVel1;
                    targetVel1 = enemy.Velocity;

                    targetAccel2 = targetAccel1;
                    targetAccel1 = (targetVel1 - targetVel2) / TaskScheduler.TimeStep;
                    accelerations.Add(targetAccel1);

                    if (accelerations.Count >= 15)
                    {
                        Vector3D total = Vector3D.Zero;
                        foreach (Vector3D acc in accelerations)
                        {
                            total += acc;
                        }
                        targetAccel = total / 15;
                        accelerations.RemoveAt(0);
                    }

                    jerks.Add((targetAccel1 - targetAccel2) / TaskScheduler.TimeStep);
                    if (jerks.Count >= 15)
                    {
                        Vector3D total = Vector3D.Zero;
                        foreach (Vector3D jerk in jerks)
                        {
                            total += jerk;
                        }
                        targetJerk = total / 15;
                        jerks.RemoveAt(0);
                    }

                    ImmutableArray<Vector3D> targetInfo = ImmutableArray.Create(new Vector3D[] { enemy.Position, enemy.Velocity, targetAccel, targetJerk });
                    IGC.SendBroadcastMessage("target", targetInfo);

                }

                Echo($"pos: {(Vector3I)enemy.Position}");
                Echo($"vel: {(Vector3I)targetVel1}");
                Echo($"acc: {(Vector3I)targetAccel1}");
                Echo($"jer: {(Vector3I)targetJerk}");
                yield return 0;
            }
        }
    }
}
