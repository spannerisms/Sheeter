using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.Intrinsics;
using Sheeter.Nes;

namespace Sheeter.Snes;

/// <summary>
/// Represents a block of data containing a tile encoded at 2-bits-per-pixel
/// and interleaved by row per bit plane.
/// </summary>
/// <remarks>
/// <para>This type also works for GB and GBC.</para>
/// <inheritdoc cref="IndexedTile" path="//remarks"/>
/// </remarks>
[InlineArray(Length)]
public struct Snes2bppTile : IConvertibleTile<Snes2bppTile> {
	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 2;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<Snes2bppTile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	static int ITileData<Snes2bppTile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<Snes2bppTile>.Length => Length;
	static ConversionType IConvertibleTile<Snes2bppTile>.ConversionType => ConversionType.SnesInterleaved;

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped ref IndexedTile destination) {
		ConversionHelpers.PlanarToIndexed<Snes2bppTile>(in _tileData, ref destination._tileData);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		ToIndexedTile(ref destination._indexedTile);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped IndexedTileViewport destination) {
		ConversionHelpers.PlanarToIndexed<Snes2bppTile>(in _tileData, destination);
	}

	/// <inheritdoc/>
	public readonly void CopyTo(scoped ref Snes2bppTile destination) {
		destination = this;
	}

	/// <inheritdoc cref="Packed1bppTile.Widen(ref Packed2bppTile)"/>
	public readonly void Widen(scoped ref Snes3bppTile destination) {
		ConversionHelpers.WidenSnesHelper(in this, ref destination);
	}

	/// <inheritdoc cref="Packed1bppTile.Widen(ref Packed2bppTile)"/>
	public readonly void Widen(scoped ref Snes4bppTile destination) {
		ConversionHelpers.WidenSnesHelper(in this, ref destination);
	}

	/// <inheritdoc cref="Packed1bppTile.Widen(ref Packed2bppTile)"/>
	public readonly void Widen(scoped ref Snes8bppTile destination) {
		ConversionHelpers.WidenSnesHelper(in this, ref destination);
	}

	/// <summary>
	/// Widens a collection of tiles to 3 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Snes3bppTile[] WidenAllTo3bpp(ReadOnlySpan<Snes2bppTile> source) {
		Snes3bppTile[] ret = GC.AllocateUninitializedArray<Snes3bppTile>(source.Length);
		WidenAllTo3bpp(source, ret);
		return ret;
	}

	/// <summary>
	/// Widens a collection of tiles to 3 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <param name="destination">The collection of tiles to write to.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> has fewer elements than <paramref name="source"/>.</exception>
	public static void WidenAllTo3bpp(ReadOnlySpan<Snes2bppTile> source, Span<Snes3bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Snes2bppTile tile in source) {
			tile.Widen(ref destination[i++]);
		}
	}

	/// <summary>
	/// Widens a collection of tiles to 4 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Snes4bppTile[] WidenAllTo4bpp(ReadOnlySpan<Snes2bppTile> source) {
		Snes4bppTile[] ret = GC.AllocateUninitializedArray<Snes4bppTile>(source.Length);
		WidenAllTo4bpp(source, ret);
		return ret;
	}

	/// <summary>
	/// Widens a collection of tiles to 4 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <param name="destination">The collection of tiles to write to.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> has fewer elements than <paramref name="source"/>.</exception>
	public static void WidenAllTo4bpp(ReadOnlySpan<Snes2bppTile> source, Span<Snes4bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Snes2bppTile tile in source) {
			tile.Widen(ref destination[i++]);
		}
	}

	/// <summary>
	/// Widens a collection of tiles to 8 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Snes8bppTile[] WidenAllTo8bpp(ReadOnlySpan<Snes2bppTile> source) {
		Snes8bppTile[] ret = GC.AllocateUninitializedArray<Snes8bppTile>(source.Length);
		WidenAllTo8bpp(source, ret);
		return ret;
	}

	/// <summary>
	/// Widens a collection of tiles to 8 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <param name="destination">The collection of tiles to write to.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> has fewer elements than <paramref name="source"/>.</exception>
	public static void WidenAllTo8bpp(ReadOnlySpan<Snes2bppTile> source, Span<Snes8bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Snes2bppTile tile in source) {
			tile.Widen(ref destination[i++]);
		}
	}

	/// <summary>
	/// Remapping vector.
	/// </summary>
	private static readonly Vector128<byte> SnesToNesShuffler =
		Vector128.Create((byte)
			0x0, 0x2, 0x4, 0x6, 0x8, 0xA, 0xC, 0xE,
			0x1, 0x3, 0x5, 0x7, 0x9, 0xB, 0xD, 0xF);

	/// <summary>
	/// Converts this tile to NES 2BPP.
	/// </summary>
	/// <param name="destination">The destination of the converted data.</param>
	public readonly void ConvertToNes2bpp(scoped ref Nes2bppTile destination) {
		if (Vector128.IsHardwareAccelerated) {
			Vector128<byte> v = Vector128.Create<byte>(this);
			Vector128.Shuffle(v, SnesToNesShuffler).CopyTo(destination);

		} else {
			ConvertToNesScalar(ref destination);
		}
	}

	/// <summary>
	/// Scalar fallback.
	/// </summary>
	/// <inheritdoc cref="ConvertToNes2bpp(ref Nes2bppTile)" path="//param"/>
	private readonly void ConvertToNesScalar(scoped ref Nes2bppTile destination) {
		ulong evens = BinaryPrimitives.ReadUInt64LittleEndian(this);
		ulong odds = BinaryPrimitives.ReadUInt64LittleEndian(this[8..]);

		var (a1, b1) = Split(evens);
		var (a2, b2) = Split(odds);

		BinaryPrimitives.WriteUInt64LittleEndian(destination, a1 | ((ulong) a2 << 32));
		BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], b1 | ((ulong) b2 << 32));

		static (uint, uint) Split(ulong rows) {
			ulong ret1 = rows & ConversionHelpers.SpreadMask4u8;
			ulong ret2 = (rows >> 8) & ConversionHelpers.SpreadMask4u8;

			ret1 |= ret1 >> 8;
			ret2 |= ret2 >> 8;

			ret1 &= ConversionHelpers.SpreadMask2u16;
			ret2 &= ConversionHelpers.SpreadMask2u16;

			ret1 |= ret1 >> 16;
			ret2 |= ret2 >> 16;

			return ((uint) ret1, (uint) ret2);
		}
	}

	/// <summary>
	/// Converts a collection of SNES 2bpp tiles to the NES 2bpp format.
	/// </summary>
	/// <param name="source">The collection of tiles to convert.</param>
	/// <returns>A new array of tiles.</returns>
	public static Nes2bppTile[] ConvertAllToNes2bpp(ReadOnlySpan<Snes2bppTile> source) {
		Nes2bppTile[] ret = GC.AllocateUninitializedArray<Nes2bppTile>(source.Length);
		ConvertAllToNes2bpp(source, ret);
		return ret;
	}

	/// <summary>
	/// Converts a collection of SNES 2bpp tiles to the NES 2bpp format.
	/// </summary>
	/// <param name="source">The collection of tiles to convert.</param>
	/// <param name="destination">The collection of tiles to write to.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> has fewer elements than <paramref name="source"/>.</exception>
	public static void ConvertAllToNes2bpp(ReadOnlySpan<Snes2bppTile> source, Span<Nes2bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Snes2bppTile tile in source) {
			tile.ConvertToNes2bpp(ref destination[i++]);
		}
	}
}
