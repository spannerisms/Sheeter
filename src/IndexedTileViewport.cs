using Sheeter.Snes;

namespace Sheeter;

/// <summary>
/// Provides a view over the pixels in an 8x8 region of an indexed image.
/// Unlike <see cref="IndexedTile"/>, the rows of this view are not necessarily contiguous.
/// </summary>
public readonly ref struct IndexedTileViewport {
	internal const int TileDimension = IndexedTile.TileDimension;

	internal readonly ref byte _reference;
	internal readonly int _bytesPerRow;

	/// <inheritdoc cref="TileViewport(SheeterBitmap, int, int)"/>
	public IndexedTileViewport(IndexedBitmap image, int x, int y) {
		ArgumentNullException.ThrowIfNull(image);
		ErrorHelpers.ThrowIfBadViewPort(image.Width, image.Height, x, y, TileDimension, TileDimension);

		int stride = image.BytesPerRow;
		_bytesPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image.GetPixelsReference(), x, y, stride);
	}

	/// <summary>
	/// Creates a new viewport over the tile at the relative coordinates inside the image viewport.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the viewport escapes the bounds of the source.</exception>
	public IndexedTileViewport(IndexedBitmapViewport image, int x, int y) {
		ErrorHelpers.ThrowIfBadViewPort(image.Width, image.Height, x, y, TileDimension, TileDimension);

		int stride = image._bytesPerRow;
		_bytesPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref image._reference, x, y, stride);
	}

	internal IndexedTileViewport(ref byte pointer, int x, int y, int stride) {
		_bytesPerRow = stride;
		_reference = ref MemoryHelpers.GetPixelRef(ref pointer, x, y, stride);
	}

	/// <summary>
	/// Gets the color at the relative offset inside this tile.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the selected pixel is outside of the viewport.</exception>
	public ref byte this[int i] {
		get {
			if ((uint) i >= IndexedTile.Length) {
				ErrorHelpers.ThrowOutOfRangeArgument(ErrorHelpers.PixelOutOfRange);
			}
			return ref UnsafeGetColor(i & 0x7, i >> 3);
		}
	}

	/// <summary>
	/// Gets the color at the relative coordinates inside this tile.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the selected pixel is outside of the viewport.</exception>
	public ref byte this[int x, int y] {
		get {
			if ((uint) x >= TileDimension || (uint) y >= TileDimension) {
				ErrorHelpers.ThrowOutOfRangeArgument(ErrorHelpers.PixelOutOfRange);
			}
			return ref UnsafeGetColor(x, y);
		}
	}

	/// <summary>
	/// Captures a single row of pixels from this viewport.
	/// </summary>
	/// <param name="row">The row to get [0..7].</param>
	/// <returns>A new span.</returns>
	/// <exception cref="ArgumentOutOfRangeException">If the selected row is outside of the viewport.</exception>
	public Span<byte> GetRow(int row) {
		if ((uint) row >= 8) {
			ErrorHelpers.ThrowOutOfRangeArgument(nameof(row), ErrorHelpers.BadTileRow);
		}

		return MemoryMarshal.CreateSpan(ref UnsafeGetColor(0, row), 8);
	}

	/// <inheritdoc cref="IndexedTile.IsEmpty"/>
	public readonly bool IsEmpty() {
		ref ulong read = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in _reference));

		ulong ztest = Unsafe.AddByteOffset(ref read, _bytesPerRow * 0)
			| Unsafe.AddByteOffset(ref read, _bytesPerRow * 1)
			| Unsafe.AddByteOffset(ref read, _bytesPerRow * 2)
			| Unsafe.AddByteOffset(ref read, _bytesPerRow * 3)
			| Unsafe.AddByteOffset(ref read, _bytesPerRow * 4)
			| Unsafe.AddByteOffset(ref read, _bytesPerRow * 5)
			| Unsafe.AddByteOffset(ref read, _bytesPerRow * 6)
			| Unsafe.AddByteOffset(ref read, _bytesPerRow * 7);

		return ztest is 0;
	}

	/// <summary>
	/// No bounds check method for internal use.
	/// </summary>
	internal ref byte UnsafeGetColor(int x, int y) {
		return ref MemoryHelpers.GetPixelRef(ref _reference, x, y, _bytesPerRow);
	}

	/// <inheritdoc cref="IndexedTile.ConvertTo{TFormat}(ref TFormat)"/>
	public void ConvertTo<TFormat>(scoped ref TFormat destination)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		ConversionType mode = TFormat.ConversionType;

		if (mode is ConversionType.RawCopy) {
			Debug.Assert(TFormat.Length == IndexedTile.Length);
			IndexedTile.CopyToScalar(in _reference, _bytesPerRow, ref destination.Reference, IndexedTile.TileDimension);
			return;
		}

		if (mode is ConversionType.Snes3bpp) {
			Asserts.IsSnes3bpp(typeof(TFormat));
			Snes3bppTile.IndexedToSnes3bpp(in _reference, ref destination.Reference, _bytesPerRow);
			return;
		}

		if (mode is ConversionType.PackedLE) {
			Asserts.IsPackedFormat(typeof(TFormat));
			ConversionHelpers.IndexedToPacked(in _reference, ref destination.Reference, _bytesPerRow, TFormat.BitsPerPixel);
			return;
		}

		if (mode is ConversionType.SnesInterleaved or ConversionType.NesInterleaved or ConversionType.LinearPlanar) {
			Asserts.IsGenericPlanar(typeof(TFormat));
			ConversionHelpers.IndexedToPlanar(in _reference, ref destination.Reference, _bytesPerRow, TFormat.BitsPerPixel, mode);
			return;
		}
	}

	/// <inheritdoc cref="IndexedTile.CopyTo(GraphicsTile)"/>
	public void CopyTo(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		IndexedTile.CopyToScalar(in _reference, _bytesPerRow, ref destination._indexedTile._tileData, IndexedTile.TileDimension);
	}

	/// <inheritdoc cref="IndexedTile.CopyTo(ref IndexedTile)"/>
	public void CopyTo(scoped ref IndexedTile destination) {
		IndexedTile.CopyToScalar(in _reference, _bytesPerRow, ref destination._tileData, IndexedTile.TileDimension);
	}

	/// <inheritdoc cref="IndexedTile.CopyTo(IndexedTileViewport)"/>
	public void CopyTo(scoped IndexedTileViewport destination) {
		IndexedTile.CopyToScalar(in _reference, _bytesPerRow, ref destination._reference, destination._bytesPerRow);
	}

	/// <inheritdoc cref="IndexedTile.Equals(ref readonly IndexedTile)"/>
	public bool Equals(scoped ref readonly IndexedTile other) {
		return other.Equals(this);
	}

	/// <inheritdoc cref="IndexedTile.Equals(ref readonly IndexedTile)"/>
	public bool Equals(GraphicsTile other) {
		ArgumentNullException.ThrowIfNull(other);
		return other._indexedTile.Equals(this);
	}

	/// <inheritdoc cref="IndexedTile.Equals(ref readonly IndexedTile)"/>
	public bool Equals(scoped IndexedTileViewport other) {
		return IndexedTile.TilesAreEqualScalar(in _reference, _bytesPerRow, in other._reference, other._bytesPerRow);
	}

	/// <inheritdoc cref="IndexedTile.Matches(ref readonly IndexedTile)"/>
	public MatchResult Matches(scoped ref readonly IndexedTile other) {
		return other.Matches(this);
	}

	/// <inheritdoc cref="IndexedTile.Matches(ref readonly IndexedTile)"/>
	public MatchResult Matches(GraphicsTile other) {
		ArgumentNullException.ThrowIfNull(other);
		return other._indexedTile.Matches(this);
	}

	/// <inheritdoc cref="IndexedTile.Matches(ref readonly IndexedTile)"/>
	public MatchResult Matches(scoped IndexedTileViewport other) {
		return Equals(other) ? MatchResult.Same : MatchResult.None;
	}

	/// <inheritdoc cref="IndexedTile.CopyToFlipped(ref IndexedTile, TileFlip)"/>
	public void CopyToFlipped(scoped IndexedTileViewport destination, TileFlip flip) {
		IndexedTile.CopyToFlippedScalar(in _reference, _bytesPerRow, ref destination._reference, destination._bytesPerRow, flip);
	}

	/// <inheritdoc cref="IndexedTile.CopyToHFlipped(ref IndexedTile)"/>
	public void CopyToHFlipped(scoped IndexedTileViewport destination) {
		IndexedTile.CopyToHFlipped(in _reference, _bytesPerRow, ref destination._reference, destination._bytesPerRow);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVFlipped(ref IndexedTile)"/>
	public void CopyToVFlipped(scoped IndexedTileViewport destination) {
		IndexedTile.CopyToVFlipped(in _reference, _bytesPerRow, ref destination._reference, destination._bytesPerRow);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVHFlipped(ref IndexedTile)"/>
	public void CopyToVHFlipped(scoped IndexedTileViewport destination) {
		IndexedTile.CopyToVHFlipped(in _reference, _bytesPerRow, ref destination._reference, destination._bytesPerRow);
	}

	/// <inheritdoc cref="IndexedTile.CopyToFlipped(ref IndexedTile, TileFlip)"/>
	public void CopyToFlipped(scoped ref IndexedTile destination, TileFlip flip) {
		IndexedTile.CopyToFlippedScalar(in _reference, _bytesPerRow, ref destination._tileData, IndexedTile.TileDimension, flip);
	}

	/// <inheritdoc cref="IndexedTile.CopyToHFlipped(ref IndexedTile)"/>
	public void CopyToHFlipped(scoped ref IndexedTile destination) {
		IndexedTile.CopyToHFlipped(in _reference, _bytesPerRow, ref destination._tileData, IndexedTile.TileDimension);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVFlipped(ref IndexedTile)"/>
	public void CopyToVFlipped(scoped ref IndexedTile destination) {
		IndexedTile.CopyToVFlipped(in _reference, _bytesPerRow, ref destination._tileData, IndexedTile.TileDimension);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVHFlipped(ref IndexedTile)"/>
	public void CopyToVHFlipped(scoped ref IndexedTile destination) {
		IndexedTile.CopyToVHFlipped(in _reference, _bytesPerRow, ref destination._tileData, IndexedTile.TileDimension);
	}

	/// <inheritdoc cref="IndexedTile.CopyToFlipped(ref IndexedTile, TileFlip)"/>
	public void CopyToFlipped(GraphicsTile destination, TileFlip flip) {
		ArgumentNullException.ThrowIfNull(destination);
		IndexedTile.CopyToFlippedScalar(in _reference, _bytesPerRow, ref destination._indexedTile._tileData, IndexedTile.TileDimension, flip);
	}

	/// <inheritdoc cref="IndexedTile.CopyToHFlipped(ref IndexedTile)"/>
	public void CopyToHFlipped(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		IndexedTile.CopyToHFlipped(in _reference, _bytesPerRow, ref destination._indexedTile._tileData, IndexedTile.TileDimension);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVFlipped(ref IndexedTile)"/>
	public void CopyToVFlipped(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		IndexedTile.CopyToVFlipped(in _reference, _bytesPerRow, ref destination._indexedTile._tileData, IndexedTile.TileDimension);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVHFlipped(ref IndexedTile)"/>
	public void CopyToVHFlipped(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		IndexedTile.CopyToVHFlipped(in _reference, _bytesPerRow, ref destination._indexedTile._tileData, IndexedTile.TileDimension);
	}

	/// <summary>
	/// Returns an enumerator for this viewport.
	/// </summary>
	public Enumerator GetEnumerator() => new(this);

	/// <summary>
	/// Enumerator for a viewport.
	/// </summary>
	public ref struct Enumerator {
		private readonly IndexedTileViewport _viewport;
		private int _x = -1;
		private int _y = 0;

		internal Enumerator(IndexedTileViewport viewport) {
			_viewport = viewport;
		}

		/// <inheritdoc cref="System.Collections.IEnumerator.Current"/>
		public readonly ref byte Current {
			get => ref _viewport.UnsafeGetColor(_x, _y);
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
