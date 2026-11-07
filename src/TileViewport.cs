namespace Sheeter;

/// <summary>
/// Provides a view over the pixels in an 8x8 region of an image.
/// </summary>
public readonly ref struct TileViewport {
	internal const int TileDimension = IndexedTile.TileDimension;
	private readonly ref SheetColor _reference;
	private readonly int _pixelsPerRow;

	/// <summary>
	/// Creates a new viewport over the tile at the given coordinates of the image.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the viewport escapes the bounds of the source.</exception>
	public TileViewport(SheeterBitmap image, int x, int y) {
		ArgumentNullException.ThrowIfNull(image);
		ErrorHelpers.ThrowIfBadViewPort(image.Width, image.Height, x, y, TileDimension, TileDimension);

		int pixelsPerRow = image.PixelsPerRow;

		_pixelsPerRow = pixelsPerRow;
		_reference = ref MemoryHelpers.GetPixelRef(ref image.GetPixelsReference(), x, y, pixelsPerRow);
	}

	/// <summary>
	/// Creates a new viewport over the tile at the relative coordinates inside the image viewport.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the viewport escapes the bounds of the source.</exception>
	public TileViewport(BitmapViewport image, int x, int y) {
		ErrorHelpers.ThrowIfBadViewPort(image.Width, image.Height, x, y, TileDimension, TileDimension);

		int ppr = image._pixelsPerRow;
		_pixelsPerRow = ppr;
		_reference = ref MemoryHelpers.GetPixelRef(ref image._reference, x, y, ppr);
	}

	internal TileViewport(ref SheetColor pointer, int x, int y, int ppr) {
		_pixelsPerRow = ppr;
		_reference = ref MemoryHelpers.GetPixelRef(ref pointer, x, y, ppr);
	}

	/// <summary>
	/// Gets the color at the specified index inside the tile.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the selected pixel is outside of the viewport.</exception>
	public ref SheetColor this[int i] {
		get {
			if ((uint) i >= IndexedTile.Length) {
				ErrorHelpers.ThrowOutOfRangeArgument(ErrorHelpers.PixelOutOfRange);
			}
			return ref UnsafeGetPixel(i & 0x7, i >> 3);
		}
	}

	/// <summary>
	/// Gets the color at the relative coordinates inside the tile.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the selected pixel is outside of the viewport.</exception>
	public ref SheetColor this[int x, int y] {
		get {
			if ((uint) x >= TileDimension || (uint) y >= TileDimension) {
				ErrorHelpers.ThrowOutOfRangeArgument(ErrorHelpers.PixelOutOfRange);
			}
			return ref UnsafeGetPixel(x, y);
		}
	}

	/// <summary>
	/// Captures a single row of pixels from this viewport.
	/// </summary>
	/// <param name="row">The row to get [0..7].</param>
	/// <returns>A new span.</returns>
	/// <exception cref="ArgumentOutOfRangeException">If the selected row is outside of the viewport.</exception>
	public Span<SheetColor> GetRow(int row) {
		if ((uint) row >= 8) {
			ErrorHelpers.ThrowOutOfRangeArgument(nameof(row), ErrorHelpers.BadTileRow);
		}

		return MemoryMarshal.CreateSpan(ref UnsafeGetPixel(0, row), 8);
	}

	/// <summary>
	/// Indexes the colors in this tile and writes them to <paramref name="tile"/>.
	/// </summary>
	/// <param name="tile">The destination tile to write indexed data to.</param>
	/// <param name="paletteSet">The palette set to index from.</param>
	public void IndexTile(scoped ref IndexedTile tile, IPalette paletteSet) {
		ArgumentNullException.ThrowIfNull(paletteSet);

		ref byte colorPointer = ref tile._tileData;
		foreach (SheetColor color in this) {
			colorPointer = paletteSet.IndexOf(color);
			colorPointer = ref Unsafe.Add(ref colorPointer, 1);
		}
	}

	/// <inheritdoc cref="IndexTile(ref IndexedTile, IPalette)"/>
	public void IndexTile(scoped IndexedTileViewport tile, IPalette paletteSet) {
		ArgumentNullException.ThrowIfNull(paletteSet);

		int pixelIndex = 0;

		foreach (SheetColor color in this) {
			tile[pixelIndex++] = paletteSet.IndexOf(color);
		}
	}

	/// <summary>
	/// Indexes the colors in this tile and returns a new indexed tile.
	/// </summary>
	/// <inheritdoc cref="IndexTile(ref IndexedTile, IPalette)" path="//param[@name='paletteSet']"/>
	/// <returns>A new <see cref="GraphicsTile"/> object.</returns>
	public GraphicsTile CreateGraphicsTile(IPalette paletteSet) {
		GraphicsTile ret = new();
		IndexTile(ref ret._indexedTile, paletteSet);
		return ret;
	}

	/// <summary>
	/// No bounds check method for internal use.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ref SheetColor UnsafeGetPixel(int x, int y) {
		return ref MemoryHelpers.GetPixelRef(ref _reference, x, y, _pixelsPerRow);
	}

	/// <summary>
	/// Returns an enumerator for this viewport.
	/// </summary>
	public Enumerator GetEnumerator() => new(this);

	/// <summary>
	/// Enumerator for a viewport.
	/// </summary>
	public ref struct Enumerator {
		private readonly TileViewport _viewport;
		private int _x = -1;
		private int _y = 0;

		internal Enumerator(TileViewport viewport) {
			_viewport = viewport;
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.Current"/>
		public readonly ref SheetColor Current {
			get => ref _viewport.UnsafeGetPixel(_x, _y);
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.MoveNext" path="//summary|//returns"/>
		public bool MoveNext() {
			int x = _x;

			if (x is TileDimension - 1) {
				int y = _y;

				if (y is TileDimension - 1) {
					return false;
				}

				x = 0;
				_y = y + 1;

			} else {
				x++;
			}

			_x = x;

			return true;
		}
	}
}
