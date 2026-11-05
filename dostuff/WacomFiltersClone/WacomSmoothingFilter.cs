using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace WacomFiltersPlus;

[PluginName("Wacom Smoothing Filter")]
public class Mean : IPositionedPipelineElement<IDeviceReport> 
{
    public PipelinePosition Position => PipelinePosition.PreTransform;
    public event Action<IDeviceReport>? Emit;

    [Property("Base Depth"), DefaultPropertyValue(4)]
    public int baseDepth { get; set; } = 4;

    [Property("Jitter Minimum"), DefaultPropertyValue(3.0)]
    public double jitterMinimum { get; set; } = 3.0;

    [Property("Jitter Maximum"), DefaultPropertyValue(12.0)]
    public double jitterMaximum { get; set; } = 12.0;
    
    private AdaptiveMeanAxis axisX;
    private AdaptiveMeanAxis axisY;

    public void Consume(IDeviceReport value) {
        if (report is ITabletReport report) {
            PenSample sample = new PenSample((double)report.Position.X, (double)report.Position.Y, report.Pressure);

        }
    }

    SampleHistory history = new SampleHistory();

    [OnDependencyLoad] 
    public void Initialize() {
        axisX = new AdaptiveMeanAxis(baseDepth, jitterMinimum, jitterMaximum);
        axisY = new AdaptiveMeanAxis(baseDepth, jitterMinimum, jitterMaximum);
    }

    private HPETDeltaStopwatch stopwatch = new HPETDeltaStopwatch();
}

internal sealed class AdaptiveMeanAxis
{

    public AdaptiveMeanAxis(int baseDepth, double jitterMinimum, double jitterMaximum) {
        BaseDepth = baseDepth;
        JitterMinimum = jitterMinimum;
        JitterMaximum = jitterMaximum;
    }

	private int BaseDepth;

    private double jitterMinimum, jitterMaximum;

	private double _depth;

	public int Update(SampleHistory history, SampleField field, byte normalizedHeight, double now, double step)
	{
		int num = history.TapCount(now);
		double delta = ((num < 3) ? 0.0 : (history.Tap(now, 2, field) - history.Tap(now, 1, field)));
		_depth = Math.Max(1.0, Slew(_depth, Target(normalizedHeight, delta), step));
		int num2 = (int)_depth;
		double num3 = _depth - (double)num2;
		double num4 = 0.0;
		for (int i = 0; i < num2; i++)
		{
			num4 += history.Tap(now, i % num, field);
		}
		if (num3 > 0.0)
		{
			num4 += num3 * history.Tap(now, num2, field);
		}
		return (num4 / _depth);
	}

	public void Reset()
	{
		_depth = 0.0;
	}

	private static int Target(byte height, double delta)
	{
		double num = height * (jitterMaximum - jitterMinimum) / 255.0 + jitterMinimum;
		double num2 = 2.0 * num;
		if (delta * delta > num2 * num2)
		{
			return 4;
		}
		double num3 = height * 3.0 / 255.0 + 1.0;
		return (4.0 * num3);
	}

	private static double Slew(double depth, double target, double step)
	{
		double num = target - depth;
		if (num > 0.0)
		{
			return Math.Min(target, depth + ((num >= 5.0) ? 2.0 : 1.0) * step);
		}
		if (num < 0.0)
		{
			return Math.Max(target, depth - step);
		}
		return depth;
	}
}
