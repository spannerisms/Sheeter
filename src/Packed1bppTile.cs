using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Sheeter;

/// <summary>
/// Represents a block of data containing a tile encoded at 1-bit-per-pixel
/// with 8 pixels encoded in each byte.
/// </summary>
/// <inheritdoc cref="IndexedTile" path="//remarks"/>
[InlineArray(Length)]
public struct Packed1bppTile : IConvertibleTile<Packed1bppTile> {
	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 1;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<Packed1bppTile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	static int ITileData<Packed1bppTile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<Packed1bppTile>.Length => Length;
	static ConversionType IConvertibleTile<Packed1bppTile>.ConversionType => ConversionType.PackedLE;

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
	public readonly void CopyTo(scoped ref Packed1bppTile destination) {
		destination = this;
	}

	/// <inheritdoc cref="Packed2bppTile.PackUntiled(ReadOnlySpan{byte}, Span{byte})"/>
	public static void PackUntiled(scoped ReadOnlySpan<byte> source, scoped Span<byte> destination) {
		ConversionHelpers.PackUntiled<Packed1bppTile>(source, destination);
	}

	/// <inheritdoc cref="Packed2bppTile.PackUntiled(ReadOnlySpan{byte})"/>
	public static byte[] PackUntiled(scoped ReadOnlySpan<byte> source) {
		return ConversionHelpers.PackUntiled<Packed1bppTile>(source);
	}

	/// <inheritdoc cref="Packed2bppTile.SpreadUntiled(ReadOnlySpan{byte}, Span{byte})"/>
	public static void SpreadUntiled(scoped ReadOnlySpan<byte> source, scoped Span<byte> destination) {
		ConversionHelpers.SpreadUntiled<Packed1bppTile>(source, destination);
	}

	/// <inheritdoc cref="Packed2bppTile.SpreadUntiled(ReadOnlySpan{byte})"/>
	public static byte[] SpreadUntiled(scoped ReadOnlySpan<byte> source) {
		return ConversionHelpers.SpreadUntiled<Packed1bppTile>(source);
	}

	/// <summary>
	/// Widens this tile to a tile with more bits per pixel.
	/// </summary>
	/// <param name="destination">A managed pointer to the destination tile.</param>
	public readonly void Widen(scoped ref Packed2bppTile destination) {
		ulong tmp = BinaryPrimitives.ReadUInt64LittleEndian(this);

		BinaryPrimitives.WriteUInt64LittleEndian(destination, Spread((uint) tmp));
		BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], Spread(tmp >> 32));

		static ulong Spread(ulong value) {
			value = (value | (value << 16)) & ConversionHelpers.SpreadMask2u16;
			value = (value | (value << 8)) & ConversionHelpers.SpreadMask4u8;
			value = (value | (value << 4)) & ConversionHelpers.SpreadMask8u4;
			value = (value | (value << 2)) & ConversionHelpers.SpreadMask16u2;
			return (value | (value << 1)) & 0x5555555555555555;
		}
	}

	/// <inheritdoc cref="Widen(ref Packed2bppTile)"/>
	public readonly void Widen(scoped ref Packed4bppTile destination) {
		ulong tmp = BinaryPrimitives.ReadUInt64LittleEndian(this);

		BinaryPrimitives.WriteUInt64LittleEndian(destination, Spread((ushort) (tmp >> 0)));
		BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], Spread((ushort) (tmp >> 16)));
		BinaryPrimitives.WriteUInt64LittleEndian(destination[16..], Spread((ushort) (tmp >> 32)));
		BinaryPrimitives.WriteUInt64LittleEndian(destination[24..], Spread((ushort) (tmp >> 48)));

		static ulong Spread(ulong value) {
			value = (value | (value << 24)) & ConversionHelpers.SpreadMask2u8;
			value = (value | (value << 12)) & ConversionHelpers.SpreadMask4u4;
			value = (value | (value << 6)) & 0x0303030303030303;
			value = (value | (value << 3)) & 0x1111111111111111;
			return (value | (value << 1)) & 0x5555555555555555;
		}
	}

	/// <summary>
	/// Widens a collection of tiles to 2 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Packed2bppTile[] WidenAllTo2bpp(ReadOnlySpan<Packed1bppTile> source) {
		Packed2bppTile[] ret = GC.AllocateUninitializedArray<Packed2bppTile>(source.Length);
		WidenAllTo2bpp(source, ret);
		return ret;
	}

	/// <summary>
	/// Widens a collection of tiles to 2 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <param name="destination">The collection of tiles to write to.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> has fewer elements than <paramref name="source"/>.</exception>
	public static void WidenAllTo2bpp(ReadOnlySpan<Packed1bppTile> source, Span<Packed2bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Packed1bppTile tile in source) {
			tile.Widen(ref destination[i++]);
		}
	}

	/// <summary>
	/// Widens a collection of tiles to 4 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Packed4bppTile[] WidenAllTo4bpp(ReadOnlySpan<Packed1bppTile> source) {
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
	public static void WidenAllTo4bpp(ReadOnlySpan<Packed1bppTile> source, Span<Packed4bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Packed1bppTile tile in source) {
			tile.Widen(ref destination[i++]);
		}
	}
}
