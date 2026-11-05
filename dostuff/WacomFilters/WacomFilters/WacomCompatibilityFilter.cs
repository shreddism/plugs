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
using WacomFilters.Core;

namespace WacomFilters;

[PluginName("wacom smoothing")]
public sealed class WacomCompatibilityFilter : IPositionedPipelineElement<IDeviceReport>, IPipelineElement<IDeviceReport>, IDisposable
{
	private sealed class ReleaseReport(byte[] raw, Vector2 position, bool eraser, uint hoverDistance, int buttonCount) : ITabletReport, IAbsolutePositionReport, IDeviceReport, IEraserReport, IProximityReport
	{
		public byte[] Raw { get; set; } = raw;

		public Vector2 Position { get; set; } = position;

		public uint Pressure { get; set; }

		public bool[] PenButtons { get; set; } = new bool[buttonCount];

		public bool Eraser { get; set; } = eraser;

		public bool NearProximity { get; set; }

		public uint HoverDistance { get; set; } = hoverDistance;
	}

	private readonly object _gate = new object();

	private WacomPreset _preset = WacomPreset.Driver6310;

	private bool _lowLatency;

	private WacomProcessor? _processor;

	private IOutputMode? _outputMode;

	private float _maxX;

	private float _maxY;

	private uint _maxPressure;

	private bool _disposed;

	private bool _accepted;

	private bool _held;

	private byte[] _lastRaw = Array.Empty<byte>();

	private Vector2 _lastPosition;

	private bool _lastEraser;

	private uint _lastHoverDistance;

	private int _lastButtonCount;

	public PipelinePosition Position => (PipelinePosition)1;

	public static IEnumerable<string> Presets { get; } = new _003C_003Ez__ReadOnlyArray<string>(new string[4] { "6.3.9w5", "6.3.10w2", "6.4.15-1", "6.4.15-1 + k-filter" });

	[Property("driver version")]
	[PropertyValidated("Presets")]
	[DefaultPropertyValue("6.3.10w2")]
	[ToolTip("6.3.9w5 uses prediction, 6.3.10w2 and 6.4.15-1 average recent movement, and 6.4.15-1 + k-filter uses the probabilistic K-Filter")]
	public string Preset
	{
		get
		{
			return _preset switch
			{
				WacomPreset.Driver639 => "6.3.9w5", 
				WacomPreset.Driver6310 => "6.3.10w2", 
				WacomPreset.Driver6415 => "6.4.15-1", 
				_ => "6.4.15-1 + k-filter", 
			};
		}
		set
		{
			_preset = value switch
			{
				"6.3.9w5" => WacomPreset.Driver639, 
				"6.3.10w2" => WacomPreset.Driver6310, 
				"6.4.15-1" => WacomPreset.Driver6415, 
				"6.4.15-1 + k-filter" => WacomPreset.Driver6415KFilter, 
				_ => throw new ArgumentException("invalid driver version.", "value"), 
			};
			_processor = null;
		}
	}

	[BooleanProperty("low latency", "")]
	[DefaultPropertyValue(false)]
	[ToolTip("Keeps smoothing at the lowest latency without 133 Hz rate scaling or pressure delay")]
	public bool LowLatency
	{
		get
		{
			return _lowLatency;
		}
		set
		{
			_lowLatency = value;
			_processor = null;
		}
	}

	[Resolved]
	public IDriver? Driver { get; set; }

	[TabletReference]
	public TabletReference Tablet
	{
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			TabletSpecifications specifications = value.Properties.Specifications;
			DigitizerSpecifications val = ((specifications != null) ? specifications.Digitizer : null);
			TabletSpecifications specifications2 = value.Properties.Specifications;
			PenSpecifications val2 = ((specifications2 != null) ? specifications2.Pen : null);
			if (val != null)
			{
				float maxX = val.MaxX;
				if (maxX > 0f && maxX < float.PositiveInfinity)
				{
					float maxY = val.MaxY;
					if (maxY > 0f && maxY < float.PositiveInfinity && val2 != null && val2.MaxPressure != 0)
					{
						_maxX = val.MaxX;
						_maxY = val.MaxY;
						_maxPressure = val2.MaxPressure;
						_processor = null;
						return;
					}
				}
			}
			throw new ArgumentException("invalid coords/pressure range.", "value");
		}
	}

	public event Action<IDeviceReport>? Emit;

	public void Consume(IDeviceReport report)
	{
		ConsumeAt(report, (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
	}

	internal void ConsumeAt(IDeviceReport report, double now)
	{
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}
			try
			{
				if (report is OutOfRangeReport)
				{
					_processor?.Reset();
					if (_accepted)
					{
						Release(report.Raw);
					}
				}
				if (_maxX != 0f)
				{
					ITabletReport val = (ITabletReport)(object)((report is ITabletReport) ? report : null);
					if (val != null)
					{
						Vector2 position = ((IAbsolutePositionReport)val).Position;
						uint pressure = val.Pressure;
						if (!IsFinite(position) || position.X < 0f || position.X > _maxX || position.Y < 0f || position.Y > _maxY || pressure > _maxPressure)
						{
							throw new ArgumentException("invalid pen report.", "report");
						}
						IEraserReport val2 = (IEraserReport)(object)((report is IEraserReport) ? report : null);
						bool flag = val2 != null && val2.Eraser;
						if (_held && flag != _lastEraser)
						{
							Release(report.Raw);
						}
						if (_outputMode == null)
						{
							IDriver? driver = Driver;
							Driver val3 = (Driver)(object)((driver is Driver) ? driver : null);
							if (val3 != null)
							{
								_outputMode = val3.InputDevices.Select((InputDeviceTree tree) => tree.OutputMode).FirstOrDefault((IOutputMode mode) => mode != null && mode.Elements?.Contains((IPositionedPipelineElement<IDeviceReport>)(object)this) == true) ?? throw new InvalidOperationException("invalid output mode.");
							}
						}
						bool[] penButtons = val.PenButtons;
						IProximityReport val4 = (IProximityReport)(object)((report is IProximityReport) ? report : null);
						uint lastHoverDistance = ((val4 != null) ? val4.HoverDistance : 0u);
						if (_processor == null)
						{
							_processor = new WacomProcessor(_preset, _lowLatency);
						}
						FilteredSample filteredSample = _processor.Process(new PenSample(now, (int)Math.Round((double)position.X * (15200.0 / (double)_maxX), MidpointRounding.AwayFromZero), (int)Math.Round((double)position.Y * (9500.0 / (double)_maxY), MidpointRounding.AwayFromZero), (int)((ulong)((long)pressure * 1023L + _maxPressure / 2) / (ulong)_maxPressure), 0, HasHeight: false, flag, penButtons.Length > 1 && penButtons[1], penButtons.Length != 0 && penButtons[0]));
						((IAbsolutePositionReport)val).Position = new Vector2((float)((double)filteredSample.X * ((double)_maxX / 15200.0)), (float)((double)filteredSample.Y * ((double)_maxY / 9500.0)));
						val.Pressure = (filteredSample.TipDown ? Math.Max(1u, (uint)((ulong)((long)filteredSample.Pressure * (long)_maxPressure + 16383) / 32767uL)) : 0u);
						if (penButtons.Length != 0)
						{
							penButtons[0] = filteredSample.LowerButton;
						}
						if (penButtons.Length > 1)
						{
							penButtons[1] = filteredSample.UpperButton;
						}
						if (!ReachesOutput(((IAbsolutePositionReport)val).Position))
						{
							if (_held)
							{
								Release(report.Raw);
							}
							Emit?.Invoke(report);
							return;
						}
						_accepted = true;
						_held = filteredSample.TipDown || Enumerable.Contains(penButtons, value: true);
						_lastRaw = report.Raw;
						_lastPosition = ((IAbsolutePositionReport)val).Position;
						_lastEraser = flag;
						_lastHoverDistance = lastHoverDistance;
						_lastButtonCount = penButtons.Length;
						Emit?.Invoke(report);
						return;
					}
				}
				Emit?.Invoke(report);
			}
			catch
			{
				_processor?.Reset();
				if (_held)
				{
					Release(_lastRaw);
				}
				_accepted = false;
				throw;
			}
		}
	}

	private bool ReachesOutput(Vector2 position)
	{
		IOutputMode? outputMode = _outputMode;
		AbsoluteOutputMode val = (AbsoluteOutputMode)(object)((outputMode is AbsoluteOutputMode) ? outputMode : null);
		if (val == null)
		{
			return true;
		}
		Vector2 vector = Vector2.Transform(position, ((OutputMode)val).TransformationMatrix);
		if (!IsFinite(vector))
		{
			return false;
		}
		if (!val.AreaLimiting && !val.AreaClipping)
		{
			return true;
		}
		Area output = val.Output;
		if (output == null || !(output.Width >= 0f) || !(output.Height >= 0f))
		{
			return false;
		}
		Vector2 vector2 = new Vector2(output.Position.X - output.Width / 2f, output.Position.Y - output.Height / 2f);
		Vector2 vector3 = new Vector2(output.Position.X + output.Width - output.Width / 2f, output.Position.Y + output.Height - output.Height / 2f);
		if (!IsFinite(vector2) || !IsFinite(vector3))
		{
			return false;
		}
		if (val.AreaLimiting)
		{
			return Vector2.Clamp(vector, vector2, vector3) == vector;
		}
		return true;
	}

	private static bool IsFinite(Vector2 value)
	{
		if (float.IsFinite(value.X))
		{
			return float.IsFinite(value.Y);
		}
		return false;
	}

	private void Release(byte[] raw)
	{
		_accepted = false;
		_held = false;
		Emit?.Invoke((IDeviceReport)(object)new ReleaseReport(raw, _lastPosition, _lastEraser, _lastHoverDistance, _lastButtonCount));
	}

	public void Dispose()
	{
		lock (_gate)
		{
			_disposed = true;
			if (_held)
			{
				Release(_lastRaw);
			}
		}
	}
}
