namespace Sheeter;

/// <summary>
/// Represents a tile in a tilemap.
/// </summary>
public readonly record struct TilemapTile {
	private const ushort TileNameMask = 0x3FFF;
	private const ushort PriorityMask = 0x2000;
	private const int PaletteShift = 10;
	private const byte PaletteMask = 0x7;

	private readonly ushort _value;

	/// <summary>
	/// Creates a new <see cref="TilemapTile"/> with the given value.
	/// </summary>
	/// <param name="value">The value representing the tile.</param>
	[CLSCompliant(false)]
	public TilemapTile(ushort value) {
		_value = value;
	}

	/// <inheritdoc cref="TilemapTile(ushort)"/>
	public TilemapTile(short value) : this((ushort) value) { }

	/// <summary>
	/// Creates a new <see cref="TilemapTile"/> with the given properties.
	/// </summary>
	/// <param name="name">The 10-bit tile name.</param>
	/// <param name="palette">The palette of the tile.</param>
	/// <param name="priority">Whether the tile is high priority.</param>
	/// <param name="flip">The reflection flags of the tile.</param>
	public TilemapTile(int name, byte palette, bool priority, TileFlip flip) {
		_value = (ushort) (
			(name & TileNameMask) |
			((palette & PaletteMask) << PaletteShift) |
			(int) (flip & TileFlip.VHFlip) |
			(priority ? PriorityMask : 0));
	}

	/// <inheritdoc cref="TilemapTile(int, byte, bool, TileFlip)"/>
	public TilemapTile(int name, byte palette) {
		_value = (ushort) (
			(name & TileNameMask) |
			((palette & PaletteMask) << PaletteShift));
	}

	/// <inheritdoc cref="TilemapTile(int, byte, bool, TileFlip)"/>
	public TilemapTile(int name, byte palette, bool priority) {
		_value = (ushort) (
			(name & TileNameMask) |
			((palette & PaletteMask) << PaletteShift) |
			(priority ? PriorityMask : 0));
	}

	/// <inheritdoc cref="TilemapTile(int, byte, bool, TileFlip)"/>
	public TilemapTile(int name, byte palette, TileFlip flip) {
		_value = (ushort) (
			(name & TileNameMask) |
			((palette & PaletteMask) << PaletteShift) |
			(int) (flip & TileFlip.VHFlip));
	}

	/// <summary>
	/// Gets the name of this tile.
	/// </summary>
	public int Name {
		get => _value & TileNameMask;

		init {
			_value &= unchecked((ushort) ~TileNameMask);
			_value |= (ushort) (value & TileNameMask);
		}
	}

	/// <summary>
	/// Gets the flip of this tile.
	/// </summary>
	public TileFlip Flip {
		get => (TileFlip) _value & TileFlip.VHFlip;

		init {
			_value &= (ushort) TileFlip.VHFlip;
			_value |= (ushort) value;
		}
	}

	/// <summary>
	/// Gets the priority of this tile.
	/// </summary>
	public bool Priority {
		get => (_value & PriorityMask) is not 0;

		init {
			if (value) {
				_value |= PriorityMask;
			} else {
				_value &= PriorityMask;
			}
		}
	}

	/// <summary>
	/// Gets the palette of this tile.
	/// </summary>
	public byte Palette {
		get => (byte) ((_value >> PaletteShift) & PaletteMask);

		init {
			_value &= PaletteMask << PaletteShift;
			_value |= (ushort) ((value & PaletteMask) << PaletteShift);
		}
	}
}
