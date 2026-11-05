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

[PluginName("Wacom Kalman Filter")]
public class Probabilistic : IPositionedPipelineElement<IDeviceReport>
{
    public PipelinePosition Position => PipelinePosition.PreTransform;
    public event Action<IDeviceReport>? Emit;

    [Property("Measurement"), DefaultPropertyValue(9.0)]
    public double measurement { get; set; } = 9.0;

    [Property("Process"), DefaultPropertyValue(0.15)]
    public double process { get; set; } = 0.15;

    [Property("Acceleration"), DefaultPropertyValue(0.005)]
    public double acceleration { get; set; } = 0.005;
    
    public void Consume(IDeviceReport value) {
        if (value is ITabletReport report) {
            if (stopwatch.Restart().TotalMilliseconds > 25) {
                axisX.Reset();
                axisY.Reset();
            }
            Vector2 save = report.Position;
            double X = (double)report.Position.X;
            double Y = (double)report.Position.Y;
            float FX = (float)axisX.Update(X, 1);
            float FY = (float)axisY.Update(Y, 1);
            report.Position = new Vector2(FX, FY);
            Console.WriteLine(report.Position - save);
        }
        Emit.Invoke(value);
    }
    private ProbabilisticAxis axisX;
    private ProbabilisticAxis axisY;

    [OnDependencyLoad] 
    public void Initialize() {
        axisX = new ProbabilisticAxis(measurement, process, acceleration);
        axisY = new ProbabilisticAxis(measurement, process, acceleration);
    }

    private HPETDeltaStopwatch stopwatch = new HPETDeltaStopwatch();
}


internal sealed class ProbabilisticAxis
{
	private readonly record struct State(double Time, double Position, double Variance);

    public ProbabilisticAxis(double measurement, double process, double acceleration) {
        Measurement = measurement;
        Process = process;
        Acceleration = acceleration;
    }

    private double Measurement, Process, Acceleration, InitialVariance;

	private readonly List<State> _history = new List<State>();

	private double _time;

	private double _previousRaw;

	public double Update(double raw, double step)
	{
		if (_history.Count == 0)
		{
			_time = 0.0;
			_previousRaw = raw;
			_history.Add(new State(0.0, raw, 20.0));
			return raw;
		}
		double time = _time;
		double num = time + step;
		while (num - _time > 1.0)
		{
			double num2 = (_time + 1.0 - time) / step;
			Advance(_previousRaw + (raw - _previousRaw) * num2, _time + 1.0);
		}
		Advance(raw, num);
		_previousRaw = raw;
		List<State> history = _history;
		return history[history.Count - 1].Position;
	}

	public void Reset()
	{
		_history.Clear();
	}

	private void Advance(double raw, double time)
	{
		double num = time - 1.0;
		int i;
		for (i = 0; i + 1 < _history.Count && _history[i + 1].Time <= num; i++)
		{
		}
		_history.RemoveRange(0, i);
		State state = _history[0];
		double num2 = state.Position;
		double num3 = state.Variance;
		if (_history.Count > 1 && num > state.Time)
		{
			State state2 = _history[1];
			double num4 = (num - state.Time) / (state2.Time - state.Time);
			num2 += (state2.Position - num2) * num4;
			num3 += (state2.Variance - num3) * num4;
		}
		double num5 = raw - num2;
		double num6 = num5 * num5 * Acceleration + (num3 + Process);
		double num7 = num6 / (num6 + Measurement);
		_history.Add(new State(time, num5 * num7 + num2, (1.0 - num7) * num6));
		_time = time;
	}
}
