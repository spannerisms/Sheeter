namespace Sheeter;

// This type more or less acts as a replacement for SKColor
// so that the type can remain an implementation detail

/// <summary>
/// Represents a 32-bit ARGB color.
/// </summary>
[DebuggerDisplay("ARGB: {_value:X8}")]
public readonly struct SheetColor : IEquatable<SheetColor> {
	internal const uint MaxAlpha = 0xFF_00_00_00;
	internal const uint RgbMask = 0x00_FF_FF_FF;

	private readonly uint _value;

	/// <summary>
	/// Creates a new <see cref="SheetColor"/> with the given ARGB value.
	/// </summary>
	[CLSCompliant(false)]
	public SheetColor(uint argb) {
		_value = argb;
	}

	/// <inheritdoc cref="SheetColor(uint)"/>
	public SheetColor(int argb) {
		_value = (uint) argb;
	}

	/// <summary>
	/// Creates a new <see cref="SheetColor"/> with the given values.
	/// </summary>
	/// <param name="r">The red value.</param>
	/// <param name="g">The green value.</param>
	/// <param name="b">The blue value.</param>
	/// <param name="a">The alpha value.</param>
	public SheetColor(byte r, byte g, byte b, byte a) {
		_value = (uint) (b | (g << 8) | (r << 16) | (a << 24));
	}

	/// <summary>
	/// Creates a new <see cref="SheetColor"/> with the given values
	/// and an alpha of 255.
	/// </summary>
	/// <inheritdoc cref="SheetColor(byte,byte,byte,byte)"/>
	public SheetColor(byte r, byte g, byte b) {
		_value = (uint) (b | (g << 8) | (r << 16) | MaxAlpha);
	}

	/// <summary>
	/// Creates a new <see cref="SheetColor"/> with the given RGB value
	/// and an alpha level of 255.
	/// </summary>
	/// <param name="rgb">The 24-bit color. The alpha byte will be ignored.</param>
	/// <returns>A new color.</returns>
	public static SheetColor FromRgb(int rgb) {
		return new((uint) rgb | MaxAlpha);
	}

	/// <inheritdoc cref="FromRgb(int)"/>
	[CLSCompliant(false)]
	public static SheetColor FromRgb(uint rgb) {
		return new(rgb | MaxAlpha);
	}

	/// <summary>
	/// Gets the alpha component of this color.
	/// </summary>
	public byte Alpha {
		get => (byte) (_value >> 24);
	}

	/// <summary>
	/// Gets the red component of this color.
	/// </summary>
	public byte Red {
		get => (byte) (_value >> 16);
	}

	/// <summary>
	/// Gets the green component of this color.
	/// </summary>
	public byte Green {
		get => (byte) (_value >> 8);
	}

	/// <summary>
	/// Gets the blue component of this color.
	/// </summary>
	public byte Blue {
		get => (byte) _value;
	}

	/// <summary>
	/// Tests this color for opaqueness against an alpha threshold.
	/// </summary>
	/// <param name="threshold">The minimum alpha level to be considered opaque.</param>
	/// <returns>
	/// <see langword="true"/> if this color's alpha component is greater than or equal to <paramref name="threshold"/>;
	/// otherwise <see langword="false"/>.
	/// </returns>
	public bool IsOpaque(byte threshold) {
		return _value >= ((uint) threshold << 24);
	}

	/// <summary>
	/// Compares two colors for having identical red, green, and blue components.
	/// </summary>
	/// <param name="other">The color to compare to.</param>
	/// <returns><see langword="true"/> if the colors are the same; otherwise, <see langword="false"/>.</returns>
	public bool EqualsIgnoringTransparency(SheetColor other) {
		return (_value & RgbMask) == (other._value & RgbMask);
	}

	/// <summary>
	/// Compares two colors for having identical red, green, and blue components when truncated to 5-bit components.
	/// </summary>
	/// <param name="other">The color to compare to.</param>
	/// <returns><see langword="true"/> if the colors are the same when truncated; otherwise, <see langword="false"/>.</returns>
	public bool EqualsTruncated(SheetColor other) {
		return (_value & Snes.SnesColor.TruncatedMask) == (other._value & Snes.SnesColor.TruncatedMask);
	}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public static bool operator ==(SheetColor a, SheetColor b) => a._value == b._value;
	public static bool operator !=(SheetColor a, SheetColor b) => a._value != b._value;

	public static explicit operator SheetColor(int a) => Unsafe.BitCast<int, SheetColor>(a);
	public static explicit operator int(SheetColor a) => (int) a._value;

	[CLSCompliant(false)] public static explicit operator SheetColor(uint a) => Unsafe.BitCast<uint, SheetColor>(a);
	[CLSCompliant(false)] public static implicit operator uint(SheetColor a) => a._value;

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj switch {
		SheetColor c => this == c,
		_ => false,
	};

	/// <summary>
	/// Indicates whether this instance and a specified <see cref="SheetColor"/> are equal.
	/// </summary>
	public bool Equals(SheetColor obj) {
		return _value == obj._value;
	}

	/// <inheritdoc/>
	public override int GetHashCode() {
		return (int) _value;
	}

	/// <inheritdoc cref="object.ToString()"/>
	public override string ToString() {
		return $"ARGB: #{_value:X4}";
	}
}
