namespace Sheeter.Snes;

/// <summary>
/// Represents a tile encoded in direct color for an 8-bits-per-pixel background.
/// </summary>
public sealed class DirectColorTile {
	private const int Length = Snes8bppTile.Length;

	/// <summary>
	/// Contains the high bits of the color data.
	/// </summary>
	private readonly DirectColorTileData _tile;

	/// <summary>
	/// Gets the palette bitfield component of the high byte required to achieve full color with this tile.
	/// <para>
	/// For SNES 8bpp, this value represents (<c>...bgr..</c>) where:
	/// <list type="bullet">
	/// <item>bit 2 of the field holds bit 1 of red</item>
	/// <item>bit 3 of the field holds bit 1 of green</item>
	/// <item>bit 4 of the field holds bit 2 of blue</item>
	/// </list>
	/// The remaining bits hold the flip, priority, and tile name high bits
	/// present in regular tilemap entries.
	/// </para>
	/// <para>
	/// For Mode 7, this value should be ignored, as it cannot be used.
	/// </para>
	/// </summary>
	public byte PaletteField { get; }

	/// <summary>
	/// Creates a new <see cref="DirectColorTile"/> from the given tile.
	/// </summary>
	/// <param name="tile">The tile to convert to direct color.</param>
	public DirectColorTile(scoped TileViewport tile) {
		int dataIndex = 0;

		int avgR = 0; // average of low bits for each color
		int avgG = 0; // we'll use this average to determine the
		int avgB = 0; // low bits present in PaletteField

		foreach (SheetColor color in tile) {
			int tempSnes = SnesColor.GetSnesColorValueFromRgb(color);

			byte pixelVal = (byte) ((tempSnes & 0x001C) >> 2); // red
			avgR += (tempSnes & 0x0003);

			pixelVal |= (byte) ((tempSnes & 0x0380) >> 4); // green
			avgG += (tempSnes & 0x0060) >> 5;

			// some extra rounding only for blue, since it has less depth
			int blueHigh = (tempSnes & 0x6000) >> 7;
			int blueLow = (tempSnes & 0x1C00) >> 10;
			avgB += blueLow;

			if (blueLow is 0b111) {
				if (blueHigh < 0xC0) {
					blueHigh += 0x40;
				}
			}

			pixelVal |= (byte) blueHigh;

			_tile[dataIndex++] = pixelVal;
		}

		avgR /= Length;
		avgG /= Length;
		avgB /= Length;

		avgR &= 0b_10;
		avgG &= 0b_10;
		avgB &= 0b100;

		PaletteField = (byte) ((avgR << 1) | (avgG << 2) | (avgB << 2));
	}

	/// <summary>
	/// Copies the color data in this instance to an 8-bits-per-pixel tile.
	/// </summary>
	/// <param name="destination">A managed pointer to the destination data.</param>
	public void CopyTo(scoped ref Snes8bppTile destination) {
		Unsafe.WriteUnaligned(ref destination._tileData, _tile);
		GC.KeepAlive(this);
	}

	/// <inheritdoc cref="CopyTo(ref Snes8bppTile)"/>
	public void CopyTo(scoped ref SnesMode7Tile destination) {
		Unsafe.WriteUnaligned(ref destination._tileData, _tile);
		GC.KeepAlive(this);
	}

	/// <inheritdoc cref="ITileData{TSelf}.AsSpan"/>
	/// <remarks>
	/// This span does not include the palette byte.
	/// </remarks>
	public ReadOnlySpan<byte> AsSpan() {
		return _tile;
	}

	[InlineArray(Length)]
	private struct DirectColorTileData {
		public byte _pixels;
	}
}
