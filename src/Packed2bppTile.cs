using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Sheeter;

/// <summary>
/// Represents a block of data containing a tile encoded at 2-bits-per-pixel
/// with 4 pixels encoded in each byte.
/// </summary>
/// <inheritdoc cref="IndexedTile" path="//remarks"/>
[InlineArray(Length)]
public struct Packed2bppTile : IConvertibleTile<Packed2bppTile> {
	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 2;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<Packed2bppTile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	static int ITileData<Packed2bppTile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<Packed2bppTile>.Length => Length;
	static ConversionType IConvertibleTile<Packed2bppTile>.ConversionType => ConversionType.PackedLE;

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped ref IndexedTile destination) {
		ConversionHelpers.PackedToIndexed(in _tileData, ref destination._tileData, IndexedTile.TileDimension, BitsPerPixel);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		ToIndexedTile(ref destination._indexedTile);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped IndexedTileViewport destination) {
		ConversionHelpers.PackedToIndexed(in _tileData, ref destination._reference, destination._bytesPerRow, BitsPerPixel);
	}

	/// <inheritdoc/>
	public readonly void CopyTo(scoped ref Packed2bppTile destination) {
		destination = this;
	}

	/// <summary>
	/// Converts an untiled block of indixed data to packed indices.
	/// </summary>
	/// <param name="source">The indexed color data to convert.</param>
	/// <param name="destination">The destination of the converted data.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> is too short to contain the converted data.</exception>
	public static void PackUntiled(scoped ReadOnlySpan<byte> source, scoped Span<byte> destination) {
		ConversionHelpers.PackUntiled<Packed2bppTile>(source, destination);
	}

	/// <inheritdoc cref="PackUntiled(ReadOnlySpan{byte}, Span{byte})" path="//param[@name='source']|//summary"/>
	/// <returns>A byte array containing the converted data.</returns>
	public static byte[] PackUntiled(scoped ReadOnlySpan<byte> source) {
		return ConversionHelpers.PackUntiled<Packed2bppTile>(source);
	}

	/// <summary>
	/// Converts an untiled block of packed indices to unpacked indexed data.
	/// </summary>
	/// <param name="source">The packed color data to convert.</param>
	/// <param name="destination">The destination of the converted data.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> is too short to contain the converted data.</exception>
	public static void SpreadUntiled(scoped ReadOnlySpan<byte> source, scoped Span<byte> destination) {
		ConversionHelpers.SpreadUntiled<Packed2bppTile>(source, destination);
	}

	/// <inheritdoc cref="SpreadUntiled(ReadOnlySpan{byte}, Span{byte})" path="//param[@name='source']|//summary"/>
	/// <returns>A byte array containing the converted data.</returns>
	public static byte[] SpreadUntiled(scoped ReadOnlySpan<byte> source) {
		return ConversionHelpers.SpreadUntiled<Packed2bppTile>(source);
	}

	/// <inheritdoc cref="Packed1bppTile.Widen(ref Packed2bppTile)"/>
	public readonly void Widen(scoped ref Packed4bppTile destination) {
		ulong tmplo = BinaryPrimitives.ReadUInt64LittleEndian(this);
		ulong tmphi = BinaryPrimitives.ReadUInt64LittleEndian(this[8..]);

		BinaryPrimitives.WriteUInt64LittleEndian(destination, Spread((uint) (tmplo >> 0)));
		BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], Spread((uint) (tmplo >> 32)));
		BinaryPrimitives.WriteUInt64LittleEndian(destination[16..], Spread((uint) (tmphi >> 0)));
		BinaryPrimitives.WriteUInt64LittleEndian(destination[24..], Spread((uint) (tmphi >> 32)));

		static ulong Spread(ulong value) {
			value = (value | (value << 16)) & ConversionHelpers.SpreadMask2u16;
			value = (value | (value << 8)) & ConversionHelpers.SpreadMask4u8;
			value = (value | (value << 4)) & ConversionHelpers.SpreadMask8u4;
			return (value | (value << 2)) & ConversionHelpers.SpreadMask16u2;
		}
	}

	/// <summary>
	/// Widens a collection of tiles to 4 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Packed4bppTile[] WidenAllTo4bpp(ReadOnlySpan<Packed2bppTile> source) {
		Packed4bppTile[] ret = GC.AllocateUninitializedArray<Packed4bppTile>(source.Length);
		WidenAllTo4bpp(source, ret);
		return ret;
	}

	/// <summary>
	/// Widens a collection of tiles to 4 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <param name="destination">The collection of tiles to write to.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> has fewer elements than <paramref name="source"/>.</exception>
	public static void WidenAllTo4bpp(ReadOnlySpan<Packed2bppTile> source, Span<Packed4bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Packed2bppTile tile in source) {
			tile.Widen(ref destination[i++]);
		}
	}
}
