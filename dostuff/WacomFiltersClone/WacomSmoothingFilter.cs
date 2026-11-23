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
using WacomFiltersPlus.Core;

namespace WacomFiltersPlus;

[PluginName("Wacom Smoothing Filter")]
public class Mean : IPositionedPipelineElement<IDeviceReport> 
{
    public PipelinePosition Position => PipelinePosition.PreTransform;
    public event Action<IDeviceReport>? Emit;

    [Property("Base Depth"), DefaultPropertyValue(4.0)]
    public double baseDepth { get; set; } = 4.0;

	[Property("Time Window"), DefaultPropertyValue(0.12781954887218044)]
    public double keepSeconds { get; set; } = 0.12781954887218044;
	
	[Property("Reference Rate"), DefaultPropertyValue(133.0)]
    public double referenceRate { get; set; } = 133.0;

	[Property("Time Step"), DefaultPropertyValue(1.0)]
    public double timeStep { get; set; } = 1.0;
    
    private AdaptiveMeanAxis axisX;
    private AdaptiveMeanAxis axisY;
	private double clock;

    public void Consume(IDeviceReport value) {
        if (value is ITabletReport report) {
			double reportTime = reportStopwatch.Restart().TotalSeconds;
			if (reportTime > keepSeconds) {
				history.Clear();
				clock = reportTime;
			}
            PenSample sample = new PenSample((double)stopwatch.Elapsed.TotalSeconds, (double)report.Position.X, (double)report.Position.Y, (int)report.Pressure, 0, false, false, new byte[4]);
			history.Add(sample);
			double X = (double)report.Position.X;
			double Y = (double)report.Position.Y;
			if (reportTime < 1.0 / referenceRate) {
				clock += timeStep * (1.0 / referenceRate);
			}
			else {
				clock = timeStep * (double)stopwatch.Elapsed.TotalSeconds;
			}
			float FX = (float)axisX.Update(history, SampleField.X, 0, clock);
			float FY = (float)axisX.Update(history, SampleField.Y, 0, clock);
			Vector2 save = report.Position;
            report.Position = new Vector2(FX, FY);
        }
		Emit.Invoke(value);
    }

    SampleHistory history;

    [OnDependencyLoad] 
    public void Initialize() {
		history = new SampleHistory(keepSeconds, referenceRate);
        axisX = new AdaptiveMeanAxis(baseDepth);
        axisY = new AdaptiveMeanAxis(baseDepth);
    }

    private HPETDeltaStopwatch stopwatch = new HPETDeltaStopwatch();
    private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
}

internal sealed class AdaptiveMeanAxis
{
    public AdaptiveMeanAxis(double baseDepth) {
        BaseDepth = baseDepth;
    }

	private double BaseDepth;
	private double _depth;

	public double Update(SampleHistory history, SampleField field, byte normalizedHeight, double now)
	{
		int num = history.TapCount(now);
		double delta = ((num < 3) ? 0.0 : (history.Tap(now, 2, field) - history.Tap(now, 1, field)));
		int num2 = (int)BaseDepth;
		double num3 = BaseDepth - (double)num2;
		double num4 = 0.0;
		for (int i = 0; i < num2; i++)
		{
			num4 += history.Tap(now, i % num, field);
		}
		if (num3 > 0.0)
		{
			num4 += num3 * history.Tap(now, num2, field);
		}
		return (num4 / BaseDepth);
	}
}
