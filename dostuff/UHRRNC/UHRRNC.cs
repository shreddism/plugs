using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace UHRRNC
{
    [PluginName("Noise Compensation Test")]
    public class UHRRNC : IPositionedPipelineElement<IDeviceReport>
    {
        public UHRRNC() : base()
        {
        }

        public PipelinePosition Position => PipelinePosition.PreTransform;

        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();

        [Property("Noise Amount"), DefaultPropertyValue(5f)]
        public float opt1 { 
            set => _opt1 = (value);
            get => _opt1;
        }
        public float _opt1;

        [Property("Velocity Multiplier"), DefaultPropertyValue(0.99f)]
        public float opt2 { 
            set => _opt2 = (value);
            get => _opt2;
        }
        public float _opt2;

        [Property("opt3"), DefaultPropertyValue(0.5f)]
        public float opt3 { 
            set => _opt3 = (value);
            get => _opt3;
        }
        public float _opt3;

        [Property("opt4"), DefaultPropertyValue(1f)]
        public float opt4 { 
            set => _opt4 = (value);
            get => _opt4;
        }
        public float _opt4;

        [Property("opt5"), DefaultPropertyValue(1f)]
        public float opt5 { 
            set => _opt5 = (value);
            get => _opt5;
        }
        public float _opt5;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                float reportTime = (float)reportStopwatch.Restart().TotalMilliseconds;

                if (!init || reportTime > 25f) {
                    opos = report.Position;
                    odir = Vector2.Zero;
                    adir = Vector2.Zero;
                    wtf = Vector2.Zero;
                    init = true;
                    ldir = Vector2.Zero;
                    lpos = report.Position;
                    paccel = Vector2.Zero;
                    return;
                }

                if (init && opt1 > 0f && opt2 > 0f) {
                    cdir = report.Position - lpos;

                    caccel = cdir - ldir;       

                   /*float wale = 0.1f + 0.15f * Smoothstep(caccel.Length(), opt1 * 2f, opt1 * 4f) + 0.15f * Smoothstep(wtf.Length(), opt1 * 2f, opt1 * 4f);     

                    wale = inversion(wale, opt2);

                    wtf = (1f - wale) * wtf + (wale) * caccel;

                    adir += wtf * Smoothstep(wtf.Length(), opt1 * 0.0f, opt1 * 0.75f);

                    float vale = 0.15f + 0.05f * Smoothstep(cdir.Length(), opt1 * 0.5f, opt1 * 2f) + 0.05f * Smoothstep(adir.Length(), opt1 * 0.5f, opt1 * 2f) + 0.075f * Smoothstep(caccel.Length(), opt1 * 2f, opt1 * 4f) + 0.075f * Smoothstep(wtf.Length(), opt1 * 2f, opt1 * 4f);

                    vale = inversion(wale, opt2);
                    
                    adir = (1f - vale) * adir + (vale) * cdir;

                    opos += adir * Smoothstep(adir.Length(), opt1 * 0.2f, opt1 * 0.5f);

                    float ale = (0.5f + 0.5f * Smoothstep(adir.Length(), opt1 * 0.5f, opt1)) * Smoothstep(Vector2.Distance(opos, report.Position), opt1 * 0.0f, opt1 * 1.5f);

                    ale = inversion(ale, MathF.Sqrt(opt2));

                    opos = Vector2.Lerp(opos, report.Position, ale);

                    Console.WriteLine(Vector2.Distance(opos, report.Position));*/

                    wtf = Vector2.Lerp(wtf, caccel, inversion(0.1f, opt4));

                    deepac = Vector2.Lerp(deepac, caccel, inversion(0.25f, opt4));

                    paccel += wtf - caccel;

                    paccel *= opt3;
                    
                    wtf += (Math.Max(Smoothstep(caccel.Length(), opt1 * 4f, opt1 * 6f), Smoothstep(paccel.Length(), opt1, opt1 * 3f))) * Math.Max(Vector2.Distance(wtf, caccel) - opt1 * 2f, 0f) * Normalize((0.67f * deepac + 0.33f * caccel) - wtf);

                    adir += wtf * Smoothstep(wtf.Length(), opt1 * 0.25f, opt1);

                    adir = Vector2.Lerp(adir, cdir, inversion(0.1f, opt4));

                    dell = Vector2.Lerp(dell, cdir, inversion(0.25f, opt4));

                    pdir += adir - cdir;

                    pdir *= opt3;

                    adir += Math.Max(Smoothstep(cdir.Length(), opt1 * 2f, opt1 * 3f), Smoothstep(pdir.Length(), opt1, opt1 * 1.5f)) * Math.Max(Vector2.Distance(adir, cdir) - opt1, 0f) * Normalize(cdir - adir);

                    opos += opt2 * Smoothstep(adir.Length(), opt1 * 0.05f, opt1 * 0.33f) * adir;

                    opos = Vector2.Lerp(opos, report.Position, inversion(0.1f * Smoothstep(adir.Length(), opt1 * 4f, 0f), opt4));

                    ppos += opos - report.Position;
                    
                    ppos *= opt3;

                    Vector2 finaldiff = 0.5f * Math.Max(Smoothstep(Vector2.Distance(opos, report.Position), opt1 * 1f, opt1 * 1.5f), Smoothstep(ppos.Length(), opt1 * 0.5f, opt1 * 2f)) * Math.Max(Vector2.Distance(opos, report.Position) - opt1 * 0.5f, 0f) * Normalize(report.Position - opos);

                    opos += finaldiff;

                    adir += 0.5f * finaldiff;

                    wtf += 0.25f * finaldiff;

                    if (opt5 < 1f) {
                        smoo = Vector2.Lerp(smoo, opos, opt5);
                    }
                    else smoo = opos;

                    //opos = Vector2.Lerp(opos, report.Position, (0.5f + 0.5f * Smoothstep(adir.Length(), 0f, opt1)) * Smoothstep(ppos.Length(), 0f, opt1 * (2f - Smoothstep(adir.Length() - dell.Length(), 0f, -25f))));
                  Console.WriteLine(opos - report.Position);

                }
               
                lpos = report.Position;
                ldir = cdir;
                report.Position = smoo;
                odir = smoo - lopos;
                lopos = smoo;
                init = true;
            }
            Emit?.Invoke(value);
        }

        Vector2 opos, odir, lpos, lopos, ldir, caccel, wtf;
        Vector2 cdir, adir = Vector2.Zero;
        Vector2 wisc;
        Vector2 cdisc;
        Vector2 ucel;
        Vector2 smoo;
        Vector2 ppos, pdir, paccel;
        Vector2 deepac, dell;
        bool init;
        bool bstate1, bstate2, bstate3;

        public static void PlotD(string c, Vector2 p, bool t) 
        {
            Console.Write(c + "x");
            Console.WriteLine(p.X);
            Console.Write(c + "y");
            Console.WriteLine(p.Y * - 1);
            if (t) {
                Console.WriteLine("xx");
                Console.WriteLine("dd");
            }
        }

        public static float Smoothstep(float x, float start, float end)
        {
            x = Math.Clamp((x - start) / (end - start), 0.0f, 1.0f);
            return x * x * (3.0f - 2.0f * x);
        }

        public static float Sigmoid(float x, float a, float b, float c) 
        {
            return 1 / (1 + a * MathF.Pow(b, c * x));
        }

        public static float inversion(float x, float pow) {
            return 1 - MathF.Pow(1 - x, pow);
        }

        public static void InsertAtFirst<T>(T[] arr, T element)
        {
            for (int p = arr.Length - 1; p > 0; p--) arr[p] = arr[p - 1];
            arr[0] = element;
        }

        public Vector2 Normalize(Vector2 a) => (a != Vector2.Zero) ? (a / a.Length()) : Vector2.Zero;

    }

    [PluginName("Noise Machine")]
    public class noisemachine : IPositionedPipelineElement<IDeviceReport>
    {
        public noisemachine() : base()
        {
        }

        Random rng;

        public PipelinePosition Position => PipelinePosition.PreTransform;

        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();

        [Property("Maximum Length"), DefaultPropertyValue(0.0f)]
        public int opt1 { 
            set => _opt1 = (value);
            get => _opt1;
        }
        public int _opt1;

        [Property("Power"), DefaultPropertyValue(1.0f)]
        public float opt2 { 
            set => _opt2 = (value);
            get => _opt2;
        }
        public float _opt2;

        [Property("Mode"), DefaultPropertyValue(0)]
        public int opt3 { 
            set => _opt3 = value;
            get => _opt3;
        }
        public int _opt3;

        public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
            //    UHRRNC.PlotD("v", report.Position - lpos, false);
                lpos = report.Position;

                if (!init) {
                    rng = new Random();
                    init = true;
                }
                float rx, ry;
                Vector2 cv = Vector2.Zero;
                Vector2 pos = report.Position;
                if (opt3 == 0) {
                    rx = (float)rng.NextDouble() * opt1;
                    ry = (float)rng.NextDouble() * opt1;
                    rx = MathF.Pow(rx / opt1, opt2) * opt1;
                    ry = MathF.Pow(ry / opt1, opt2) * opt1;
                    cv.X = MathF.Round(rx, 0, MidpointRounding.AwayFromZero);
                    cv.Y = MathF.Round(ry, 0, MidpointRounding.AwayFromZero);
                    if (rng.NextDouble() > 0.5) pos.X += cv.X;
                        else pos.X -= cv.X;             
                    if (rng.NextDouble() > 0.5) pos.Y += cv.Y;
                        else pos.Y -= cv.Y;   
                }
                else {
                    rx = (float)rng.NextDouble() * opt1;
                    rx = MathF.Pow(rx / opt1, opt2) * opt1;
                    ry = (float)rng.NextDouble() * 2.0f * MathF.PI;
                    cv = Rotate(new Vector2(rx, 0), ry);
                    cv.X = MathF.Round(cv.X, 0, MidpointRounding.AwayFromZero);
                    cv.Y = MathF.Round(cv.Y, 0, MidpointRounding.AwayFromZero); 
                    pos += cv;
                }
                
                report.Position = pos;
            }
            Emit?.Invoke(value);
        }
        
        public static Vector2 Rotate(Vector2 p, float a)
        {
            float cosine = MathF.Cos(a);
            float sine = MathF.Sin(a);
            return new Vector2((cosine * p.X) - (sine * p.Y), (sine * p.X) + (cosine * p.Y));
        }

        bool init;

        Vector2 lpos;

    }
}