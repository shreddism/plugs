using System;

namespace WacomFilters.Core;

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
