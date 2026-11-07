namespace Sheeter;

/// <summary>
/// Provides an aligned view over the tiles in a bitmap.
/// </summary>
public readonly ref struct TiledIndexedBitmapViewport {
	internal const int TileDimension = IndexedTile.TileDimension;

	internal readonly ref byte _reference;
	internal readonly int _bytesPerRow;

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
	internal TiledIndexedBitmapViewport(ref byte pointer, int x, int y, int tilesWidth, int tilesHeight, int stride) {
		Width = tilesWidth;
		Height = tilesHeight;

		_bytesPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref pointer, x, y, stride);
	}

	/// <inheritdoc cref="TiledBitmapViewport(SheeterBitmap, int, int, int, int)"/>
	public TiledIndexedBitmapViewport(IndexedBitmap image, int x, int y, int tilesWidth, int tilesHeight) {
		ArgumentNullException.ThrowIfNull(image);

		int stride = image.Width;
		ErrorHelpers.ThrowIfBadViewPort(stride, image.Height, x, y, tilesWidth *= TileDimension, tilesHeight * TileDimension);

		Width = tilesWidth;
		Height = tilesHeight;

		_bytesPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image.GetPixelsReference(), x, y, stride);
	}

	/// <inheritdoc cref="TiledBitmapViewport(SheeterBitmap, int, int, int, int)"/>
	public TiledIndexedBitmapViewport(IndexedBitmapViewport image, int x, int y, int tilesWidth, int tilesHeight) {
		int stride = image._bytesPerRow;
		ErrorHelpers.ThrowIfBadViewPort(stride, image.Height, x, y, tilesWidth *= TileDimension, tilesHeight * TileDimension);

		Width = tilesWidth;
		Height = tilesHeight;

		_bytesPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image._reference, x, y, stride);
	}

	/// <inheritdoc cref="TiledBitmapViewport.Slice(int, int, int, int)"/>
	public TiledIndexedBitmapViewport Slice(int tilesX, int tilesY, int tilesWidth, int tilesHeight) {
		tilesX *= TileDimension;
		tilesY *= TileDimension;
		ErrorHelpers.ThrowIfBadViewPort(Width, Height, tilesX, tilesY, tilesWidth *= TileDimension, tilesHeight * TileDimension);

		return new TiledIndexedBitmapViewport(ref _reference, tilesX, tilesY, tilesWidth, tilesHeight, _bytesPerRow);
	}

	/// <inheritdoc cref="TiledBitmapViewport(SheeterBitmap)"/>
	public TiledIndexedBitmapViewport(IndexedBitmap image) {
		ArgumentNullException.ThrowIfNull(image);

		Width = image.Width / TileDimension;
		Height = image.Height / TileDimension;

		_bytesPerRow = image.BytesPerRow;
		_reference = ref image.GetPixelsReference();
	}

	/// <inheritdoc cref="TiledBitmapViewport(BitmapViewport)"/>
	public TiledIndexedBitmapViewport(IndexedBitmapViewport image) {
		Width = image.Width / TileDimension;
		Height = image.Height / TileDimension;

		_bytesPerRow = image._bytesPerRow;
		_reference = ref image._reference;
	}

	/// <inheritdoc cref="TiledBitmapViewport.this"/>
	public IndexedTileViewport this[int x, int y] {
		get {
			if ((uint) x >= Width || (uint) y >= Height) {
				ErrorHelpers.ThrowOutOfRangeArgument(ErrorHelpers.TileOutOfRange);
			}
			return UnsafeGetTile(x, y);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private IndexedTileViewport UnsafeGetTile(int x, int y) {
		return new(ref _reference, x * TileDimension, y * TileDimension, _bytesPerRow);
	}

	/// <summary>
	/// Returns a new copy of every tile in this viewport.
	/// </summary>
	/// <returns>A new array of <see cref="IndexedTile"/> structs.</returns>
	public IndexedTile[] CreateIndexedTiles() {
		IndexedTile[] tiles = GC.AllocateUninitializedArray<IndexedTile>(TileCount);

		int tileIndex = 0;
		foreach (IndexedTileViewport sourceTile in this) {
			sourceTile.CopyTo(ref tiles[tileIndex++]);
		}

		return tiles;
	}

	/// <summary>
	/// Returns a new managed copy of every tile in this viewport.
	/// </summary>
	/// <returns>A new array of <see cref="GraphicsTile"/> objects.</returns>
	public GraphicsTile[] CreateGraphicsTiles() {
		GraphicsTile[] tiles = new GraphicsTile[TileCount];

		int tileIndex = 0;
		foreach (IndexedTileViewport sourceTile in this) {
			GraphicsTile tmp = new();
			sourceTile.CopyTo(tmp);
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
		private readonly TiledIndexedBitmapViewport _viewport;
		private int _x = -1;
		private int _y = 0;

		internal Enumerator(TiledIndexedBitmapViewport viewport) {
			_viewport = viewport;
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.Current"/>
		public readonly IndexedTileViewport Current {
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
