using System;
using System.Collections.Generic;

namespace WacomFilters.Core;

internal sealed class ProbabilisticAxis
{
	private readonly record struct State(double Time, double Position, double Variance);

	private const double Measurement = 9.0;

	private const double Process = 0.15;

	private const double Acceleration = 0.005;

	private const double InitialVariance = 20.0;

	private readonly List<State> _history = new List<State>();

	private double _time;

	private int _previousRaw;

	public int Update(int raw, double step)
	{
		if (_history.Count == 0)
		{
			_time = 0.0;
			_previousRaw = raw;
			_history.Add(new State(0.0, raw, InitialVariance));
			return raw;
		}
		double time = _time;
		double num = time + step;
		while (num - _time > 1.0)
		{
			double num2 = (_time + 1.0 - time) / step;
			Advance((double)_previousRaw + (double)(raw - _previousRaw) * num2, _time + 1.0);
		}
		Advance(raw, num);
		_previousRaw = raw;
		List<State> history = _history;
		return (int)Math.Round(history[history.Count - 1].Position, MidpointRounding.AwayFromZero);
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
