using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;
using Sheeter.Snes;

using static System.Buffers.Binary.BinaryPrimitives;

namespace Sheeter;

/// <summary>
/// Represents a block of data containing a tile of 8-bits-per-pixel indexed colors.
/// </summary>
/// <remarks>
/// This is a large struct that facilitates high-performance code.
/// Avoid creating instances of this type outside of arrays and
/// prefer passing instances around by reference using <see langword="ref"/> or <see langword="in"/>.
/// </remarks>
[InlineArray(Length)]
public struct IndexedTile : ITileData<IndexedTile> {
	internal const int TileDimension = 8;

	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 8;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<IndexedTile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	/// <summary>
	/// Internal helper for row-by-row comparisons.
	/// </summary>
	[UnscopedRef]
	internal readonly ReadOnlySpan<ulong> AsSpanUInt64() => MemoryMarshal.Cast<byte, ulong>(this);

	static int ITileData<IndexedTile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<IndexedTile>.Length => Length;

	/// <summary>
	/// Gets or sets the pixel at the given position in this tile.
	/// </summary>
	/// <param name="x">The X coordinate of the pixel to select [0..7].</param>
	/// <param name="y">The Y coordinate of the pixel to select [0..7].</param>
	/// <exception cref="IndexOutOfRangeException">If the selected pixel is outside of the tile.</exception>
	public byte this[int x, int y] {
		readonly get => this[x + (8 * y)];
		set => this[x + (8 * y)] = value;
	}

	/// <summary>
	/// Tests a tile for only containing transparent pixels.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the tile only contains index 0 pixels; otherwise, <see langword="false"/>.
	/// </returns>
	public readonly bool IsEmpty() {
		if (Vector512.IsHardwareAccelerated) {
			return Vector512.EqualsAll(Vector512.Create<byte>(this), Vector512<byte>.Zero);
		
		} else if (Vector256.IsHardwareAccelerated) {
			Vector256<byte> v1 = Vector256.Create<byte>(this);
			Vector256<byte> v2 = Vector256.Create(this[32..]);
			v1 |= v2;
			return Vector256.EqualsAll(v1, Vector256<byte>.Zero);
		
		} else {
			ReadOnlySpan<ulong> tmp = AsSpanUInt64();

			ulong ztest = tmp[0] | tmp[1];
			ztest |= tmp[2] | tmp[3];
			ztest |= tmp[4] | tmp[5];
			ztest |= tmp[6] | tmp[7];

			return ztest is 0;
		}
	}

	/// <summary>
	/// Tests the indexed data in this tile against <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The tile to compare to.</param>
	/// <returns><see langword="true"/> if the two tiles are identical; otherwise <see langword="false"/>.</returns>
	public readonly bool Equals(scoped ref readonly IndexedTile other) {
		return MemoryExtensions.SequenceEqual<byte>(this, other);
	}

	/// <inheritdoc cref="Equals(ref readonly IndexedTile)"/>
	public readonly bool Equals(GraphicsTile other) {
		ArgumentNullException.ThrowIfNull(other);
		return Equals(in other._indexedTile);
	}

	/// <inheritdoc cref="Equals(ref readonly IndexedTile)"/>
	public readonly bool Equals(scoped IndexedTileViewport other) {
		return TilesAreEqualScalar(in _tileData, TileDimension, in other._reference, other._bytesPerRow);
	}

	/// <summary>
	/// Determines whether two tiles match under some transformation.
	/// </summary>
	/// <param name="other">The tile to compare to.</param>
	/// <returns>A <see cref="MatchResult"/> enum containing the result.</returns>
	public readonly MatchResult Matches(scoped ref readonly IndexedTile other) {
		// check exact match first, since it's (ideally) vectorized
		if (Equals(in other)) {
			return MatchResult.Same;
		}

		ReadOnlySpan<ulong> readA = AsSpanUInt64();
		ReadOnlySpan<ulong> readB = other.AsSpanUInt64();

		if (readA[0] == readB[7] &&
			readA[1] == readB[6] &&
			readA[2] == readB[5] &&
			readA[3] == readB[4] &&
			readA[4] == readB[3] &&
			readA[5] == readB[2] &&
			readA[6] == readB[1] &&
			readA[7] == readB[0]) {
			return MatchResult.VFlipped;
		}

		if (readA[0] == ReverseEndianness(readB[0]) &&
			readA[1] == ReverseEndianness(readB[1]) &&
			readA[2] == ReverseEndianness(readB[2]) &&
			readA[3] == ReverseEndianness(readB[3]) &&
			readA[4] == ReverseEndianness(readB[4]) &&
			readA[5] == ReverseEndianness(readB[5]) &&
			readA[6] == ReverseEndianness(readB[6]) &&
			readA[7] == ReverseEndianness(readB[7])) {
			return MatchResult.HFlipped;
		}

		if (readA[0] == ReverseEndianness(readB[7]) &&
			readA[1] == ReverseEndianness(readB[6]) &&
			readA[2] == ReverseEndianness(readB[5]) &&
			readA[3] == ReverseEndianness(readB[4]) &&
			readA[4] == ReverseEndianness(readB[3]) &&
			readA[5] == ReverseEndianness(readB[2]) &&
			readA[6] == ReverseEndianness(readB[1]) &&
			readA[7] == ReverseEndianness(readB[0])) {
			return MatchResult.VHFlipped;
		}

		return MatchResult.None;
	}

	/// <inheritdoc cref="Matches(ref readonly IndexedTile)"/>
	public readonly MatchResult Matches(scoped IndexedTileViewport other) {
		return MatchesScalar(in _tileData, TileDimension, in other._reference, other._bytesPerRow);
	}

	/// <inheritdoc cref="Matches(ref readonly IndexedTile)"/>
	public readonly MatchResult Matches(GraphicsTile other) {
		ArgumentNullException.ThrowIfNull(other);
		return Matches(in other._indexedTile);
	}

	/// <summary>
	/// Converts this indexed tile to <typeparamref name="TFormat"/> data
	/// and writes it to <paramref name="destination"/>.
	/// </summary>
	/// <typeparam name="TFormat">The tile format to convert to.</typeparam>
	/// <param name="destination">A managed reference to the tile to write data to.</param>
	public readonly void ConvertTo<TFormat>(scoped ref TFormat destination)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		ConversionType mode = TFormat.ConversionType;

		if (mode is ConversionType.RawCopy) {
			Debug.Assert(TFormat.Length == Length);
			destination = Unsafe.BitCast<IndexedTile, TFormat>(this);
			return;
		}

		if (mode is ConversionType.Snes3bpp) {
			Asserts.IsSnes3bpp(typeof(TFormat));
			Snes3bppTile.IndexedToSnes3bpp(in _tileData, ref destination.Reference, TileDimension);
			return;
		}

		if (mode is ConversionType.PackedLE) {
			Asserts.IsPackedFormat(typeof(TFormat));
			ConversionHelpers.IndexedToPacked(in _tileData, ref destination.Reference, TileDimension, TFormat.BitsPerPixel);
			return;
		}

		if (mode is ConversionType.SnesInterleaved or ConversionType.NesInterleaved or ConversionType.LinearPlanar) {
			Asserts.IsGenericPlanar(typeof(TFormat));
			ConversionHelpers.IndexedToPlanar(in _tileData, ref destination.Reference, TileDimension, TFormat.BitsPerPixel, mode);
			return;
		}
	}

	/// <summary>
	/// Converts all indexed tile data in a collection to tile data in a new collection.
	/// </summary>
	/// <inheritdoc cref="ConvertAllTo{TFormat}(ReadOnlySpan{IndexedTile}, Span{TFormat})" path="//param|//typeparam"/>
	/// <returns>A new array with the converted tile data.</returns>
	public static TFormat[] ConvertAllTo<TFormat>(scoped ReadOnlySpan<IndexedTile> source)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		TFormat[] ret = GC.AllocateUninitializedArray<TFormat>(source.Length);
		ConvertAllTilesHelper(source, ret);
		return ret;
	}

	/// <summary>
	/// Converts all indexed tile data in a collection to tile data in place inside <paramref name="destination"/>.
	/// </summary>
	/// <typeparam name="TFormat">The tile format to convert to.</typeparam>
	/// <param name="source">The collection of tiles to convert.</param>
	/// <param name="destination">The destination of the converted data.</param>
	/// <exception cref="ArgumentException">
	/// If <paramref name="destination"/> does not have at least as many elements as <paramref name="source"/>.
	/// </exception>
	public static void ConvertAllTo<TFormat>(scoped ReadOnlySpan<IndexedTile> source, scoped Span<TFormat> destination)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);
		ConvertAllTilesHelper(source, destination);
	}

	internal static void ConvertAllTilesHelper<TFormat>(scoped ReadOnlySpan<IndexedTile> source, scoped Span<TFormat> destination)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		ConversionType mode = TFormat.ConversionType;

		if (mode is ConversionType.RawCopy) {
			// Faster to copy literally everything together instead of treating tiles as separate
			source.AsBytes().CopyTo(destination.AsBytes());

		} else if (mode is ConversionType.PackedLE) {
			// Faster to just treat the data as a large untiled block
			ConversionHelpers.PackUntiled<TFormat>(source.AsBytes(), destination.AsBytes());

		} else {
			int i = 0;
			foreach (ref readonly IndexedTile block in source) {
				block.ConvertTo(ref destination[i++]);
			}
		}
	}

	/// <inheritdoc/>
	public readonly void CopyTo(scoped ref IndexedTile destination) {
		destination = this;
	}

	/// <inheritdoc cref="ITileData{TSelf}.CopyTo(ref TSelf)"/>
	public readonly void CopyTo(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		destination._indexedTile = this;
	}

	/// <inheritdoc cref="ITileData{TSelf}.CopyTo(ref TSelf)"/>
	public readonly void CopyTo(scoped IndexedTileViewport destination) {
		CopyToScalar(in _tileData, Length, ref destination._reference, destination._bytesPerRow);
	}

	/// <summary>
	/// Copies the data in this instance to <paramref name="destination"/>
	/// such that the tile is flipped.
	/// </summary>
	/// <param name="destination">The destination of the flipped data.</param>
	/// <param name="flip">The transformation to perform.</param>
	/// <exception cref="ArgumentException">If an invalid flip value is passed.</exception>
	public readonly void CopyToFlipped(scoped ref IndexedTile destination, TileFlip flip) {
		if (flip is TileFlip.HFlip) {
			CopyToHFlipped(ref destination);

		} else if (flip is TileFlip.VFlip) {
			CopyToVFlipped(ref destination);

		} else if (flip is TileFlip.VHFlip) {
			CopyToVHFlipped(ref destination);

		} else if (flip is TileFlip.None) {
			CopyTo(ref destination);

		} else {
			ErrorHelpers.ThrowBadArgument(ErrorHelpers.InvalidTileFlip);
		}
	}

	/// <summary>
	/// Copies the data in this instance to <paramref name="destination"/>
	/// such that the tile is flipped horizontally.
	/// </summary>
	/// <param name="destination">The destination of the flipped data.</param>
	public readonly void CopyToHFlipped(scoped ref IndexedTile destination) {
		CopyToHFlipped(in _tileData, TileDimension, ref destination._tileData, TileDimension);
	}

	/// <summary>
	/// Copies the data in this instance to <paramref name="destination"/>
	/// such that the tile is flipped vertically.
	/// </summary>
	/// <param name="destination">The destination of the flipped data.</param>
	public readonly void CopyToVFlipped(scoped ref IndexedTile destination) {
		CopyToVFlipped(in _tileData, TileDimension, ref destination._tileData, TileDimension);
	}

	/// <summary>
	/// Copies the data in this instance to <paramref name="destination"/>
	/// such that the tile is flipped vertically and horizontally.
	/// </summary>
	/// <param name="destination">The destination of the flipped data.</param>
	public readonly void CopyToVHFlipped(scoped ref IndexedTile destination) {
		CopyToVHFlipped(in _tileData, TileDimension, ref destination._tileData, TileDimension);
	}

	/// <inheritdoc cref="CopyToFlipped(ref IndexedTile, TileFlip)"/>
	public readonly void CopyToFlipped(scoped IndexedTileViewport destination, TileFlip flip) {
		CopyToFlippedScalar(in _tileData, TileDimension, ref destination._reference, destination._bytesPerRow, flip);
	}

	/// <inheritdoc cref="CopyToHFlipped(ref IndexedTile)"/>
	public readonly void CopyToHFlipped(scoped IndexedTileViewport destination) {
		CopyToHFlipped(in _tileData, TileDimension, ref destination._reference, destination._bytesPerRow);
	}

	/// <inheritdoc cref="CopyToVFlipped(ref IndexedTile)"/>
	public readonly void CopyToVFlipped(scoped IndexedTileViewport destination) {
		CopyToVFlipped(in _tileData, TileDimension, ref destination._reference, destination._bytesPerRow);
	}

	/// <inheritdoc cref="CopyToVHFlipped(ref IndexedTile)"/>
	public readonly void CopyToVHFlipped(scoped IndexedTileViewport destination) {
		CopyToVHFlipped(in _tileData, TileDimension, ref destination._reference, destination._bytesPerRow);
	}

	/// <inheritdoc cref="CopyToFlipped(ref IndexedTile, TileFlip)"/>
	public readonly void CopyToFlipped(GraphicsTile destination, TileFlip flip) {
		ArgumentNullException.ThrowIfNull(destination);
		CopyToFlipped(ref destination._indexedTile, flip);
	}

	/// <inheritdoc cref="CopyToHFlipped(ref IndexedTile)"/>
	public readonly void CopyToHFlipped(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		CopyToHFlipped(ref destination._indexedTile);
	}

	/// <inheritdoc cref="CopyToVFlipped(ref IndexedTile)"/>
	public readonly void CopyToVFlipped(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		CopyToVFlipped(ref destination._indexedTile);
	}

	/// <inheritdoc cref="CopyToVHFlipped(ref IndexedTile)"/>
	public readonly void CopyToVHFlipped(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		CopyToVHFlipped(ref destination._indexedTile);
	}

	/// <summary>
	/// Converts all tile data in a collection to indexed tile data in a new collection.
	/// </summary>
	/// <typeparam name="TFormat">The tile type to convert from.</typeparam>
	/// <param name="source">The collection of tiles to convert.</param>
	/// <returns>A new array of <see cref="IndexedTile"/> objects.</returns>
	public static IndexedTile[] ConvertAllToIndexed<TFormat>(scoped ReadOnlySpan<TFormat> source)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		IndexedTile[] ret = GC.AllocateUninitializedArray<IndexedTile>(source.Length);
		ConvertAllToIndexed(source, ret);
		return ret;
	}

	/// <summary>
	/// Converts all tile data in a collection to indexed tile data in place in <paramref name="destination"/>.
	/// </summary>
	/// <typeparam name="TFormat">The tile type to convert from.</typeparam>
	/// <param name="source">The collection of tiles to convert.</param>
	/// <param name="destination">The destination to convert the data to.</param>
	/// <exception cref="ArgumentException">
	/// If <paramref name="destination"/> is too short to contain every tile in <paramref name="source"/>.
	/// </exception>
	public static void ConvertAllToIndexed<TFormat>(scoped ReadOnlySpan<TFormat> source, scoped Span<IndexedTile> destination)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		ConversionType mode = TFormat.ConversionType;

		if (mode is ConversionType.RawCopy) {
			// Faster to copy literally everything together instead of treating tiles as separate
			source.AsBytes().CopyTo(destination.AsBytes());

		} else if (mode is ConversionType.PackedLE) {
			// Faster to just treat the data as a large untiled block
			ConversionHelpers.SpreadUntiled<TFormat>(source.AsBytes(), destination.AsBytes());

		} else {
			int i = 0;

			foreach (ref readonly TFormat block in source) {
				block.ToIndexedTile(ref destination[i++]);
			}
		}
	}

	internal static void CopyToFlippedScalar(scoped ref readonly byte source, int sourceStride, scoped ref byte dest, int destStride, TileFlip flip) {
		if (flip is TileFlip.HFlip) {
			CopyToHFlipped(in source, sourceStride, ref dest, destStride);

		} else if (flip is TileFlip.VFlip) {
			CopyToVFlipped(in source, sourceStride, ref dest, destStride);

		} else if (flip is TileFlip.VHFlip) {
			CopyToVHFlipped(in source, sourceStride, ref dest, destStride);

		} else if (flip is TileFlip.None) {
			CopyToScalar(in source, sourceStride, ref dest, destStride);

		} else {
			ErrorHelpers.ThrowBadArgument(ErrorHelpers.InvalidTileFlip);
		}
	}

	// Inlining because these helper routines have a lot of places where they're using constants
	/// <remarks>
	/// Scalar method for noncontiguous data blocks.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void CopyToScalar(scoped ref readonly byte source, int sourceStride, scoped ref byte dest, int destStride) {
		ref ulong dw = ref Unsafe.As<byte, ulong>(ref dest);
		ref ulong sw = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in source));

		ulong a, b;

		a = Unsafe.AddByteOffset(ref sw, 0 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 1 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 0 * destStride) = a;
		Unsafe.AddByteOffset(ref dw, 1 * destStride) = b;

		a = Unsafe.AddByteOffset(ref sw, 2 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 3 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 2 * destStride) = a;
		Unsafe.AddByteOffset(ref dw, 3 * destStride) = b;

		a = Unsafe.AddByteOffset(ref sw, 4 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 5 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 4 * destStride) = a;
		Unsafe.AddByteOffset(ref dw, 5 * destStride) = b;

		a = Unsafe.AddByteOffset(ref sw, 6 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 7 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 6 * destStride) = a;
		Unsafe.AddByteOffset(ref dw, 7 * destStride) = b;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void CopyToHFlipped(scoped ref readonly byte source, int sourceStride, scoped ref byte dest, int destStride) {
		ref ulong dw = ref Unsafe.As<byte, ulong>(ref dest);
		ref ulong sw = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in source));

		ulong a, b;

		a = Unsafe.AddByteOffset(ref sw, 0 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 1 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 0 * destStride) = ReverseEndianness(a);
		Unsafe.AddByteOffset(ref dw, 1 * destStride) = ReverseEndianness(b);

		a = Unsafe.AddByteOffset(ref sw, 2 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 3 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 2 * destStride) = ReverseEndianness(a);
		Unsafe.AddByteOffset(ref dw, 3 * destStride) = ReverseEndianness(b);

		a = Unsafe.AddByteOffset(ref sw, 4 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 5 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 4 * destStride) = ReverseEndianness(a);
		Unsafe.AddByteOffset(ref dw, 5 * destStride) = ReverseEndianness(b);

		a = Unsafe.AddByteOffset(ref sw, 6 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 7 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 6 * destStride) = ReverseEndianness(a);
		Unsafe.AddByteOffset(ref dw, 7 * destStride) = ReverseEndianness(b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void CopyToVFlipped(scoped ref readonly byte source, int sourceStride, scoped ref byte dest, int destStride) {
		ref ulong dw = ref Unsafe.As<byte, ulong>(ref dest);
		ref ulong sw = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in source));

		ulong a, b; // by writing it this way, we can safely allow a tile to flip itself

		a = Unsafe.AddByteOffset(ref sw, 7 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 0 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 0 * destStride) = a;
		Unsafe.AddByteOffset(ref dw, 7 * destStride) = b;

		a = Unsafe.AddByteOffset(ref sw, 6 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 1 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 1 * destStride) = a;
		Unsafe.AddByteOffset(ref dw, 6 * destStride) = b;

		a = Unsafe.AddByteOffset(ref sw, 5 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 2 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 2 * destStride) = a;
		Unsafe.AddByteOffset(ref dw, 5 * destStride) = b;

		a = Unsafe.AddByteOffset(ref sw, 4 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 3 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 3 * destStride) = a;
		Unsafe.AddByteOffset(ref dw, 4 * destStride) = b;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void CopyToVHFlipped(scoped ref readonly byte source, int sourceStride, scoped ref byte dest, int destStride) {
		ref ulong dw = ref Unsafe.As<byte, ulong>(ref dest);
		ref ulong sw = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in source));

		ulong a, b; // by writing it this way, we can safely allow a tile to flip itself

		a = Unsafe.AddByteOffset(ref sw, 7 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 0 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 0 * destStride) = ReverseEndianness(a);
		Unsafe.AddByteOffset(ref dw, 7 * destStride) = ReverseEndianness(b);

		a = Unsafe.AddByteOffset(ref sw, 6 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 1 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 1 * destStride) = ReverseEndianness(a);
		Unsafe.AddByteOffset(ref dw, 6 * destStride) = ReverseEndianness(b);

		a = Unsafe.AddByteOffset(ref sw, 5 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 2 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 2 * destStride) = ReverseEndianness(a);
		Unsafe.AddByteOffset(ref dw, 5 * destStride) = ReverseEndianness(b);

		a = Unsafe.AddByteOffset(ref sw, 4 * sourceStride);
		b = Unsafe.AddByteOffset(ref sw, 3 * sourceStride);
		Unsafe.AddByteOffset(ref dw, 3 * destStride) = ReverseEndianness(a);
		Unsafe.AddByteOffset(ref dw, 4 * destStride) = ReverseEndianness(b);
	}

	/// <summary>
	/// Scalar method for non-contiguous data.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TilesAreEqualScalar(scoped ref readonly byte source, int sourceStride, scoped ref readonly byte dest, int destStride) {
		ref ulong readA = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in dest));
		ref ulong readB = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in source));

		return
			Unsafe.AddByteOffset(ref readA, sourceStride * 0) == Unsafe.AddByteOffset(ref readB, destStride * 0) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 1) == Unsafe.AddByteOffset(ref readB, destStride * 1) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 2) == Unsafe.AddByteOffset(ref readB, destStride * 2) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 3) == Unsafe.AddByteOffset(ref readB, destStride * 3) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 4) == Unsafe.AddByteOffset(ref readB, destStride * 4) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 5) == Unsafe.AddByteOffset(ref readB, destStride * 5) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 6) == Unsafe.AddByteOffset(ref readB, destStride * 6) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 7) == Unsafe.AddByteOffset(ref readB, destStride * 7);
	}

	/// <summary>
	/// Scalar method for non-contiguous data.
	/// </summary>
	internal static MatchResult MatchesScalar(scoped ref readonly byte source, int sourceStride, scoped ref readonly byte dest, int destStride) {
		ref ulong readA = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in dest));
		ref ulong readB = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in source));

		if (Unsafe.AddByteOffset(ref readA, sourceStride * 0) == Unsafe.AddByteOffset(ref readB, destStride * 0) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 1) == Unsafe.AddByteOffset(ref readB, destStride * 1) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 2) == Unsafe.AddByteOffset(ref readB, destStride * 2) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 3) == Unsafe.AddByteOffset(ref readB, destStride * 3) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 4) == Unsafe.AddByteOffset(ref readB, destStride * 4) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 5) == Unsafe.AddByteOffset(ref readB, destStride * 5) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 6) == Unsafe.AddByteOffset(ref readB, destStride * 6) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 7) == Unsafe.AddByteOffset(ref readB, destStride * 7)) {
			return MatchResult.Same;
		}

		if (Unsafe.AddByteOffset(ref readA, sourceStride * 0) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 0)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 1) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 1)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 2) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 2)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 3) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 3)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 4) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 4)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 5) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 5)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 6) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 6)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 7) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 7))) {
			return MatchResult.HFlipped;
		}

		if (Unsafe.AddByteOffset(ref readA, sourceStride * 0) == Unsafe.AddByteOffset(ref readB, destStride * 7) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 1) == Unsafe.AddByteOffset(ref readB, destStride * 6) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 2) == Unsafe.AddByteOffset(ref readB, destStride * 5) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 3) == Unsafe.AddByteOffset(ref readB, destStride * 4) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 4) == Unsafe.AddByteOffset(ref readB, destStride * 3) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 5) == Unsafe.AddByteOffset(ref readB, destStride * 2) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 6) == Unsafe.AddByteOffset(ref readB, destStride * 1) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 7) == Unsafe.AddByteOffset(ref readB, destStride * 0)) {
			return MatchResult.VFlipped;
		}

		if (Unsafe.AddByteOffset(ref readA, sourceStride * 0) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 7)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 1) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 6)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 2) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 5)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 3) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 4)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 4) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 3)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 5) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 2)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 6) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 1)) &&
			Unsafe.AddByteOffset(ref readA, sourceStride * 7) == ReverseEndianness(Unsafe.AddByteOffset(ref readB, destStride * 0))) {
			return MatchResult.VHFlipped;
		}

		return MatchResult.None;
	}

	/// <summary>
	/// Returns a hashcode that is identical for all four reflections.
	/// </summary>
	internal readonly int GetInvariantHashCode() {
		ReadOnlySpan<ulong> aslong = AsSpanUInt64();

		ulong hash = HashOne(
			aslong[0],
			aslong[1],
			aslong[2],
			aslong[3],
			aslong[4],
			aslong[5],
			aslong[6],
			aslong[7]
		);

		// we need to add these together so that order is irrelevant to the final hash
		hash += HashOne(
			ReverseEndianness(aslong[0]),
			ReverseEndianness(aslong[1]),
			ReverseEndianness(aslong[2]),
			ReverseEndianness(aslong[3]),
			ReverseEndianness(aslong[4]),
			ReverseEndianness(aslong[5]),
			ReverseEndianness(aslong[6]),
			ReverseEndianness(aslong[7])
		);

		hash += HashOne(
			aslong[7],
			aslong[6],
			aslong[5],
			aslong[4],
			aslong[3],
			aslong[2],
			aslong[1],
			aslong[0]
		);

		hash += HashOne(
			ReverseEndianness(aslong[7]),
			ReverseEndianness(aslong[6]),
			ReverseEndianness(aslong[5]),
			ReverseEndianness(aslong[4]),
			ReverseEndianness(aslong[3]),
			ReverseEndianness(aslong[2]),
			ReverseEndianness(aslong[1]),
			ReverseEndianness(aslong[0])
		);

		return (int) (hash ^ (hash >> 32));

		// Implementation of rapidhash micro
		// Copyright 2025 Nicolas De Carli
		// https://github.com/Nicoshev/rapidhash
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ulong HashOne(ulong r0, ulong r1, ulong r2, ulong r3, ulong r4, ulong r5, ulong r6, ulong r7) {
			// default secrets
			//const ulong s0 = 0x2D358DCCAA6C78A5;
			const ulong s1 = 0x8BB84B93962EACC9;
			const ulong s2 = 0x4B33A62ED433D4A3;

			ulong seed = 0x422765567D8FBFD6; // mix(s2, s1);

			seed = Mix(r0 ^ s2, r1 ^ seed);
			seed = Mix(r2 ^ s2, r3 ^ seed);
			seed = Mix(r4 ^ s1, r5 ^ seed);

			return Mix(r7 ^ 64 ^ s1, r6 ^ seed);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static ulong Mix(ulong p1, ulong p2) {
				p1 = Math.BigMul(p1, p2, out p2); // mum
				return p1 ^ p2;
			}
		}
	}
}
