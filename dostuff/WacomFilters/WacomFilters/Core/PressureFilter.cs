using System;

namespace WacomFilters.Core;

internal sealed class PressureFilter
{
	private struct ToolState
	{
		public int Offset;

		public bool Seen;

		public bool Ready;

		public bool Down;

		public double LastTime;

		public double Period;
	}

	public const int MaxPressure = 1023;

	private const int TipSafetyMargin = 20;

	private const int EraserSafetyMargin = 2;

	private const int LowerThreshold = 73;

	private const int UpperThreshold = 81;

	private const int CurveLowX = 81;

	private const int CurveLowY = 0;

	private const int CurveMiddleX = 511;

	private const int CurveMiddleY = 511;

	private const int CurveHighX = 1023;

	private const int CurveHighY = 1023;

	private const int CurveIntervals = 32;

	private static readonly ushort[] Curve = BuildCurve();

	private ToolState _tip = new ToolState
	{
		Offset = 20
	};

	private ToolState _eraser = new ToolState
	{
		Offset = 2
	};

	public void Observe(int pressure, double now, bool eraser)
	{
		ref ToolState reference = ref eraser ? ref _eraser : ref _tip;
		int val = (eraser ? 2 : 20);
		if (reference.Seen)
		{
			double num = now - reference.LastTime;
			if (num > Math.Max(0.25, 8.0 * reference.Period))
			{
				reference = new ToolState
				{
					Offset = reference.Offset
				};
			}
			reference.Period = num;
		}
		reference.LastTime = now;
		if (reference.Ready)
		{
			if (pressure != 0 && pressure < reference.Offset)
			{
				reference.Offset = Math.Max(pressure, val);
			}
		}
		else if (reference.Seen || pressure != 0)
		{
			reference.Ready = true;
			if (pressure < 40)
			{
				reference.Offset = Math.Max(pressure, val);
			}
		}
		reference.Seen = true;
	}

	public PressureResult Convert(int pressure, bool eraser, bool truncate = false)
	{
		ref ToolState reference = ref eraser ? ref _eraser : ref _tip;
		PressureResult result = MapPressure(pressure, reference.Offset, reference.Down, truncate);
		reference.Down = result.TipDown;
		return result;
	}

	public void Reset()
	{
		_tip = new ToolState
		{
			Offset = _tip.Offset
		};
		_eraser = new ToolState
		{
			Offset = _eraser.Offset
		};
	}

	public static PressureResult MapPressure(int raw, int offset, bool priorDown, bool truncate = false)
	{
		if (truncate)
		{
			int num = ((offset != 0 && raw > offset) ? Math.Min(1023, (int)(1023.0 * ((double)(raw - offset) / (double)(1023 - offset)))) : raw);
			bool flag = num > 81 || ((num > 73) & priorDown);
			int num2 = Math.Min(32767, (int)((double)(int)Curve[num] / 1023.0 * 32767.0));
			if (flag && num2 == 0)
			{
				num2 = 1;
			}
			return new PressureResult((ushort)num2, flag);
		}
		double num3 = ((raw < offset) ? 0.0 : Math.Min(1023.0, 1023.0 * ((double)raw - (double)offset) / (double)(1023 - offset)));
		bool flag2 = num3 > 81.0 || ((num3 > 73.0) & priorDown);
		int num4 = Curve[(int)(num3 + 0.5)] * 32767 / 1023;
		if (flag2 && num4 == 0)
		{
			num4 = 1;
		}
		return new PressureResult((ushort)num4, flag2);
	}

	public static ushort[] BuildCurve()
	{
		Span<double> span = stackalloc double[33];
		Span<double> span2 = stackalloc double[33];
		for (int i = 0; i <= 32; i++)
		{
			double num = (double)i / 32.0;
			double num2 = 1.0 - num;
			double num3 = num2 * (num2 * num2);
			double num4 = num * 3.0 * (num2 * num2);
			double num5 = num2 * 3.0 * num * num;
			double num6 = num * num * num;
			span[i] = num4 * 511.0 + 81.0 * num3 + num5 * 511.0 + 1023.0 * num6;
			span2[i] = num4 * 511.0 + 0.0 * num3 + 511.0 * num5 + 1023.0 * num6;
		}
		ushort[] array = new ushort[1024];
		int j = 1;
		double num7 = (span2[1] - span2[0]) / (span[1] - span[0]);
		for (int k = 0; k <= 1023; k++)
		{
			double val;
			if ((double)k <= span[0])
			{
				val = span2[1] - span[1] * num7 + num7 * (double)k;
			}
			else
			{
				for (; j < 32 && span[j] < (double)k; j++)
				{
				}
				double num8 = ((double)k - span[j - 1]) / (span[j] - span[j - 1]);
				val = span2[j - 1] + num8 * (span2[j] - span2[j - 1]);
			}
			array[k] = (ushort)Math.Floor(Math.Max(0.0, val) + 0.5);
		}
		return array;
	}
}
