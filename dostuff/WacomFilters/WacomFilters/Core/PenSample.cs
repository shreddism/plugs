namespace WacomFilters.Core;

internal readonly record struct PenSample(double TimeSeconds, int X, int Y, int Pressure, byte RawHeight, bool HasHeight, bool Eraser, bool UpperButton, bool LowerButton);
