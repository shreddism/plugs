using System;

namespace WacomFilters.Core;

internal sealed class WacomProcessor
{
	public const int MaxX = 15200;

	public const int MaxY = 9500;

	public const double ReferencePeriod = 1.0 / 133.0;

	public const double GapSeconds = 0.25;

	private const int PeriodIntervals = 64;

	private const int HysteresisRadius = 5;

	private const int CtcHysteresisRadius = 2;

	private const int HeightDepth = 4;

	private const int PressureDepth = 4;

	private readonly WacomPreset _preset;

	private readonly bool _lowLatency;

	private readonly AlphaBetaAxis _alphaBetaX = new AlphaBetaAxis();

	private readonly AlphaBetaAxis _alphaBetaY = new AlphaBetaAxis();

	private readonly ProbabilisticAxis _probabilisticX = new ProbabilisticAxis();

	private readonly ProbabilisticAxis _probabilisticY = new ProbabilisticAxis();

	private readonly AdaptiveMeanAxis _meanX = new AdaptiveMeanAxis();

	private readonly AdaptiveMeanAxis _meanY = new AdaptiveMeanAxis();

	private readonly SampleHistory _history = new SampleHistory();

	private readonly HeightCalibration _heightCalibration = new HeightCalibration();

	private readonly PressureFilter _pressure = new PressureFilter();

	private readonly ButtonFilters _buttons = new ButtonFilters();

	private PenSample? _previousSample;

	private double _reportPeriod;

	private int _intervals;

	private double _clock;

	private byte _normalizedHeight;

	private int _hysteresisX;

	private int _hysteresisY;

	public WacomProcessor(WacomPreset preset, bool lowLatency = false)
	{
		_preset = preset;
		_lowLatency = lowLatency;
	}

	public FilteredSample Process(PenSample sample)
	{
		double timeSeconds = sample.TimeSeconds;
		PenSample? previousSample = _previousSample;
		if (previousSample.HasValue)
		{
			PenSample valueOrDefault = previousSample.GetValueOrDefault();
			double num = timeSeconds - valueOrDefault.TimeSeconds;
			int num2;
			if (!(num <= 0.0))
			{
				num2 = ((num >= Math.Max(0.25, 8.0 * _reportPeriod)) ? 1 : 0);
				if (num2 == 0)
				{
					_intervals = Math.Min(_intervals + 1, 64);
					_reportPeriod += (num - _reportPeriod) / (double)_intervals;
				}
			}
			else
			{
				num2 = 1;
			}
			if (num2 != 0 || sample.Eraser != valueOrDefault.Eraser || sample.HasHeight != valueOrDefault.HasHeight)
			{
				Reset();
			}
		}
		double num3 = ((_reportPeriod > 0.0) ? (_reportPeriod / (1.0 / 133.0)) : 1.0);
		if (_lowLatency)
		{
			num3 = Math.Max(1.0, num3);
		}
		else if (Math.Abs(num3 - 1.0) <= 0.01)
		{
			num3 = 1.0;
		}
		_clock += num3 * (1.0 / 133.0);
		int pressure = sample.Pressure;
		int candidate;
		int candidate2;
		if (_preset == WacomPreset.Driver639)
		{
			candidate = _alphaBetaX.Update(sample.X, num3);
			candidate2 = _alphaBetaY.Update(sample.Y, num3);
		}
		else if (_preset == WacomPreset.Driver6415KFilter)
		{
			candidate = _probabilisticX.Update(sample.X, num3);
			candidate2 = _probabilisticY.Update(sample.Y, num3);
		}
		else
		{
			_history.Add(sample with
			{
				TimeSeconds = _clock
			});
			if (!sample.HasHeight || _lowLatency)
			{
				_normalizedHeight = 0;
			}
			candidate = _meanX.Update(_history, SampleField.X, _normalizedHeight, _clock, num3);
			candidate2 = _meanY.Update(_history, SampleField.Y, _normalizedHeight, _clock, num3);
			if (sample.HasHeight && !_lowLatency)
			{
				_normalizedHeight = _heightCalibration.Update(MeanHeight(_history, _clock), sample.Pressure);
			}
			if (!_lowLatency)
			{
				pressure = DelayedPressure(_history, _clock);
			}
		}
		int radius = _preset switch
		{
			WacomPreset.Driver6415KFilter => 0, 
			WacomPreset.Driver6415 => 2, 
			_ => 5, 
		};
		candidate = (_hysteresisX = SpatialHysteresis(candidate, _hysteresisX, 15200, radius));
		candidate2 = (_hysteresisY = SpatialHysteresis(candidate2, _hysteresisY, 9500, radius));
		_pressure.Observe(sample.Pressure, timeSeconds, sample.Eraser);
		_previousSample = sample;
		WacomPreset preset = _preset;
		bool flag = (uint)(preset - 2) <= 1u;
		bool truncate = flag;
		return _buttons.Apply(candidate, candidate2, _pressure.Convert(pressure, sample.Eraser, truncate), sample);
	}

	public void Reset()
	{
		_alphaBetaX.Reset();
		_alphaBetaY.Reset();
		_probabilisticX.Reset();
		_probabilisticY.Reset();
		_meanX.Reset();
		_meanY.Reset();
		_history.Clear();
		_pressure.Reset();
		_buttons.Reset();
		_previousSample = null;
		_clock = 0.0;
	}

	public static int SpatialHysteresis(int candidate, int previous, int axisCount, int radius = 5)
	{
		int value = candidate - previous;
		if (Math.Abs(value) <= radius)
		{
			if (candidate <= 0 || candidate >= axisCount - 1)
			{
				return candidate;
			}
			return previous;
		}
		return candidate - Math.Sign(value) * radius;
	}

	public static int DelayedPressure(SampleHistory history, double now)
	{
		int lag = Math.Min(4, history.TapCount(now)) / 2;
		return (int)Math.Round(history.Tap(now, lag, SampleField.Pressure), MidpointRounding.AwayFromZero);
	}

	private static int MeanHeight(SampleHistory history, double now)
	{
		int num = Math.Min(4, history.TapCount(now));
		double num2 = 0.0;
		for (int i = 0; i < num; i++)
		{
			num2 += history.Tap(now, i, SampleField.Height);
		}
		return (int)(num2 / (double)num);
	}
}
