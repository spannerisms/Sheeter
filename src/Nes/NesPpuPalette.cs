namespace Sheeter.Nes;

/// <summary>
/// Represents a 64-color NES PPU palette.
/// </summary>
public sealed class NesPpuPalette {
	/// <summary>
	/// The number of colors in a single PPU palette.
	/// </summary>
	public const int Length = 64;
	internal const int ByteLength = Length * 4;

	private readonly PaletteColors _colorPalette;

	/// <summary>
	/// Returns the color at <paramref name="index"/>.
	/// </summary>
	public NesColor this[int index] {
		get => _colorPalette[index];
	}

	/// <summary>
	/// Creates a new <see cref="NesPpuPalette"/> with the specified colors.
	/// </summary>
	/// <param name="colors">The colors to use.</param>
	/// <exception cref="ArgumentException">If the input is not 64 elements in length.</exception>
	public NesPpuPalette(scoped ReadOnlySpan<SheetColor> colors) {
		if (colors.Length is not Length) {
			ErrorHelpers.ThrowBadArgument("Input data must be exactly 64 elements in length.");
		}

		uint index = 0; // palette index that goes in byte 3

		for (int i = 0; i < Length; i++) {
			_colorPalette[i] = new(index | ((uint) colors[i] & SheetColor.RgbMask));
			index += 1 << NesColor.IndexShift;
		}
	}

	/// <inheritdoc cref="NesPpuPalette(ReadOnlySpan{SheetColor})"/>
	public NesPpuPalette(scoped ReadOnlySpan<NesColor> colors) : this(MemoryMarshal.Cast<NesColor, SheetColor>(colors)) {
		unsafe {
			Debug.Assert(sizeof(NesColor) == sizeof(SheetColor));
		}
	}

	/// <summary>
	/// Internal use only. For generating palettes with pre-compiled binary data.
	/// </summary>
	private NesPpuPalette(scoped ReadOnlySpan<uint> cols) {
		cols.CopyTo(MemoryMarshal.Cast<NesColor, uint>((Span<NesColor>) _colorPalette));
	}

	/// <summary>
	/// Returns a copy of the colors in this palette.
	/// </summary>
	/// <returns>A new array with 64 colors.</returns>
	public SheetColor[] GetPaletteColors() {
		SheetColor[] returnPalette = new SheetColor[Length];

		for (int i = 0; i < Length; i++) {
			returnPalette[i] = _colorPalette[i].Color;
		}

		return returnPalette;
	}

	/// <summary>
	/// Standard palette for the 2C02 PPU.
	/// </summary>
	public static readonly NesPpuPalette Palette2C02 = new(Colors2C02);

	// These colors were lifted from https://www.nesdev.org/wiki/PPU_palettes
	// Everything posted on this website is considered public domain
	private static ReadOnlySpan<uint> Colors2C04 => [
		0x00_6D6D6D, 0x01_002491, 0x02_0000DA, 0x03_6D48DA, 0x04_91006D, 0x05_B6006D, 0x06_B62400, 0x07_914800,
		0x08_6D4800, 0x09_244800, 0x0A_006D24, 0x0B_009100, 0x0C_004848, 0x0D_242424, 0x0E_00006D, 0x0F_004800,
		0x10_B6B6B6, 0x11_006DDA, 0x12_0048FF, 0x13_9100FF, 0x14_B600FF, 0x15_FF0091, 0x16_FF0000, 0x17_DA6D00,
		0x18_916D00, 0x19_249100, 0x1A_009100, 0x1B_00B66D, 0x1C_009191, 0x1D_484848, 0x1E_480000, 0x1F_6D2400,
		0x20_FFFFFF, 0x21_6DB6FF, 0x22_9191FF, 0x23_DA6DFF, 0x24_FF00FF, 0x25_FF6DFF, 0x26_FF9100, 0x27_FFB600,
		0x28_DADA00, 0x29_6DDA00, 0x2A_00FF00, 0x2B_48FFDA, 0x2C_00FFFF, 0x2D_919191, 0x2E_000000, 0x2F_000000,
		0x30_FFFFFF, 0x31_B6DAFF, 0x32_DAB6FF, 0x33_FFB6FF, 0x34_FF91FF, 0x35_FFB6B6, 0x36_FFDA91, 0x37_FFFF00,
		0x38_FFFF6D, 0x39_B6FF48, 0x3A_91FF6D, 0x3B_48FFDA, 0x3C_91DAFF, 0x3D_DADADA, 0x3E_DAB66D, 0x3F_FFDA00
	];

	private static ReadOnlySpan<uint> Colors2C02 => [
		0x00_575757, 0x01_000C8E, 0x02_0800A6, 0x03_340096, 0x04_550061, 0x05_630015, 0x06_5A0000, 0x07_3C0E00,
		0x08_112800, 0x09_003B00, 0x0A_004200, 0x0B_003A05, 0x0C_002652, 0x0D_000000, 0x0E_000000, 0x0F_000000,
		0x10_A5A5A5, 0x11_0041D9, 0x12_2F1EFF, 0x13_6704F2, 0x14_9400B4, 0x15_AA0057, 0x16_A31800, 0x17_803900,
		0x18_4B5B00, 0x19_137600, 0x1A_008100, 0x1B_007923, 0x1C_006288, 0x1D_000000, 0x1E_000000, 0x1F_000000,
		0x20_FFFFFF, 0x21_4A9FFF, 0x22_797EFF, 0x23_AF63FF, 0x24_DD55FF, 0x25_F757C2, 0x26_F76A63, 0x27_DC8810,
		0x28_AEA900, 0x29_78C400, 0x2A_4AD211, 0x2B_2FCF64, 0x2C_2FBDC4, 0x2D_414141, 0x2E_000000, 0x2F_000000,
		0x30_FFFFFF, 0x31_B9DDFF, 0x32_CAD1FF, 0x33_DEC6FF, 0x34_F0C0FF, 0x35_FCC0EE, 0x36_FDC6CA, 0x37_F5D0AA,
		0x38_E4DD95, 0x39_D0E892, 0x3A_BDEEA2, 0x3B_B2EEC0, 0x3C_B0E8E3, 0x3D_B3B3B3, 0x3E_000000, 0x3F_000000
	];

	[InlineArray(Length)]
	private struct PaletteColors {
		internal NesColor _colors;
	}
}
