namespace Sheeter.Snes;

/* Not sure I want this class yet.

/// <summary>
/// Represents a 256-color palette on the SNES.
/// </summary>
public sealed class SnesCGRam {
	const int Length = 256;
	private PaletteData _values = default;
	private PaletteColors _colors = default;

	/// <summary>
	/// Creates a new instance of the <see cref="SnesCGRam"/> class.
	/// </summary>
	public SnesCGRam() {

	}

	/// <summary>
	/// Returns a span of the SNES color values in this palette.
	/// </summary>
	public ReadOnlySpan<SnesColor> Values => _values;

	/// <summary>
	/// Returns a span of the ARGB color values in this palette.
	/// </summary>
	public ReadOnlySpan<SheetColor> Colors => _colors;

	/// <summary>
	/// Resets every color in this palette to black (0000).
	/// </summary>
	public void Clear() {
		Unsafe.InitBlockUnaligned(ref Unsafe.As<PaletteData, byte>(ref _values), 0, (uint) Unsafe.SizeOf<PaletteData>());
		Unsafe.InitBlockUnaligned(ref Unsafe.As<PaletteColors, byte>(ref _colors), 0, (uint) Unsafe.SizeOf<PaletteColors>());
	}

	/// <summary>
	/// Gets the color at <paramref name="index"/> as both a <see cref="SnesColor"/> and a <see cref="SheetColor"/>.
	/// </summary>
	/// <inheritdoc cref="SetColor(int, SnesColor)" path="//param"/>
	/// <returns>A tuple containing the value-color pair.</returns>
	public (SnesColor snes, SheetColor rgb) GetColorAndValue(int index) {
		ValidateIndex(index);

		return (UnsafeGetSnesColor(index), UnsafeGetSheetColor(index));
	}

	/// <summary>
	/// Gets the color at <paramref name="index"/> as a <see cref="SheetColor"/>.
	/// </summary>
	/// <inheritdoc cref="SetColor(int, SnesColor)" path="//param"/>
	/// <returns>A tuple containing the value-color pair.</returns>
	public SheetColor GetColor(int index) {
		ValidateIndex(index);

		return UnsafeGetSheetColor(index);
	}

	/// <summary>
	/// Gets the color at <paramref name="index"/> as a <see cref="SnesColor"/>.
	/// </summary>
	/// <inheritdoc cref="SetColor(int, SnesColor)" path="//param"/>
	/// <returns>A struct containing the color value.</returns>
	public SnesColor GetValue(int index) {
		ValidateIndex(index);

		return UnsafeGetSnesColor(index);
	}

	/// <summary>
	/// Sets the color at <paramref name="index"/> to <paramref name="color"/>.
	/// </summary>
	/// <param name="index">The index to set [0..255].</param>
	/// <param name="color">The color to add.</param>
	public void SetColor(int index, SnesColor color) {
		ValidateIndex(index);

		UnsafeSetSnesColor(index, color);
		UnsafeSetSheetColor(index, color.ToSheetColor());
	}

	/// <inheritdoc cref="SetColor(int, SnesColor)"/>
	public void SetColor(int index, SheetColor color) {
		ValidateIndex(index);

		UnsafeSetSnesColor(index, SnesColor.FromRgb(color));
		UnsafeSetSheetColor(index, color);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ValidateIndex(int index) {
		if ((uint) index >= Length) {
			ErrorHelpers.ThrowOutOfRangeArgument("Palette index must be a non-negative number less than 256.");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UnsafeSetSnesColor(int index, SnesColor color) {
		Unsafe.Add(ref Unsafe.As<PaletteData, SnesColor>(ref _values), index) = color;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UnsafeSetSheetColor(int index, SheetColor color) {
		Unsafe.Add(ref Unsafe.As<PaletteColors, SheetColor>(ref _colors), index) = color;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref SnesColor UnsafeGetSnesColor(int index) {
		return ref Unsafe.Add(ref Unsafe.As<PaletteData, SnesColor>(ref _values), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref SheetColor UnsafeGetSheetColor(int index) {
		return ref Unsafe.Add(ref Unsafe.As<PaletteColors, SheetColor>(ref _colors), index) ;
	}


	[InlineArray(Length)]
	private struct PaletteData {
		public SnesColor Colors;
	}

	[InlineArray(Length)]
	private struct PaletteColors {
		public SheetColor Colors;
	}

	[Flags]
	private enum TransparencyWhen : byte {
		Never = 0,
		Always = 1,
	}
}
*/
