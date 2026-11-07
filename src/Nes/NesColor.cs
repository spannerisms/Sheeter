namespace Sheeter.Nes;

/// <summary>
/// Represents a color in a NES PPU palette.
/// </summary>
public readonly struct NesColor {
	internal const int IndexShift = 24;

	/// <summary>
	/// This field should be index in the highest byte and color in the lowest 3,
	/// so that this type is easily bit cast to and from <see cref="SheetColor"/>.
	/// </summary>
	private readonly uint _value;

	/// <summary>
	/// Returns the PPU index represented by this instance.
	/// </summary>
	public readonly byte Index {
		get => (byte) (_value >> IndexShift);
	}

	/// <summary>
	/// Returns the color represented by this instance.
	/// </summary>
	public readonly SheetColor Color {
		get => (SheetColor) (_value | SheetColor.MaxAlpha);
	}

	/// <summary>
	/// Internal use only; for already legal values.
	/// </summary>
	internal NesColor(uint value) {
		_value = value;
	}

	/// <summary>
	/// Creates a new <see cref="NesColor"/> for the given value and color.
	/// </summary>
	/// <param name="value">The index of the color, masked to 6 bits.</param>
	/// <param name="color">The 24-bit color represented.</param>
	public NesColor(byte value, SheetColor color) {
		_value = ((uint) color & SheetColor.RgbMask) | ((uint) value & 0x3F) << IndexShift;
	}

	/// <summary>
	/// Compares two colors by their 24-bit RGB values.
	/// </summary>
	/// <param name="other">The color to compare to.</param>
	/// <returns><see langword="true"/> if both colors have the same RGB value; otherwise <see langword="false"/>.</returns>
	public bool ColorEquals(NesColor other) {
		return ((_value ^ other._value) & SheetColor.RgbMask) is 0;
	}

	/// <summary>
	/// Compares two colors by both RGB value and palette index.
	/// </summary>
	public static bool operator ==(NesColor a, NesColor b) => a._value == b._value;

	/// <inheritdoc cref="operator ==(NesColor, NesColor)"/>
	public static bool operator !=(NesColor a, NesColor b) => a._value != b._value;

	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj switch {
		NesColor a => this == a,
		_ => false
	};

	/// <inheritdoc/>
	public override int GetHashCode() {
		return (int) _value;
	}

	/// <inheritdoc cref="object.ToString()"/>
	public override string ToString() {
		return $"Index: 0x{Index:X2} | RGB: #{_value & SheetColor.RgbMask:X6}";
	}
}
