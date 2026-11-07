using SkiaSharp;

namespace Sheeter;

// This class acts as a replacement for SKBitmap
// so that the actual format of an image remains an implementation detail

/// <summary>
/// Represents a bitmap image with 32-bit ARGB colors.
/// </summary>
public unsafe sealed class SheeterBitmap : IDisposable {
	private SheetColor* _pointer;

	/// <summary>
	/// Returns a pointer to this image's data.
	/// </summary>
	public nint Pointer => (nint) _pointer;

	/// <summary>
	/// Gets the height of this image in pixels.
	/// </summary>
	public int Height { get; init; }

	/// <summary>
	/// Gets the width of this image in pixels.
	/// </summary>
	public int Width { get; }

	/// <summary>
	/// Gets the number of pixels in a single row.
	/// </summary>
	internal int PixelsPerRow {
		get;
		init {
			field = value;
			BytesPerRow = value * 4;
		}
	}

	/// <summary>
	/// Gets the number of bytes in a single row.
	/// </summary>
	public int BytesPerRow { get; private init; }

	/// <summary>
	/// Returns the total number of visible pixels in this image.
	/// </summary>
	public int PixelCount => Width * Height;

	/// <summary>
	/// Returns whether this instance can be safely tiled without losing excess content.
	/// </summary>
	/// <remarks>An image is tileable if both its width and height are multiples of 8.</remarks>
	public bool Tileable => DimensionsCanBeTiled(Width, Height);

	private readonly bool _ownsPixels;
	private bool _disposed = false;

	/// <summary>
	/// Creates a new blank image with the given dimensions.
	/// </summary>
	/// <param name="width">The width of the image.</param>
	/// <param name="height">The height of the image.</param>
	/// <exception cref="ArgumentOutOfRangeException">If either dimension is zero or negative.</exception>
	public SheeterBitmap(int width, int height) {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		Width = width;
		Height = height;
		PixelsPerRow = width;
		_pointer = NativeMemory.AllocNice<SheetColor>(width * height);
		_ownsPixels = true;

		Clear();
	}

	/// <summary>
	/// Creates a new image with the given dimensions.
	/// </summary>
	/// <param name="width">The width of the image.</param>
	/// <param name="height">The height of the image.</param>
	/// <param name="bytesPerRow">The number of bytes in a row of the image. This value must be a multiple of 4.</param>
	/// <param name="handle">
	/// A pointer to the color data.
	/// This should be an existing block of memory representing 32-bit colors
	/// that has already been pinned or is unmanaged.
	/// </param>
	/// <exception cref="ArgumentOutOfRangeException">If either dimension is zero or negative.</exception>
	/// <exception cref="ArgumentException">If <paramref name="bytesPerRow"/> is not a multiple of 4.</exception>
	/// <exception cref="ArgumentException">If bytes per row cannot fit the width of the image.</exception>
	public SheeterBitmap(int width, int height, int bytesPerRow, nint handle) {
		ArgumentNullException.ThrowIfNull((void*) handle, nameof(handle));
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesPerRow);

		var (p, r) = Math.DivRem(bytesPerRow, 4);

		if (r is not 0) {
			ErrorHelpers.ThrowBadArgument("Bytes per row must be a multiple of 4.");
		}

		if (p < width) {
			ErrorHelpers.ThrowBadArgument($"{nameof(bytesPerRow)} must be at least 4 times {nameof(width)}.");
		}

		PixelsPerRow = p;
		Width = width;
		Height = height;
		_pointer = (SheetColor*) handle;
		_ownsPixels = false;
	}

	/// <summary>
	/// Creates a new image from the given file.
	/// </summary>
	/// <param name="path">The file to open.</param>
	public SheeterBitmap(string path) {
		using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read);
		(Width, Height) = GetImage(stream);
		PixelsPerRow = Width;
		_ownsPixels = true;
	}

	/// <summary>
	/// Creates a new image from the given file.
	/// </summary>
	/// <param name="file">The file to open.</param>
	public SheeterBitmap(FileInfo file) {
		ArgumentNullException.ThrowIfNull(file);
		FileStream stream = file.OpenRead();
		(Width, Height) = GetImage(stream);
		PixelsPerRow = Width;
		_ownsPixels = true;
	}

	/// <summary>
	/// Creates a new image from the given stream.
	/// </summary>
	/// <param name="stream">The stream to read.</param>
	public SheeterBitmap(Stream stream) {
		ArgumentNullException.ThrowIfNull(stream);
		(Width, Height) = GetImage(stream);
		PixelsPerRow = Width;
		_ownsPixels = true;
	}

	private (int width, int height) GetImage(Stream stream) {
		using SKCodec codec = SKCodec.Create(stream);

		int width = codec.Info.Width;
		int height = codec.Info.Height;

		SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

		_pointer = NativeMemory.AllocNice<SheetColor>(width * height);
	
		codec.GetPixels(info, (nint) _pointer);

		return (width, height);
	}

	/// <exception cref="ObjectDisposedException"/>
	internal ref SheetColor GetPixelsReference() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return ref Unsafe.AsRef<SheetColor>(_pointer);
	}

	/// <summary>
	/// Gets or sets the color at the given coordinates.
	/// </summary>
	/// <param name="x">X coordinate</param>
	/// <param name="y">Y coordinate</param>
	/// <returns>The color at position {<paramref name="x"/>, <paramref name="y"/>}.</returns>
	/// <exception cref="ArgumentOutOfRangeException">If the selected pixel is outside of the image.</exception>
	public SheetColor this[int x, int y] {
		get => GetPixel(x, y);
		set => GetPixel(x, y) = value;
	}

	internal ref SheetColor GetPixel(int x, int y) {
		if ((uint) x >= Width) {
			ErrorHelpers.ThrowOutOfRangeArgument(nameof(x), ErrorHelpers.BadImageX);
		}

		if ((uint) y >= Height) {
			ErrorHelpers.ThrowOutOfRangeArgument(nameof(y), ErrorHelpers.BadImageY);
		}

		return ref UnsafeGetPixel(x, y);
	}

	/// <exception cref="ObjectDisposedException"/>
	private ref SheetColor UnsafeGetPixel(int x, int y) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return ref _pointer[(y * BytesPerRow) + x];
	}

	/// <summary>
	/// Returns a new span over every color in the image.
	/// </summary>
	/// <returns>A new span!</returns>
	public Span<SheetColor> GetPixelSpan() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return new(_pointer, Height * PixelsPerRow);
	}

	internal static bool DimensionsCanBeTiled(int width, int height) {
		return ((width | height) & 7) == 0;
	}

	/// <summary>
	/// Clears all data in this image.
	/// </summary>
	public void Clear() {
		GetPixelSpan().Clear();
	}

	/// <summary>
	/// Converts this image to a collection of indexed tiles for the given palette set.
	/// </summary>
	/// <inheritdoc cref="CreateGraphicsTiles(IPalette)" path="//remarks|//exception"/>
	/// <param name="paletteSet">The palette set to use.</param>
	public IndexedTile[] CreateIndexedTiles(IPalette paletteSet) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		IndexedTile[] ret = new TiledBitmapViewport(this).CreateIndexedTiles(paletteSet);
		GC.KeepAlive(this);
		return ret;
	}

	/// <summary>
	/// Converts this image to an array of managed indexed tiles using the given palette set.
	/// </summary>
	/// <remarks>
	/// Instances calling this method should verify they have a width and height that are each
	/// a multiple of 8; otherwise, excess content will be trimmed and lost.
	/// </remarks>
	/// <param name="paletteSet">The palette set to use.</param>
	/// <returns>A new array of tiles.</returns>
	public GraphicsTile[] CreateGraphicsTiles(IPalette paletteSet) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		GraphicsTile[] ret = new TiledBitmapViewport(this).CreateGraphicsTiles(paletteSet);
		GC.KeepAlive(this);
		return ret;
	}

	/// <summary>
	/// Indexes every color in this instance.
	/// </summary>
	/// <param name="paletteSet">The palette set to index colors from.</param>
	/// <returns>A new <see cref="IndexedBitmap"/> containing an indexed copy of this instance's pixels.</returns>
	public IndexedBitmap IndexImage(IPalette paletteSet) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentNullException.ThrowIfNull(paletteSet);

		IndexedBitmap indexedImage = new(Width, Height);
		byte* writePtr = (byte*) indexedImage.Pointer;

		// use a Span when possible for performance
		// fall back to custom enumerator otherwise
		if (Width == PixelsPerRow) {
			foreach (SheetColor color in GetPixelSpan()) {
				*writePtr++ = paletteSet.IndexOf(color);
			}

		} else {
			foreach (SheetColor color in this) {
				*writePtr++ = paletteSet.IndexOf(color);
			}
		}

		GC.KeepAlive(this);
		return indexedImage;
	}

	/// <summary>
	/// Finalizer.
	/// </summary>
	~SheeterBitmap() {
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
	/// Returns an enumerator for this instance.
	/// </summary>
	public Enumerator GetEnumerator() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		return new Enumerator(this);
	}

	/// <summary>
	/// Enumerator for <see cref="SheeterBitmap"/>.
	/// </summary>
	public sealed class Enumerator {
		private readonly SheeterBitmap _image;
		private int _x = -1;
		private int _y = 0;

		internal Enumerator(SheeterBitmap image) {
			_image = image;
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.Current"/>
		public ref SheetColor Current {
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
