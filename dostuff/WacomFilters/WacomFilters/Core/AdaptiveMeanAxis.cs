using System;

namespace WacomFilters.Core;

internal sealed class AdaptiveMeanAxis
{
	private const int BaseDepth = 4;

	private const double JitterMinimum = 3.0;

	private const double JitterMaximum = 12.0;

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
		return (int)(num4 / _depth);
	}

	public void Reset()
	{
		_depth = 0.0;
	}

	private static int Target(byte height, double delta)
	{
		double num = (double)(int)height * 9.0 / 255.0 + 3.0;
		double num2 = 2.0 * num;
		if (delta * delta > num2 * num2)
		{
			return 4;
		}
		double num3 = (double)(int)height * 3.0 / 255.0 + 1.0;
		return (int)(4.0 * num3);
	}

	private static double Slew(double depth, int target, double step)
	{
		double num = (double)target - depth;
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
