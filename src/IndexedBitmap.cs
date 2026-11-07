namespace Sheeter;

/// <summary>
/// Represents a bitmap image with 8-bits-per-pixel indexed colors.
/// </summary>
public unsafe sealed class IndexedBitmap : IDisposable {
	private byte* _pointer;

	/// <summary>
	/// Returns a pointer to this image's data.
	/// </summary>
	public nint Pointer => (nint) _pointer;

	/// <summary>
	/// Gets the height of this image in pixels.
	/// </summary>
	public int Height { get; }

	/// <summary>
	/// Gets the width of this image in pixels.
	/// </summary>
	public int Width { get; }

	/// <summary>
	/// Gets the number of bytes in a single row.
	/// </summary>
	public int BytesPerRow { get; }

	/// <summary>
	/// Gets the number of pixels in a single row.
	/// </summary>
	public int PixelsPerRow => BytesPerRow;

	/// <summary>
	/// Returns the total number of visible pixels in this image.
	/// </summary>
	public int PixelCount => Width * Height;

	/// <inheritdoc cref="SheeterBitmap.Tileable"/>
	public bool Tileable => SheeterBitmap.DimensionsCanBeTiled(Width, Height);

	private readonly bool _ownsPixels;
	private bool _disposed = false;

	/// <summary>
	/// Creates a new image with the given dimensions.
	/// </summary>
	/// <param name="width">The width of the image.</param>
	/// <param name="height">The height of the image.</param>
	/// <exception cref="ArgumentOutOfRangeException">If either dimension is zero or negative.</exception>
	public IndexedBitmap(int width, int height) {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		Width = width;
		Height = height;
		BytesPerRow = width;
		_pointer = NativeMemory.AllocNice<byte>(width * height);
		_ownsPixels = true;
		Clear();
	}

	/// <summary>
	/// Creates a new image with the given dimensions and pointer.
	/// </summary>
	/// <remarks>
	/// <paramref name="handle"/> should point to pinned or unmanaged data that is already
	/// in a valid 8-bits-per-pixel indexed format.
	/// </remarks>
	/// <param name="width">The width of the image.</param>
	/// <param name="height">The height of the image.</param>
	/// <param name="bytesPerRow">The number of bytes per row in the image. This value be a multiple of 4.</param>
	/// <param name="handle">
	/// A pointer to the indexed data.
	/// This should be an existing block of memory that has already been pinned or is unmanaged.
	/// </param>
	/// <exception cref="ArgumentOutOfRangeException">If either dimension is zero or negative.</exception>
	/// <exception cref="ArgumentException">If bytes per row cannot fit the width of the image.</exception>
	public IndexedBitmap(int width, int height, int bytesPerRow, nint handle) {
		ArgumentNullException.ThrowIfNull((void*) handle, nameof(handle));
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesPerRow);

		if (bytesPerRow < width) {
			ErrorHelpers.ThrowBadArgument($"{nameof(bytesPerRow)} must be greater than or equal to {nameof(width)}.");
		}

		Width = width;
		Height = height;
		_pointer = (byte*) handle;
		BytesPerRow = bytesPerRow;
		_ownsPixels = false;
	}

	/// <exception cref="ObjectDisposedException"/>
	internal ref byte GetPixelsReference() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return ref Unsafe.AsRef<byte>(_pointer);
	}

	/// <summary>
	/// Gets or sets the color index at the given coordinates.
	/// </summary>
	/// <param name="x">X coordinate</param>
	/// <param name="y">Y coordinate</param>
	/// <returns>The color index at position {<paramref name="x"/>, <paramref name="y"/>}.</returns>
	/// <exception cref="ArgumentOutOfRangeException">If the selected pixel is outside of the viewport.</exception>
	public byte this[int x, int y] {
		get => GetPixel(x, y);
		set => GetPixel(x, y) = value;
	}

	internal ref byte GetPixel(int x, int y) {
		if ((uint) x >= Width) {
			ErrorHelpers.ThrowOutOfRangeArgument(nameof(x), ErrorHelpers.BadImageX);
		}

		if ((uint) y >= Height) {
			ErrorHelpers.ThrowOutOfRangeArgument(nameof(y), ErrorHelpers.BadImageY);
		}

		return ref UnsafeGetPixel(x, y);
	}

	/// <exception cref="ObjectDisposedException"/>
	private ref byte UnsafeGetPixel(int x, int y) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return ref _pointer[(y * BytesPerRow) + x];
	}

	/// <summary>
	/// Creates an array of managed indexed tiles from the tiles in this image.
	/// </summary>
	/// <inheritdoc cref="SheeterBitmap.CreateIndexedTiles(IPalette)" path="//remarks"/>
	/// <returns>A new array of <see cref="IndexedTile"/> structs.</returns>
	public IndexedTile[] CreateIndexedTiles() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		IndexedTile[] ret = new TiledIndexedBitmapViewport(this).CreateIndexedTiles();
		GC.KeepAlive(this);
		return ret;
	}

	/// <summary>
	/// Creates an array of managed indexed tiles from the tiles in this image.
	/// </summary>
	/// <inheritdoc cref="SheeterBitmap.CreateIndexedTiles(IPalette)" path="//remarks"/>
	/// <returns>A new array of <see cref="GraphicsTile"/> objects.</returns>
	public GraphicsTile[] CreateGraphicsTiles() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		GraphicsTile[] ret = new IndexedBitmapViewport(this).CreateGraphicsTiles();
		GC.KeepAlive(this);
		return ret;
	}

	/// <summary>
	/// Draws a given tile to this image.
	/// </summary>
	/// <param name="tile">The tile to draw.</param>
	/// <param name="x">The X coordinate of the top-left pixel of the destination.</param>
	/// <param name="y">The Y coordinate of the top-left pixel of the destination.</param>
	public void DrawTile(scoped ref readonly IndexedTile tile, int x, int y) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		tile.CopyTo(new IndexedTileViewport(this, x, y));
	}

	/// <inheritdoc cref="DrawTile(ref readonly IndexedTile, int, int)"/>
	public void DrawTile(scoped IndexedTileViewport tile, int x, int y) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		tile.CopyTo(new IndexedTileViewport(this, x, y));
	}

	/// <inheritdoc cref="DrawTile(ref readonly IndexedTile, int, int)"/>
	public void DrawTile(GraphicsTile tile, int x, int y) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		tile.CopyTo(new IndexedTileViewport(this, x, y));
	}

	/// <summary>
	/// Gets a viewport for the tile at the given coordinates.
	/// </summary>
	/// <param name="x">The X coordinate of the top-left pixel of the tile.</param>
	/// <param name="y">The Y coordinate of the top-left pixel of the tile.</param>
	public IndexedTileViewport GetTile(int x, int y) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return new IndexedTileViewport(this, x, y);
	}

	/// <summary>
	/// Returns a new span over every color in the image.
	/// </summary>
	public Span<byte> GetPixelSpan() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return new(_pointer, Height * BytesPerRow);
	}

	/// <summary>
	/// Clears all pixels in this image.
	/// </summary>
	public void Clear() {
		GetPixelSpan().Clear();
	}

	/// <summary>
	/// Finalizer.
	/// </summary>
	~IndexedBitmap() {
		Dispose(false);
	}

	/// <inheritdoc/>
	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool _) {
		if (_disposed) {
			return;
		}

		_disposed = true;

		if (_ownsPixels) {
			NativeMemory.AlignedFree(_pointer);
		}

		_pointer = default;
	}

	/// <summary>
	/// Gets enumerator.
	/// </summary>
	public Enumerator GetEnumerator() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return new Enumerator(this);
	}

	/// <summary>
	/// Enumerator for <see cref="IndexedBitmap"/>.
	/// </summary>
	public sealed class Enumerator {
		private readonly IndexedBitmap _image;
		private int _x = -1;
		private int _y = 0;

		internal Enumerator(IndexedBitmap image) {
			_image = image;
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.Current"/>
		public ref byte Current {
			get => ref _image.UnsafeGetPixel(_x, _y);
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.MoveNext" path="//summary|//returns"/>
		public bool MoveNext() {
			int x = _x + 1;

			if (x == _image.Width) {
				int y = _y + 1;

				if (y == _image.Height) {
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
