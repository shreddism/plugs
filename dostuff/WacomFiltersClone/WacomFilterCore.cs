using System;
using System.Numerics;
using System.Collections.Generic;

namespace WacomFiltersPlus.Core;

internal sealed class SampleHistory
{
	public double KeepSeconds;
	public double ReferenceRate = 133.0;

	public SampleHistory(double window, double period) {
		KeepSeconds = window;
		ReferenceRate = period;
	}

	private const double MachineEpsilon = 2.220446049250313E-16;

	public List<PenSample> _samples = new List<PenSample>();

	public void Add(PenSample sample)
	{
		double cutoff = sample.TimeSeconds - KeepSeconds;
        
        int drop = 0;
        while (drop + 1 < _samples.Count && _samples[drop + 1].TimeSeconds <= cutoff)
            drop++;

        _samples.RemoveRange(0, drop);
        _samples.Add(sample);
	}

	public int TapCount(double now)
	{
		double timeSeconds = _samples[0].TimeSeconds;
		double num = (now - timeSeconds) / (1.0 / ReferenceRate);
		double num2 = Tolerance(Math.Max(Math.Abs(now), Math.Abs(timeSeconds)), 1.0 / ReferenceRate);
		return 1 + (int)Math.Min(num + num2, 2147483646.0);
	}

	public double Tap(double now, int lag, SampleField field)
	{
		double num = now - (double)lag * (1.0 / ReferenceRate);
		if (num <= _samples[0].TimeSeconds)
		{
			return Value(_samples[0], field);
		}
		List<PenSample> samples = _samples;
		if (num >= samples[samples.Count - 1].TimeSeconds)
		{
			List<PenSample> samples2 = _samples;
			return Value(samples2[samples2.Count - 1], field);
		}
		int num2 = 0;
		int num3 = _samples.Count - 1;
		while (num3 - num2 > 1)
		{
			int num4 = (num2 + num3) / 2;
			if (_samples[num4].TimeSeconds <= num)
			{
				num2 = num4;
			}
			else
			{
				num3 = num4;
			}
		}
		PenSample sample = _samples[num2];
		PenSample sample2 = _samples[num3];
		double num5 = sample2.TimeSeconds - sample.TimeSeconds;
		double num6 = (num - sample.TimeSeconds) / num5;
		double num7 = Tolerance(Math.Max(Math.Abs(sample.TimeSeconds), Math.Abs(sample2.TimeSeconds)), num5);
		if (num6 <= num7)
		{
			return Value(sample, field);
		}
		if (1.0 - num6 <= num7)
		{
			return Value(sample2, field);
		}
		double num8 = Value(sample, field);
		return num8 + (Value(sample2, field) - num8) * num6;
	}

	public void Clear()
	{
		_samples.Clear();
	}

	private static double Tolerance(double scale, double interval)
	{
		return Math.Min(1E-06, 1.7763568394002505E-15 * (scale / interval));
	}

	public static double Value(PenSample sample, SampleField field)
	{
		return field switch
		{
			SampleField.X => sample.X, 
			SampleField.Y => sample.Y, 
			SampleField.Pressure => sample.Pressure, 
			_ => 255 - sample.RawHeight, 
		};
	}
}


internal enum WacomPreset
{
	Driver639,
	Driver6310,
	Driver6415,
	Driver6415KFilter
}

internal enum SampleField
{
	X,
	Y,
	Pressure,
	Height
}

internal readonly record struct PressureResult(int Pressure, bool TipDown);

internal readonly record struct PenSample(double TimeSeconds, double X, double Y, int Pressure, int RawHeight, bool HasHeight, bool Eraser, byte[] PenButtons);
internal readonly record struct FilteredSample(double X, double Y, int Pressure, bool TipDown, byte[] PenButtons);

internal sealed class HeightCalibration
{
	private const int Maximum = 120;

	private int _minimum;

	private bool _captureMinimum;

	public byte Update(int height, int pressure)
	{
		int num = Math.Min(height, 120);
		_minimum = (_captureMinimum ? num : Math.Min(_minimum, num));
		_captureMinimum = pressure > 10;
		if (_minimum == 120)
		{
			return 0;
		}
		return (byte)(255 * (num - _minimum) / (120 - _minimum));
	}
}
