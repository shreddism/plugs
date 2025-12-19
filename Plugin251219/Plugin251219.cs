using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;       

namespace Plugin251219
{
    [PluginName("Plugin251219")]
    public class Plugin251219 : IPositionedPipelineElement<IDeviceReport>
    {
        public Plugin251219() : base()
        {
        }

       public PipelinePosition Position => PipelinePosition.PreTransform;

       public event Action<IDeviceReport> Emit;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                pos1 = pos0;
                pos0 = report.Position;

                dir1 = dir0;
                dir0 = pos0 - pos1;

                outputVel0 = VelocityFilter(dir0);

                outputPos0 = outputPos0 + outputVel0;
            
                /*
                Console.WriteLine(Math.Sqrt(Math.Pow(dir0.X, 2) + Math.Pow(dir0.Y, 2)));
                Console.WriteLine(Math.Sqrt(Math.Pow(outputVel0.X, 2) + Math.Pow(outputVel0.Y, 2)));
                Console.WriteLine("------------------------------");
                */

                Console.WriteLine(pos0 - outputPos0);

                outputPos0 = Vector2.Lerp(outputPos0, pos0, 0.01f);
                
                report.Position = outputPos0;
                
            }
            Emit?.Invoke(value);
        }

        public Vector2 VelocityFilter(Vector2 velocity) {

            disc = dir0 - outputVel0;
            dist = MathF.Sqrt(MathF.Pow(disc.X, 2) + MathF.Pow(disc.Y, 2));
            compensation = MathF.Log((MathF.Sqrt(MathF.Pow(outputVel0.X, 2) + MathF.Pow(outputVel0.Y, 2)) / 25) + 1) + 1;
            scale = MathF.Pow(1 / (1 + (float)Math.Pow(Math.E, -1 * (dist - 0) / compensation)), 5);
            
            outputVel0 = Vector2.Lerp(outputVel0, velocity, scale);

            return outputVel0;
        }

        public Vector2 pos0, pos1, outputPos0, outputPos1, dir0, dir1, disc, outputVel0;
        public float dist, scale, compensation;
        
        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);

    }
}