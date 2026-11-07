namespace Sheeter;

/// <summary>
/// Provides a view over a rectangular region of an indexed image.
/// </summary>
[DebuggerDisplay("Width: {Width}; Height: {Height}")]
public readonly ref struct IndexedBitmapViewport {
	internal readonly ref byte _reference;
	internal readonly int _bytesPerRow;

	/// <inheritdoc cref="BitmapViewport.Width"/>
	public int Width { get; }

	/// <inheritdoc cref="BitmapViewport.Height"/>
	public int Height { get; }

	/// <inheritdoc cref="BitmapViewport.PixelCount"/>
	public int PixelCount => Width * Height;

	/// <inheritdoc cref="SheeterBitmap.Tileable"/>
	public bool Tileable => SheeterBitmap.DimensionsCanBeTiled(Width, Height);

	/// <inheritdoc cref="BitmapViewport(SheeterBitmap)"/>
	public IndexedBitmapViewport(IndexedBitmap image) {
		ArgumentNullException.ThrowIfNull(image);

		Width = image.Width;
		Height = image.Height;
		_bytesPerRow = image.BytesPerRow;
		_reference = ref image.GetPixelsReference();
	}

	/// <inheritdoc cref="BitmapViewport(SheeterBitmap)"/>
	public IndexedBitmapViewport(TiledIndexedBitmapViewport image) {
		Width = image.Width * TiledIndexedBitmapViewport.TileDimension;
		Height = image.Height * TiledIndexedBitmapViewport.TileDimension;
		_bytesPerRow = image._bytesPerRow;
		_reference = ref image._reference;
	}

	/// <inheritdoc cref="BitmapViewport(SheeterBitmap, int, int, int, int)"/>
	public IndexedBitmapViewport(IndexedBitmap image, int x, int y, int width, int height) {
		ArgumentNullException.ThrowIfNull(image);
		ErrorHelpers.ThrowIfBadViewPort(image.Width, image.Height, x, y, width, height);

		Width = width;
		Height = height;

		int stride = image.BytesPerRow;

		_bytesPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image.GetPixelsReference(), x, y, stride);
	}

	/// <inheritdoc cref="BitmapViewport(BitmapViewport, int, int, int, int)"/>
	public IndexedBitmapViewport(IndexedBitmapViewport image, int x, int y, int width, int height) {
		ErrorHelpers.ThrowIfBadViewPort(image.Width, image.Height, x, y, width, height);

		Width = width;
		Height = height;

		int stride = image._bytesPerRow;

		_bytesPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image._reference, x, y, stride);
	}

	/// <inheritdoc cref="IndexedBitmap.CreateIndexedTiles()"/>
	public IndexedTile[] CreateIndexedTiles() {
		return new TiledIndexedBitmapViewport(this).CreateIndexedTiles();
	}

	/// <inheritdoc cref="IndexedBitmap.CreateGraphicsTiles()"/>
	public GraphicsTile[] CreateGraphicsTiles() {
		return new TiledIndexedBitmapViewport(this).CreateGraphicsTiles();
	}

	/// <summary>
	/// Gets the color index at the given coordinates inside this viewport.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the selected pixel is outside of the viewport.</exception>
	public ref byte this[int x, int y] {
		get {
			if ((uint) x > Width || (uint) y > Height) {
				ErrorHelpers.ThrowOutOfRangeArgument(ErrorHelpers.PixelOutOfRange);
			}

			return ref UnsafeGetPixel(x, y);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref byte UnsafeGetPixel(int x, int y) {
		return ref MemoryHelpers.GetPixelRef(ref _reference, x, y, _bytesPerRow);
	}

	/// <summary>
	/// Returns an enumerator for this viewport.
	/// </summary>
	public Enumerator GetEnumerator() => new(this);

	/// <summary>
	/// Enumerator for <see cref="BitmapViewport"/>
	/// </summary>
	public ref struct Enumerator {
		private readonly IndexedBitmapViewport _viewport;
		private int _x = -1;
		private int _y = 0;

		internal Enumerator(IndexedBitmapViewport viewport) {
			_viewport = viewport;
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.Current"/>
		public readonly ref byte Current {
			get => ref _viewport.UnsafeGetPixel(_x, _y);
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.MoveNext" path="//summary|//returns"/>
		public bool MoveNext() {
			if (_x == _viewport.Width) {
				if (_y == _viewport.Height) {
					return false;
				}

				_x = 0;
				_y++;
				return true;
			}

			_x++;
			return true;
		}

	}
}
