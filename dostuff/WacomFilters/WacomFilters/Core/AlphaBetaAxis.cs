using System;

namespace WacomFilters.Core;

internal sealed class AlphaBetaAxis
{
	private const double AlphaLow = 0.2;

	private const double AlphaHigh = 0.8;

	private const double BetaLow = 0.02;

	private const double BetaHigh = 0.4;

	private const double SpeedLow = 0.2;

	private const double SpeedHigh = 3.0;

	private double _position;

	private double _velocity;

	private bool _initialized;

	public int Update(int raw, double step)
	{
		if (!_initialized)
		{
			_position = raw;
			_velocity = 0.0;
			_initialized = true;
			return raw;
		}
		double num = Math.Abs(_velocity);
		double num2;
		double num3;
		if (num <= 0.2)
		{
			num2 = 0.2;
			num3 = 0.02;
		}
		else if (num >= 3.0)
		{
			num2 = 0.8;
			num3 = 0.4;
		}
		else
		{
			num2 = 0.2 + (num - 0.2) * 0.6000000000000001 / 2.8;
			num3 = 0.02 + (num - 0.2) * 0.38 / 2.8;
		}
		if (step == 1.0)
		{
			double num4 = (double)raw - (_position + _velocity);
			_position = num2 * num4 + _velocity + _position;
			_velocity = num3 * num4 + _velocity;
		}
		else
		{
			Advance(raw, num2, num3, step);
		}
		double num5 = _position + _velocity;
		return (int)((num5 > 0.0) ? Math.Floor(num5 + 0.5) : Math.Ceiling(num5 - 0.5));
	}

	public void Reset()
	{
		_initialized = false;
	}

	private void Advance(int raw, double alpha, double beta, double step)
	{
		double num = _position - (double)raw;
		double num2 = (alpha + beta) / 2.0;
		double x = 1.0 - num2;
		double num3 = num2 * num2 - beta;
		double num4 = (beta - alpha) / 2.0;
		double num5 = Math.Sqrt(0.0 - num3);
		double num6 = Math.Atan2(num5, x);
		double num7 = Math.Exp(step * (0.5 * LogOnePlus(0.0 - alpha)));
		double num8 = step * num6;
		double num9 = num7 * Math.Cos(num8);
		double num10 = ((num8 < 0.0001) ? (num7 * step * (num6 / num5) * Sinc(num8)) : (num7 * (Math.Sin(num8) / num5)));
		_position = (double)raw + num9 * num + num10 * (num4 * num + (1.0 - alpha) * _velocity);
		_velocity = num9 * _velocity + num10 * ((0.0 - beta) * num - num4 * _velocity);
	}

	private static double LogOnePlus(double value)
	{
		double num = 1.0 + value;
		return Math.Log(num) * (value / (num - 1.0));
	}

	private static double Sinc(double value)
	{
		double num = value * value;
		return 1.0 - num / 6.0 + num * num / 120.0;
	}
}
