using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.Intrinsics;
using Sheeter.Snes;

namespace Sheeter.Nes;

/// <summary>
/// Represents a block of data containing a tile encoded at 2-bits-per-pixel
/// and interleaved by row per bit plane.
/// </summary>
/// <inheritdoc cref="IndexedTile" path="//remarks"/>
[InlineArray(Length)]
public struct Nes2bppTile : IConvertibleTile<Nes2bppTile> {
	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 2;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<Nes2bppTile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	static int ITileData<Nes2bppTile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<Nes2bppTile>.Length => Length;
	static ConversionType IConvertibleTile<Nes2bppTile>.ConversionType => ConversionType.NesInterleaved;

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped ref IndexedTile destination) {
		ConversionHelpers.PlanarToIndexed<Nes2bppTile>(in _tileData, ref destination._tileData);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		ToIndexedTile(ref destination._indexedTile);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped IndexedTileViewport destination) {
		ConversionHelpers.PlanarToIndexed<Nes2bppTile>(in _tileData, destination);
	}

	/// <inheritdoc/>
	public readonly void CopyTo(scoped ref Nes2bppTile destination) {
		destination = this;
	}

	/// <summary>
	/// Remapping vector.
	/// </summary>
	private static readonly Vector128<byte> NesToSnesShuffler =
		Vector128.Create((byte)
			0x0, 0x8, 0x1, 0x9, 0x2, 0xA, 0x3, 0xB,
			0x4, 0xC, 0x5, 0xD, 0x6, 0xE, 0x7, 0xF);

	/// <summary>
	/// Converts this tile to SNES 2BPP.
	/// </summary>
	/// <param name="destination">The destination of the converted data.</param>
	public readonly void ConvertToSnes2bpp(scoped ref Snes2bppTile destination) {
		if (Vector128.IsHardwareAccelerated) {
			Vector128<byte> v = Vector128.Create<byte>(this);
			Vector128.Shuffle(v, NesToSnesShuffler).CopyTo(destination);

		} else {
			ConvertToSnesScalar(ref destination);
		}
	}

	/// <summary>
	/// Scalar fallback.
	/// </summary>
	/// <inheritdoc cref="ConvertToSnes2bpp(ref Snes2bppTile)" path="//param"/>
	private readonly void ConvertToSnesScalar(scoped ref Snes2bppTile destination) {
		ulong rows0123 = BinaryPrimitives.ReadUInt64LittleEndian(this);
		ulong rows4567 = BinaryPrimitives.ReadUInt64LittleEndian(this[8..]);

		BinaryPrimitives.WriteUInt64LittleEndian(destination, Merge((uint) rows0123, (uint) rows4567));

		rows0123 >>= 32;
		rows4567 >>= 32;
		BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], Merge((uint) rows0123, (uint) rows4567));

		static ulong Merge(uint rows0123, uint rows4567) {
			ulong a0 = rows0123;
			ulong a1 = rows4567;

			a0 |= a0 << 16;
			a1 |= a1 << 16;

			a0 &= ConversionHelpers.SpreadMask2u16;
			a1 &= ConversionHelpers.SpreadMask2u16;

			a0 |= a0 << 8;
			a1 |= a1 << 8;

			a0 &= ConversionHelpers.SpreadMask4u8;
			a1 &= ConversionHelpers.SpreadMask4u8;

			return a0 | (a1 << 8);
		}
	}

	/// <summary>
	/// Converts a collection of NES 2bpp tiles to the SNES 2bpp format.
	/// </summary>
	/// <param name="source">The collection of tiles to convert.</param>
	/// <returns>A new array of tiles.</returns>
	public static Snes2bppTile[] ConvertAllToSnes2bpp(ReadOnlySpan<Nes2bppTile> source) {
		Snes2bppTile[] ret = GC.AllocateUninitializedArray<Snes2bppTile>(source.Length);
		ConvertAllToSnes2bpp(source, ret);
		return ret;
	}

	/// <summary>
	/// Converts a collection of NES 2bpp tiles to the SNES 2bpp format.
	/// </summary>
	/// <param name="source">The collection of tiles to convert.</param>
	/// <param name="destination">The collection of tiles to write to.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> has fewer elements than <paramref name="source"/>.</exception>
	public static void ConvertAllToSnes2bpp(ReadOnlySpan<Nes2bppTile> source, Span<Snes2bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Nes2bppTile tile in source) {
			tile.ConvertToSnes2bpp(ref destination[i++]);
		}
	}
}
