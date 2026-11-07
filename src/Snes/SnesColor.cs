namespace Sheeter.Snes;

/// <summary>
/// Represents a little endian, 15-bit BGR color.
/// </summary>
/// <remarks>
/// <para>This type always uses a little endian layout, irrespective of platform.</para>
/// <para>This type does not validate the state of bit 15, as it is semantically irrelevant.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct SnesColor : IEquatable<SnesColor> {
	internal const int TruncatedMask = 0xF8_F8_F8;

	/// <summary>
	/// Bit mask that extracts only the red component of a 15-bit SNES color.
	/// </summary>
	public const int RedMask = 0x001F;

	/// <summary>
	/// Bit mask that extracts only the green component of a 15-bit SNES color.
	/// </summary>
	public const int GreenMask = 0x03E0;

	/// <summary>
	/// Bit mask that extracts only the blue component of a 15-bit SNES color.
	/// </summary>
	public const int BlueMask = 0x7C00;

	internal readonly ushort _value;

	/// <summary>
	/// The low 8-bits of this color.
	/// </summary>
	public byte Low {
		get => (byte) FixBigEndian(_value);
	}

	/// <summary>
	/// The high 8-bits of this color.
	/// </summary>
	public byte High {
		get => (byte) (FixBigEndian(_value) >> 8);
	}

	/// <summary>
	/// Gets the 16-bit value that represents this color in SNES CGRAM.
	/// </summary>
	public short Value {
		get => (short) FixBigEndian(_value);
	}

	/// <summary>
	/// Gets the 5-bit red component of this color.
	/// </summary>
	public byte Red {
		get => (byte) (FixBigEndian(_value) & RedMask);
	}

	/// <summary>
	/// Gets the 5-bit green component of this color.
	/// </summary>
	public byte Green {
		get => (byte) ((FixBigEndian(_value) & GreenMask) >> 5);
	}

	/// <summary>
	/// Gets the 5-bit blue component of this color.
	/// </summary>
	public byte Blue {
		get => (byte) ((FixBigEndian(_value) & BlueMask) >> 10);
	}

	/// <summary>
	/// Creates a new <see cref="SnesColor"/> using a 15-bit color value.
	/// </summary>
	/// <param name="color">15-bit color value</param>
	public SnesColor(int color) {
		_value = FixBigEndian((ushort) color);
	}

	/// <inheritdoc cref="SnesColor(int)"/>
	public SnesColor(short color) {
		_value = FixBigEndian((ushort) color);
	}

	/// <inheritdoc cref="SnesColor(int)"/>
	[CLSCompliant(false)]
	public SnesColor(ushort color) {
		_value = FixBigEndian(color);
	}

	/// <summary>
	/// Creates a <see cref="SnesColor"/> structure from the specified RGB or ARGB value.
	/// </summary>
	public static SnesColor FromRgb(int rgb) {
		return new(GetSnesColorValueFromRgb(rgb));
	}

	/// <inheritdoc cref="FromRgb(int)"/>
	[CLSCompliant(false)]
	public static SnesColor FromRgb(uint rgb) {
		return new(GetSnesColorValueFromRgb((int) rgb));
	}

	/// <inheritdoc cref="FromRgb(int)"/>
	[OverloadResolutionPriority(1)]
	public static SnesColor FromRgb(SheetColor rgb) {
		return new(GetSnesColorValueFromRgb((int) rgb));
	}

	/// <inheritdoc cref="FromRgb(int)"/>
	public static SnesColor FromRgb(byte r, byte g, byte b) {
		g &= unchecked((byte) TruncatedMask);
		b &= unchecked((byte) TruncatedMask);

		return new((r >> 3) | (g << 2) | (b << 7));
	}

	/// <summary>
	/// Returns an integer representing this color as an interpolated 24-bit RGB value.
	/// </summary>
	public int ToRgb() {
		return InterpolatePreTruncated(ToRgbTruncated());
	}

	/// <summary>
	/// Returns an integer representing this color as an interpolated 32-bit ARGB value with maximum opacity.
	/// </summary>
	public int ToArgb() {
		return ToRgb() | unchecked((int) SheetColor.MaxAlpha);
	}

	/// <summary>
	/// Returns an integer representing this color as a truncated 24-bit RGB value.
	/// </summary>
	public int ToRgbTruncated() {
		return GetRgb(FixBigEndian(_value));
	}

	/// <summary>
	/// Returns an integer representing this color as a truncated 32-bit ARGB value with maximum opacity.
	/// </summary>
	public int ToArgbTruncated() {
		return ToRgbTruncated() | unchecked((int) SheetColor.MaxAlpha);
	}

	/// <summary>
	/// Returns a <see cref="SheetColor"/> representing this color.
	/// </summary>
	public SheetColor ToSheetColor() {
		return (SheetColor) ToArgb();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj switch {
		SnesColor a => this == a,
		_ => false
	};

	/// <inheritdoc/>
	public override int GetHashCode() {
		return _value;
	}

	/// <inheritdoc/>
	public bool Equals(SnesColor other) {
		return (Value & 0x7FFF) == (other.Value & 0x7FFF);
	}

	/// <inheritdoc cref="object.ToString()"/>
	public override string ToString() {
		return $"${Value:X4}";
	}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public static bool operator ==(SnesColor a, SnesColor b) => a._value == b._value;
	public static bool operator !=(SnesColor a, SnesColor b) => a._value != b._value;

	public static explicit operator SnesColor(int a) => new(a);
	public static explicit operator SnesColor(short a) => new(a);

	public static implicit operator int(SnesColor a) => FixBigEndian(a._value);
	public static explicit operator short(SnesColor a) => (short) FixBigEndian(a._value);

	[CLSCompliant(false)] public static explicit operator SnesColor(ushort a) => new(a);
	[CLSCompliant(false)] public static implicit operator ushort(SnesColor a) => FixBigEndian(a._value);

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	internal static int GetRgb(ushort value) {
		int r = (value & RedMask) << 19;
		int g = (value & GreenMask) << 6;
		int b = (value & BlueMask) >> 7;

		return r | g | b;
	}

	/// <summary>
	/// Converts a 24-bit RGB value to a 15-bit BGR value.
	/// </summary>
	/// <param name="rgb">The color to convert.</param>
	/// <returns>The converted 15-bit value.</returns>
	public static int GetSnesColorValueFromRgb(int rgb) {
		int r = (rgb >> 19) & RedMask;
		int g = (rgb >> 6) & GreenMask;
		int b = (rgb << 7) & BlueMask;

		return r | g | b;
	}

	/// <inheritdoc cref="GetSnesColorValueFromRgb(int)"/>
	[CLSCompliant(false)]
	public static int GetSnesColorValueFromRgb(uint rgb) {
		return GetSnesColorValueFromRgb((int) rgb);
	}

	/// <inheritdoc cref="GetSnesColorValueFromRgb(int)"/>
	public static int GetSnesColorValueFromRgb(SheetColor rgb) {
		return GetSnesColorValueFromRgb((int) rgb);
	}

	/// <summary>
	/// Converts a 15-bit BGR color value to a truncated 24-bit RGB color value.
	/// </summary>
	/// <param name="color">The color to convert.</param>
	/// <returns>The converted 24-bit value.</returns>
	public static int GetRgbFromSnesColorValue(int color) {
		return GetRgb((ushort) color);
	}

	private static int InterpolatePreTruncated(int argb) {
		int tmp = argb & 0x00_E0_E0_E0;
		return argb | (tmp >> 5);
	}

	/// <summary>
	/// Interpolates an RGB color to match the interpolation used when converting 15-bit colors.
	/// </summary>
	/// <remarks>
	/// The returned value will have the same alpha component as the input.
	/// </remarks>
	/// <param name="argb">The color value to interpolate.</param>
	/// <returns>The interpolated value.</returns>
	[CLSCompliant(false)]
	public static uint Interpolate(uint argb) {
		return (uint) Interpolate((int) argb);
	}

	/// <inheritdoc cref="Interpolate(uint)"/>
	public static int Interpolate(int argb) {
		return InterpolatePreTruncated(Truncate(argb));
	}

	/// <inheritdoc cref="Interpolate(uint)"/>
	public static SheetColor Interpolate(SheetColor color) {
		return (SheetColor) Interpolate((int) color);
	}

	/// <summary>
	/// Truncates an RGB color by resetting the lower 3 bits of each component.
	/// </summary>
	/// <param name="argb">The color value to truncate.</param>
	/// <returns>The truncated value.</returns>
	/// <inheritdoc cref="Interpolate(uint)" path="//remarks"/>
	[CLSCompliant(false)]
	public static uint Truncate(uint argb) {
		return (uint) Truncate((int) argb);
	}

	/// <inheritdoc cref="Truncate(uint)"/>
	public static int Truncate(int argb) {
		return argb & (TruncatedMask | unchecked((int) SheetColor.MaxAlpha));
	}

	/// <inheritdoc cref="Truncate(uint)"/>
	public static SheetColor Truncate(SheetColor color) {
		return (SheetColor) Truncate((uint) color);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ushort FixBigEndian(ushort value) {
		return BitConverter.IsLittleEndian ? value : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(value);
	}

	/// <summary>
	/// Reinterprets a block of memory as a block of <see cref="SnesColor"/> objects.
	/// </summary>
	/// <param name="source">The data to span over.</param>
	/// <returns>A new span.</returns>
	[OverloadResolutionPriority(1)]
	public static Span<SnesColor> CreateSpanFromBytes(Span<byte> source) {
		return MemoryHelpers.CreateSpanFromBytes<SnesColor>(source);
	}

	/// <inheritdoc cref="CreateSpanFromBytes(Span{byte})" path="//summary|//param"/>
	/// <returns>A new read-only span.</returns>
	public static ReadOnlySpan<SnesColor> CreateSpanFromBytes(ReadOnlySpan<byte> source) {
		return MemoryHelpers.CreateSpanFromBytes<SnesColor>(source);
	}
}
