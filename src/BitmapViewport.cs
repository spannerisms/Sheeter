namespace Sheeter;

/// <summary>
/// Provides a view over a rectangular portion of a bitmap.
/// </summary>
public readonly ref struct BitmapViewport {
	internal readonly ref SheetColor _reference;
	internal readonly int _pixelsPerRow;

	/// <summary>
	/// Gets the width in pixels of this viewport.
	/// </summary>
	public int Width { get; }

	/// <summary>
	/// Gets the height in pixels of this viewport.
	/// </summary>
	public int Height { get; }

	/// <summary>
	/// Returns the number of pixels captured by this viewport.
	/// </summary>
	public int PixelCount => Width * Height;

	/// <inheritdoc cref="SheeterBitmap.Tileable"/>
	public bool Tileable => SheeterBitmap.DimensionsCanBeTiled(Width, Height);

	/// <summary>
	/// Creates a new viewport over the entire image.
	/// </summary>
	/// <param name="image">The image to capture.</param>
	public BitmapViewport(SheeterBitmap image) {
		ArgumentNullException.ThrowIfNull(image);

		Width = image.Width;
		Height = image.Height;
		_pixelsPerRow = image.PixelsPerRow;
		_reference = ref image.GetPixelsReference();
	}

	/// <inheritdoc cref="BitmapViewport(SheeterBitmap)"/>
	public BitmapViewport(TiledBitmapViewport image) {
		Width = image.Width * TiledBitmapViewport.TileDimension;
		Height = image.Height * TiledBitmapViewport.TileDimension;
		_pixelsPerRow = image._pixelsPerRow;
		_reference = ref image._reference;
	}

	/// <summary>
	/// Creates a new viewport over a section of an image.
	/// </summary>
	/// <param name="image">The source image.</param>
	/// <param name="x">X-position of the new top-left corner relative the top-left of the source.</param>
	/// <param name="y">Y-position of the new top-left corner relative the top-left of the source.</param>
	/// <param name="width">The width of the viewport in pixels.</param>
	/// <param name="height">The height of the viewport in pixels.</param>
	/// <exception cref="ArgumentOutOfRangeException">If the viewport escapes the bounds of the source.</exception>
	public BitmapViewport(SheeterBitmap image, int x, int y, int width, int height) {
		ArgumentNullException.ThrowIfNull(image);
		ErrorHelpers.ThrowIfBadViewPort(image.Width, image.Height, x, y, width, height);

		Width = width;
		Height = height;

		int stride = image.PixelsPerRow;

		_pixelsPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image.GetPixelsReference(), x, y, stride);
	}

	/// <inheritdoc cref="BitmapViewport(SheeterBitmap, int, int, int, int)"/>
	public BitmapViewport(BitmapViewport image, int x, int y, int width, int height) {
		ErrorHelpers.ThrowIfBadViewPort(image.Width, image.Height, x, y, width, height);

		Width = width;
		Height = height;

		int stride = image._pixelsPerRow;

		_pixelsPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image._reference, x, y, stride);
	}

	/// <inheritdoc cref="SheeterBitmap.IndexImage(IPalette)"/>
	public IndexedBitmap IndexImage(IPalette paletteSet) {
		ArgumentNullException.ThrowIfNull(paletteSet);

		IndexedBitmap ret = new(Width, Height);

		ref byte outptr = ref ret.GetPixelsReference();

		foreach (SheetColor color in this) {
			outptr = paletteSet.IndexOf(color);
			outptr = ref Unsafe.Add(ref outptr, 1);
		}

		return ret;
	}

	/// <summary>
	/// Gets or sets the color at the given coordinates inside this viewport.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the selected pixel is outside of the viewport.</exception>
	public ref SheetColor this[int x, int y] {
		get {
			if ((uint) x > Width || (uint) y > Height) {
				ErrorHelpers.ThrowOutOfRangeArgument(ErrorHelpers.PixelOutOfRange);
			}
			return ref UnsafeGetPixel(x, y);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref SheetColor UnsafeGetPixel(int x, int y) {
		return ref MemoryHelpers.GetPixelRef(ref _reference, x, y, _pixelsPerRow);
	}

	/// <inheritdoc cref="SheeterBitmap.CreateIndexedTiles(IPalette)"/>
	public IndexedTile[] CreateIndexedTiles(IPalette paletteSet) {
		return new TiledBitmapViewport(this).CreateIndexedTiles(paletteSet);
	}

	/// <inheritdoc cref="SheeterBitmap.CreateGraphicsTiles(IPalette)"/>
	public GraphicsTile[] CreateGraphicsTiles(IPalette paletteSet) {
		return new TiledBitmapViewport(this).CreateGraphicsTiles(paletteSet);
	}

	/// <summary>
	/// Returns an enumerator for this viewport.
	/// </summary>
	public Enumerator GetEnumerator() => new(this);

	/// <summary>
	/// Enumerator for <see cref="BitmapViewport"/>
	/// </summary>
	public ref struct Enumerator {
		private readonly BitmapViewport _viewport;
		private int _x = -1;
		private int _y = 0;

		internal Enumerator(BitmapViewport viewport) {
			_viewport = viewport;
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.Current"/>
		public readonly ref SheetColor Current {
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
