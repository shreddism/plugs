using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Saturn
{
    [PluginName("Saturn - Noise Compensation Test")]
    public class NCT : IPositionedPipelineElement<IDeviceReport>
    {
        public NCT() : base()
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

        [Property("opt6"), DefaultPropertyValue(1f)]
        public float opt6 { 
            set => _opt6 = (value);
            get => _opt6;
        }
        public float _opt6;

        [BooleanProperty("opt7", ""), DefaultPropertyValue(false)]
        public bool opt7 { 
            set => _opt7 = value;
            get => _opt7;
        }
        public bool _opt7;

        [BooleanProperty("opt8", ""), DefaultPropertyValue(false)]
        public bool opt8 {
            set => _opt8 = value;
            get => _opt8;
        }
        public bool _opt8;

        [Property("opt9"), DefaultPropertyValue(1f)]
        public float opt9 { 
            set => _opt9 = (value);
            get => _opt9;
        }
        public float _opt9;

        public event Action<IDeviceReport> Emit;

        int pt;

        public void Consume(IDeviceReport value)
        {
            tick++;

            if (value is ITabletReport report)
            {
                float reportTime = (float)reportStopwatch.Restart().TotalMilliseconds;
                reportMsAvg = 0.9f * reportMsAvg + 0.1f * reportTime;


                if (tabletType == 1 && reportMsAvg < 6.25f && tick > 10) {
                    adjustmentType = 1;
                }

                if (!init || reportTime > 25f) { 
                    TabletMode(name);
                    opos = report.Position;
                    odir = Vector2.Zero;
                    adir = Vector2.Zero;
                    wtf = Vector2.Zero;
                    init = true;
                    ldir = Vector2.Zero;
                    lpos = report.Position;
                    paccel = Vector2.Zero;
                    ppos = Vector2.Zero;
                    pdir = Vector2.Zero;
                    return;
                }

                    cdir = report.Position - lpos;


                    caccel = cdir - ldir;   

                  
                    conf = opt1;


             

                    wtf = Vector2.Lerp(wtf, caccel, inversion(0.1f, opt4));

                    deepac = Vector2.Lerp(deepac, caccel, inversion(0.25f, opt4));

                    paccel += wtf - caccel;

                    paccel *= opt3;

                    Vector2 acdiff = (Math.Max(Smoothstep(caccel.Length(), conf * 4f, conf * 6f), Smoothstep(paccel.Length(), conf, conf * 3f))) * Math.Max(Vector2.Distance(wtf, caccel) - conf * 2f, 0f) * Normalize((0.67f * deepac + 0.33f * caccel) - wtf);
                    
                    wtf += opt9 * acdiff;
                    
                    adir += opt2 * wtf * Smoothstep(wtf.Length(), conf * 0.25f, conf);

                    adir = Vector2.Lerp(adir, cdir, inversion(0.1f, opt4));

                    dell = Vector2.Lerp(dell, cdir, inversion(0.25f, opt4));

                    pdir += adir - cdir;

                    pdir *= opt3;

                    Vector2 vdiff = Math.Max(Smoothstep(cdir.Length(), conf * 2f, conf * 3f), Smoothstep(pdir.Length(), conf, conf * 1.5f)) * Math.Max(Vector2.Distance(adir, cdir) - conf, 0f) * Normalize(cdir - adir);
                
                    adir += opt9 * vdiff;
                    wtf += (opt9 / 2.0f) * vdiff;
                    
                    opos += Smoothstep(Vector2.Distance(opos, report.Position), 0, opt1 * 0.25f) * opt2 * Smoothstep(adir.Length(), conf * 0.05f, conf * 0.33f) * adir;

                    opos = Vector2.Lerp(opos, report.Position, inversion(0.1f * Smoothstep(adir.Length(), conf * 4f, 0f), opt4));

                    ppos += opos - report.Position;
                    
                    ppos *= MathF.Sqrt(opt3);

                    Vector2 finaldiff = Math.Max(Smoothstep(Vector2.Distance(opos, report.Position), conf * 1f * Smoothstep(adir.Length(), conf, 0f), conf * opt6), Smoothstep(ppos.Length(), conf * 0.5f * opt6, conf * 1.5f * opt6)) * Math.Max(Vector2.Distance(opos, report.Position) - conf * 0.5f, 0f) * Normalize(report.Position - opos);

                    opos += (opt9) * finaldiff;
                    adir += (opt9 / 2.0f) * finaldiff;
                    wtf += (opt9 / 4.0f) * finaldiff;
                    
                    odir = opos - lopos;
                    lopos = opos;

                    if (opt5 < 1f) {
                        smoo = Vector2.Lerp(smoo, opos, opt5);
                    }
                    else smoo = opos;


                 //   opos = Vector2.Lerp(opos, report.Position, (0.5f + 0.5f * Smoothstep(adir.Length(), 0f, opt1)) * Smoothstep(ppos.Length(), 0f, opt1 * (2f - Smoothstep(adir.Length() - dell.Length(), 0f, -25f))));
                 

                    float c = Vector2.Cross(Normalize(cdir), Normalize(ldir));

                    if (float.IsFinite(c)) {
                        ac = 0.9f * ac + 0.1f * c;
                    }
                    
                    if (cdir.Length() > 25) {
                     //   Console.WriteLine(ac);
                    }
           // Console.WriteLine("?");

                
                lpos = report.Position;
                ldir = cdir;
                sim = Vector2.Lerp(sim, report.Position, opt5);
                simdir = sim - lsim;
                lsim = sim;
                

            //    PlotD("v", cdir, false);
            //    PlotD("a", simdir, false);
             //   PlotD("j", odir, true);
                sd = smoo - ls;

                //PlotD("v", simdir, false);
               // PlotD("j", sd, true);

               init = true;
                if (!float.IsFinite(smoo.X)) {
                    Console.WriteLine("??");
                    smoo = report.Position;
                    init = false;
                    
                }

                     //   Console.WriteLine(report.Position - opos);


                report.Position = smoo;


                ls = smoo;

                
                
                
            }
            Emit?.Invoke(value);
        }

        float reportMsAvg;
        int tick;

        float conf;

        float ac = 0f;

        Vector2 opos, odir, lpos, lopos, ldir, caccel, wtf;
        Vector2 cdir, adir = Vector2.Zero;
        Vector2 wisc;
     //   float[] cross = new float[6];
        Vector2 cdisc;
        Vector2 ucel;
        Vector2 smoo, sd, ls;
        Vector2 sim, lsim, simdir;
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

        [TabletReference]
        public TabletReference TabletReference { set { name = value.Properties.Name; } }
        public string name = string.Empty;

        public int tabletType;

        public void TabletMode(string tabletName) {
            switch (tabletName) {
                case "Wacom CTL-480":
                    tabletType = 1;
                    reportMsAvg = 7.5f;
                break;
                default:
                    tabletType = 0;
                break;
            }

            //Console.WriteLine(tabletType);
        }

        public int adjustmentType = 0;

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
              //  UHRRNC.PlotD("s", report.Position - lpos, false);
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