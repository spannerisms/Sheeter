namespace Sheeter;

/// <summary>
/// Provides an aligned view over the tiles in a bitmap.
/// </summary>
public readonly ref struct TiledBitmapViewport {
	internal const int TileDimension = IndexedTile.TileDimension;

	internal readonly ref SheetColor _reference;
	internal readonly int _pixelsPerRow;

	/// <summary>
	/// Gets the width in tiles of this viewport.
	/// </summary>
	public int Width { get; }

	/// <summary>
	/// Gets the height in tiles of this viewport.
	/// </summary>
	public int Height { get; }

	/// <summary>
	/// Gets the number of tiles captured by this viewport.
	/// </summary>
	public int TileCount => Width * Height;

	/// <summary>
	/// Unsafe constructor that elides bounds checking.
	/// </summary>
	internal TiledBitmapViewport(ref SheetColor pointer, int x, int y, int tilesWidth, int tilesHeight, int pixelsPerRow) {
		Width = tilesWidth;
		Height = tilesHeight;

		_pixelsPerRow = pixelsPerRow;
		_reference = ref MemoryHelpers.GetPixelRef(ref pointer, x, y, pixelsPerRow);
	}

	/// <summary>
	/// Creates a new gridded viewport of image <paramref name="image"/>
	/// with its top-left corner at the given coordinates in pixels
	/// and with the given width and height in tiles.
	/// </summary>
	/// <param name="image">The image to create a viewport for.</param>
	/// <param name="x">The X-coordinate in pixels of the top-left corner of the tilemap view.</param>
	/// <param name="y">The Y-coordinate in pixels of the top-left corner of the tilemap view.</param>
	/// <param name="tilesWidth">The width of the tilemap in tiles.</param>
	/// <param name="tilesHeight">The height of the tilemap in tiles.</param>
	/// <exception cref="ArgumentOutOfRangeException">If the viewport escapes the bounds of the source.</exception>
	public TiledBitmapViewport(SheeterBitmap image, int x, int y, int tilesWidth, int tilesHeight) {
		ArgumentNullException.ThrowIfNull(image);

		int stride = image.Width;
		ErrorHelpers.ThrowIfBadViewPort(stride, image.Height, x, y, tilesWidth *= TileDimension, tilesHeight * TileDimension);

		Width = tilesWidth;
		Height = tilesHeight;

		_pixelsPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image.GetPixelsReference(), x, y, stride);
	}

	/// <inheritdoc cref="TiledBitmapViewport(SheeterBitmap, int, int, int, int)"/>
	public TiledBitmapViewport(BitmapViewport image, int x, int y, int tilesWidth, int tilesHeight) {
		int stride = image._pixelsPerRow;
		ErrorHelpers.ThrowIfBadViewPort(stride, image.Height, x, y, tilesWidth *= TileDimension, tilesHeight * TileDimension);

		Width = tilesWidth;
		Height = tilesHeight;

		_pixelsPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image._reference, x, y, stride);
	}

	/// <summary>
	/// Creates a new gridded viewport over the entire image. Excess content is trimmed.
	/// </summary>
	/// <param name="image">The image to tile.</param>
	public TiledBitmapViewport(SheeterBitmap image) {
		ArgumentNullException.ThrowIfNull(image);

		Width = image.Width / TileDimension;
		Height = image.Height / TileDimension;

		_pixelsPerRow = image.PixelsPerRow;
		_reference = ref image.GetPixelsReference();
	}

	/// <inheritdoc cref="TiledBitmapViewport(SheeterBitmap)"/>
	public TiledBitmapViewport(BitmapViewport image) {
		Width = image.Width / TileDimension;
		Height = image.Height / TileDimension;

		_pixelsPerRow = image._pixelsPerRow;
		_reference = ref image._reference;
	}

	/// <summary>
	/// Creates a slice of this viewport at the given coordinates
	/// with the given width and height.
	/// </summary>
	/// <param name="tilesX">The X-coordinate in tiles of the top-left tile of the new viewport.</param>
	/// <param name="tilesY">The Y-coordinate in tiles of the top-left tile of the new viewport.</param>
	/// <param name="tilesWidth">The width in tiles of the new viewport.</param>
	/// <param name="tilesHeight">The height in tiles of the new viewport.</param>
	/// <returns>A new viewport.</returns>
	/// <exception cref="ArgumentOutOfRangeException">If the slice escapes the bounds of the viewport.</exception>
	public TiledBitmapViewport Slice(int tilesX, int tilesY, int tilesWidth, int tilesHeight) {
		tilesX *= TileDimension;
		tilesY *= TileDimension;
		ErrorHelpers.ThrowIfBadViewPort(Width, Height, tilesX, tilesY, tilesWidth *= TileDimension, tilesHeight * TileDimension);

		return new TiledBitmapViewport(ref _reference, tilesX, tilesY, tilesWidth, tilesHeight, _pixelsPerRow);
	}

	/// <summary>
	/// Returns a viewport for the tile at the relative coordinates inside this tilemap.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the selected tile is outside of the viewport.</exception>
	public TileViewport this[int x, int y] {
		get {
			if ((uint) x >= Width || (uint) y >= Height) {
				ErrorHelpers.ThrowOutOfRangeArgument(ErrorHelpers.TileOutOfRange);
			}

			return UnsafeGetTile(x, y);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private TileViewport UnsafeGetTile(int x, int y) {
		return new(ref _reference, x * TileDimension, y * TileDimension, _pixelsPerRow);
	}

	/// <summary>
	/// Indexes the tiles in this viewport to a new array of indexed tiles using the given palette.
	/// </summary>
	/// <param name="paletteSet">The palette set to use.</param>
	/// <returns>A new array of <see cref="IndexedTile"/> structs.</returns>
	public IndexedTile[] CreateIndexedTiles(IPalette paletteSet) {
		ArgumentNullException.ThrowIfNull(paletteSet);
		IndexedTile[] ret = GC.AllocateUninitializedArray<IndexedTile>(TileCount);

		int tileIndex = 0;
		foreach (TileViewport sourceTile in this) {
			sourceTile.IndexTile(ref ret[tileIndex++], paletteSet);
		}

		return ret;
	}

	/// <summary>
	/// Indexes the tiles in this viewport to an array of managed indexed tiles using the given palette.
	/// </summary>
	/// <param name="paletteSet">The palette set to use.</param>
	/// <returns>A new array of <see cref="GraphicsTile"/> objects.</returns>
	public GraphicsTile[] CreateGraphicsTiles(IPalette paletteSet) {
		ArgumentNullException.ThrowIfNull(paletteSet);
		GraphicsTile[] tiles = new GraphicsTile[TileCount];

		int tileIndex = 0;
		foreach (TileViewport sourceTile in this) {
			GraphicsTile tmp = new();
			sourceTile.IndexTile(ref tmp._indexedTile, paletteSet);
			tiles[tileIndex++] = tmp;
		}

		return tiles;
	}

	/// <summary>
	/// Returns an enumerator for this viewport.
	/// </summary>
	public Enumerator GetEnumerator() => new(this);

	/// <summary>
	/// Enumerator for a viewport.
	/// </summary>
	public ref struct Enumerator {
		private readonly TiledBitmapViewport _viewport;
		private int _x = -1;
		private int _y = 0;

		internal Enumerator(TiledBitmapViewport viewport) {
			_viewport = viewport;
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.Current"/>
		public readonly TileViewport Current {
			get => _viewport.UnsafeGetTile(_x, _y);
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.MoveNext" path="//summary|//returns"/>
		public bool MoveNext() {
			int x = _x + 1;

			if (x == _viewport.Width) {
				int y = _y + 1;

				if (y == _viewport.Height) {
					return false;
				}

				x = 0;
				_y = y;
			}

			_x = x;

			return true;
		}
	}
}
