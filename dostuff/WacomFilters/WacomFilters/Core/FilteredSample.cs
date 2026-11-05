namespace WacomFilters.Core;

internal readonly record struct FilteredSample(int X, int Y, ushort Pressure, bool TipDown, bool UpperButton, bool LowerButton);
