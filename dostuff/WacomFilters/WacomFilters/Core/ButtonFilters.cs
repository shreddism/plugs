using System;

namespace WacomFilters.Core;

internal sealed class ButtonFilters
{
	private const int ClickDistance = 71;

	private const double ClickSeconds = 0.5;

	private bool _previousTip;

	private bool _previousUpper;

	private bool _previousLower;

	private (int X, int Y) _upperPosition;

	private (int X, int Y) _lowerPosition;

	private (int X, int Y) _anchor;

	private double _anchorTime;

	private bool _hasAnchor;

	private bool _armed;

	public FilteredSample Apply(int x, int y, PressureResult pressure, PenSample input)
	{
		(int, int) tuple = (x, y);
		bool tipDown = pressure.TipDown;
		bool flag = input.UpperButton && !input.Eraser;
		bool lowerButton = input.LowerButton;
		if (_previousUpper && !flag)
		{
			tuple = _upperPosition;
		}
		_upperPosition = tuple;
		if (_previousLower && !lowerButton)
		{
			tuple = _lowerPosition;
		}
		_lowerPosition = tuple;
		tuple = AssistClick(tuple, tipDown, input.TimeSeconds);
		_previousTip = tipDown;
		_previousUpper = flag;
		_previousLower = lowerButton;
		return new FilteredSample(tuple.Item1, tuple.Item2, pressure.Pressure, tipDown, flag, lowerButton);
	}

	public void Reset()
	{
		_previousTip = (_previousUpper = (_previousLower = (_hasAnchor = (_armed = false))));
	}

	private (int X, int Y) AssistClick((int X, int Y) position, bool tip, double now)
	{
		bool flag = tip && !_previousTip;
		bool flag2 = !tip && _previousTip;
		if (!_armed)
		{
			if (flag)
			{
				_anchor = position;
				_anchorTime = now;
				_hasAnchor = true;
			}
			else if (flag2 && _hasAnchor)
			{
				_armed = true;
			}
			return position;
		}
		if (flag2 || Math.Abs(position.X - _anchor.X) > 71 || Math.Abs(position.Y - _anchor.Y) > 71 || now - _anchorTime > 0.5)
		{
			_hasAnchor = (_armed = false);
			return position;
		}
		if (!flag)
		{
			return position;
		}
		return _anchor;
	}
}
