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

        public class PIDController
        {
            public double p;
            public double i;
            public double d;
            public double lastError = 0;
            public double lastErrorInt = 0;
            public Action<string> Echo;

            public double PID(double error, double timestep)
            {
                lastErrorInt += error * timestep;
                Echo($"({error} * {p}) + (({error} - {lastError}) / {timestep} * {d})");
                double result = (error * p) + ((error - lastError) / timestep * d);
                lastError = error;


                return result;
            }

            public PIDController(double _p, double _i, double _d)
            {
                p = _p;
                i = _i;
                d = _d;
            }
        }

        public static class Polynomial
        {
            static double a;
            static double b;
            static double c;
            static double d;
            static double e;
            static double f;
            static double g;

            static Vector3D pos;
            static Vector3D vel;
            static Vector3D accel;

            public static Action<string> Echo;

            public static void SetVariables(Vector3D _pos, Vector3D _vel, Vector3D _accel, double speed)
            {
                pos = _pos;
                vel = _vel;
                accel = _accel;

                a = (accel.X * accel.X + accel.Y * accel.Y + accel.Z * accel.Z) / 4;
                b = accel.X * vel.X + accel.Y * vel.Y + accel.Z * vel.Z;
                c = vel.X * vel.X + pos.X * accel.X +
                          vel.Y * vel.Y + pos.Y * accel.Y +
                          vel.Z * vel.Z + pos.Z * accel.Z - (speed * speed);
                d = 2 * (pos.X * vel.X + pos.Y * vel.Y + pos.Z * vel.Z);
                e = pos.X * pos.X + pos.Y * pos.Y + pos.Z * pos.Z;
            }

            public static double[] SolveQuartic()
            {
                double D0 = c * c - 3 * b * d + 12 * a * e;
                double D1 = 2 * c * c * c - 9 * b * c * d + 27 * b * b * e + 27 * a * d * d - 72 * a * c * e;
                double p = (8 * a * c - 3 * b * b) / (8 * a * a);
                double q = (b * b * b - 4 * a * b * c + 8 * a * a * d) / (8 * a * a * a);
                Complex Q = Complex.Pow((D1 + Complex.Sqrt(D1 * D1 - 4 * D0 * D0 * D0)) / 2, 1.0 / 3.0);
                Complex S = Complex.Sqrt(-2 * p / 3 + (Q + D0 / Q) / (3 * a)) / 2;
                Complex u = Complex.Sqrt(-4 * S * S - 2 * p + q / S) / 2;
                Complex v = Complex.Sqrt(-4 * S * S - 2 * p - q / S) / 2;
                Complex[] output = new Complex[4];
                output[0] = -b / (4 * a) - S + u;
                output[1] = -b / (4 * a) - S - u;
                output[2] = -b / (4 * a) + S + v;
                output[3] = -b / (4 * a) + S - v;

                return new double[] { output[0].Real, output[1].Real, output[2].Real, output[3].Real };
            }

            public static double[] SolveQuadratic()
            {
                double sqrt = Math.Sqrt(b * b - (4 * a * c));
                if (sqrt == double.NaN) { return null; }

                return new double[] { (-b - sqrt) / (2 * a), (-b + sqrt) / (2 * a) };
            }

            public static Vector3D GetInterceptPoint()
            {
                double[] roots;

                //Echo("1");
                if (a == 0)
                {
                    if (d == 0)
                    {
                        //Echo("2");
                        return pos;
                    }

                    //Echo(e.ToString());
                    roots = SolveQuadratic();
                }
                else
                {
                    //Echo("3");
                    roots = SolveQuartic();
                }

                float t = -1;

                //Echo("4");
                foreach (double x in roots)
                {
                    if (x > 0 && (x < t || t < 0))
                    {
                        t = (float)x;
                    }
                }

                if (t < 0)
                {
                    return Vector3D.Zero;
                }

                //Echo("5");
                return pos + t * vel + (t / 2) * accel;
            }
        }

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
        IMyBroadcastListener listener;
        IMyRemoteControl rc;
        List<IMyGyro> gyros = new List<IMyGyro>();

        IMyThrust fwd1;
        IMyThrust fwd2;

        public Program()
        {
            Polynomial.Echo = Echo;
            radar = GridTerminalSystem.GetBlockWithName("radar") as IMyTurretControlBlock;
            TaskScheduler.EstablishTaskScheduler(Runtime, Echo, true);
            TaskScheduler.SpawnCoroutine(new Func<IEnumerator<int>>(IEnumMain));

            rc = (IMyRemoteControl)GridTerminalSystem.GetBlockWithName("rc");
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);

            fwd1 = GridTerminalSystem.GetBlockWithName("fwd1") as IMyThrust;
            fwd2 = GridTerminalSystem.GetBlockWithName("fwd2") as IMyThrust;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            TaskScheduler.StepCoroutines(updateSource, argument);
        }

        MyTuple<double, Vector3D> targetVel1 = new MyTuple<double, Vector3D>(0, Vector3D.Zero);
        MyTuple<double, Vector3D> targetVel2 = new MyTuple<double, Vector3D>(0, Vector3D.Zero);
        Vector3D targetAcceleration = Vector3D.Zero;
        Vector3D finalTargetVel = Vector3D.Zero;
        Vector3D interceptPoint = Vector3D.Zero;
        Vector3D localPos = Vector3D.Zero;

        IEnumerator<int> IEnumMain()
        {
            Vector3D testPoint = new Vector3D(53418.35, -26840.46, 12298.07);
            listener = IGC.RegisterBroadcastListener("target");
            int count = 0;
            PIDController pitch = new PIDController(10, 0, 20);
            PIDController yaw = new PIDController(10, 0, 20);
            pitch.Echo = Echo;
            yaw.Echo = Echo;

            fwd1.ThrustOverridePercentage = 1;
            fwd2.ThrustOverridePercentage = 1;

            foreach (IMyGyro gyro in gyros)
            {
                gyro.GyroOverride = true;
            }

            while (true)
            {
                MyDetectedEntityInfo enemy = radar.GetTargetedEntity();


                if (listener.HasPendingMessage && enemy.Position == Vector3D.Zero)
                {
                    targetVel2 = targetVel1;
                    ImmutableArray<Vector3D> data = (ImmutableArray<Vector3D>)listener.AcceptMessage().Data;

                    localPos = Vector3DExtensions.ConvertToLocalPosition(data[0], rc);
                    finalTargetVel = Vector3DExtensions.ConvertToLocalDirection(data[1], rc);
                    targetAcceleration = Vector3DExtensions.ConvertToLocalDirection(data[2], rc);
                }

                if (enemy.Position != Vector3D.Zero && (enemy.TimeStamp - targetVel1.Item1) > 100)
                {
                    targetVel2 = targetVel1;
                    targetVel1 = new MyTuple<double, Vector3D>(enemy.TimeStamp, enemy.Velocity);

                    localPos = Vector3DExtensions.ConvertToLocalPosition(enemy.Position, rc);
                    finalTargetVel = Vector3DExtensions.ConvertToLocalDirection(enemy.Velocity, rc);
                    targetAcceleration = Vector3DExtensions.ConvertToLocalDirection((targetVel1.Item2 - targetVel2.Item2) / ((double)(targetVel1.Item1 - targetVel2.Item1) / 1000), rc);

                    if (interceptPoint == null) interceptPoint = Vector3D.Zero;
                }

                Polynomial.SetVariables(localPos, finalTargetVel, targetAcceleration, 100);
                interceptPoint = Polynomial.GetInterceptPoint();

                double timestep = Runtime.TimeSinceLastRun.TotalSeconds;

                /*foreach (IMyGyro gyro in gyros)
                {
                    Echo(timestep.ToString());
                    Echo(pitch.PID(interceptDir.Y, timestep).ToString());
                    Echo(yaw.PID(interceptDir.X, timestep).ToString());
                    gyro.Pitch = (float)pitch.PID(interceptDir.Y, timestep);
                    gyro.Yaw = (float)yaw.PID(interceptDir.X, timestep);
                }*/
                Vector3D localTestPoint = Vector3DExtensions.ConvertToLocalPosition(testPoint, rc);

                double pitchAngle = Math.Atan(interceptPoint.Y / interceptPoint.Z);
                double yawAngle = Math.Atan(interceptPoint.X / interceptPoint.Z);

                /*double pitchAngle = Math.Atan(localTestPoint.Y / localTestPoint.Z);
                double yawAngle = Math.Atan(localTestPoint.X / localTestPoint.Z);*/

                foreach (IMyGyro gyro in gyros)
                {
                    Echo(timestep.ToString());
                    //Echo(pitch.PID(localTestPoint.Y, timestep).ToString());
                    //Echo(yaw.PID(localTestPoint.X, timestep).ToString());
                    gyro.Pitch = (float)pitch.PID(pitchAngle, timestep);
                    gyro.Yaw = (float)yaw.PID(-yawAngle, timestep);
                }

                count++;
                if (count > 60)
                {
                    //break;
                }

                

                /*Echo($"pos: {(Vector3I)localPos}");
                Echo($"vel: {(Vector3I)targetVel1.Item2}");
                Echo($"acc: {(Vector3I)targetAcceleration}");
                Echo($"Int: {(Vector3I)interceptPoint}");
                Echo($"Int: {interceptDir}");*/
                yield return 0;
            }
        }
    }
}
