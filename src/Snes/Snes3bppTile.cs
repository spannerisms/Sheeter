using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;

namespace Sheeter.Snes;

/// <summary>
/// Represents a block of data containing a tile encoded at 3-bits-per-pixel
/// and interleaved by row per bit plane.
/// </summary>
/// <inheritdoc cref="IndexedTile" path="//remarks"/>
[InlineArray(Length)]
public struct Snes3bppTile : IConvertibleTile<Snes3bppTile> {
	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 3;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<Snes3bppTile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	static int ITileData<Snes3bppTile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<Snes3bppTile>.Length => Length;
	static ConversionType IConvertibleTile<Snes3bppTile>.ConversionType => ConversionType.Snes3bpp;

	/// <inheritdoc/>
	public readonly void CopyTo(scoped ref Snes3bppTile destination) {
		destination = this;
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped ref IndexedTile destination) {
		ToIndexedTile(in _tileData, ref destination._tileData, IndexedTile.TileDimension);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		ToIndexedTile(ref destination._indexedTile);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped IndexedTileViewport destination) {
		ToIndexedTile(in _tileData, ref destination._reference, destination._bytesPerRow);
	}

	private static void ToIndexedTile(scoped ref readonly byte convRef, scoped ref byte indxRef, int indexedIncrement) {
		ref readonly byte convRef2 = ref Unsafe.AddRO(in convRef, 16);

		for (int row = 0; row < 8; row++) {
			ulong rowIndexed = ConversionHelpers.PlaneDeposit(convRef, 0);
			rowIndexed |= ConversionHelpers.PlaneDeposit(Unsafe.AddRO(in convRef, 1), 1);
			rowIndexed |= ConversionHelpers.PlaneDeposit(convRef2, 2);

			MemoryHelpers.WriteLE(ref indxRef, rowIndexed);

			indxRef = ref Unsafe.Add(ref indxRef, indexedIncrement);
			convRef = ref Unsafe.AddRO(in convRef, 2);
			convRef2 = ref Unsafe.AddRO(in convRef2, 1);
		}
	}

	/// <inheritdoc cref="Packed1bppTile.Widen(ref Packed2bppTile)"/>
	public readonly void Widen(scoped ref Snes4bppTile destination) {
		if (Vector128.IsHardwareAccelerated) { // Vector128 codegen seems better, so it should be preferred
			WidenVector128(ref destination);

		} else if (Vector256.IsHardwareAccelerated) {
			WidenVector256(ref destination);

		} else {
			WidenFallback(ref destination);
		}
	}

	/// <inheritdoc cref="Packed1bppTile.Widen(ref Packed2bppTile)"/>
	public readonly void Widen(scoped ref Snes8bppTile destination) {
		ref Snes4bppTile dest4 = ref Unsafe.As<Snes8bppTile, Snes4bppTile>(ref destination);
		Widen(ref dest4); // hoist the 4bpp code for lower half
		destination[Snes4bppTile.Length..].Clear(); // clear upper half
	}

	/// <summary>
	/// Remapping vector.
	/// </summary>
	private static readonly Vector256<byte> To4bppShuffler =
		Vector256.Create((byte)
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
			0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,

			0x10, 0x1F, 0x11, 0x1F, 0x12, 0x1F, 0x13, 0x1F,
			0x14, 0x1F, 0x15, 0x1F, 0x16, 0x1F, 0x17, 0x1F
		);

	/// <summary>
	/// Remapping vector.
	/// </summary>
	private static readonly Vector128<byte> To4bppShufflerTop =
		Vector128.Create((byte)
			0x0, 0xF, 0x1, 0xF, 0x2, 0xF, 0x3, 0xF,
			0x4, 0xF, 0x5, 0xF, 0x6, 0xF, 0x7, 0xF
		);

	internal readonly void WidenVector256(scoped ref Snes4bppTile destination) {
		Vector256<byte> v2 = Vector256.Create(
			BitConverter.ToUInt64(this),
			BitConverter.ToUInt64(this[8..]),
			BitConverter.ToUInt64(this[16..]),
			0L
		).AsByte();

		Vector256.Shuffle(v2, To4bppShuffler).CopyTo(destination);
	}

	internal readonly void WidenVector128(scoped ref Snes4bppTile destination) {
		// first half is identical to the target format
		this[..Snes2bppTile.Length].CopyTo(destination);

		// only the upper half needs shuffling
		Vector128<byte> v2 = Vector128.CreateScalar(BitConverter.ToUInt64(this[Snes2bppTile.Length..])).AsByte();

		Vector128.Shuffle(v2, To4bppShufflerTop).CopyTo(destination[Snes2bppTile.Length..]);
	}

	internal readonly void WidenFallback(scoped ref Snes4bppTile destination) {
		this[..Snes2bppTile.Length].CopyTo(destination);

		Span<ushort> destU16 = MemoryMarshal.Cast<byte, ushort>(destination[Snes2bppTile.Length..]);

		// casting from byte to ushort to clear upper byte
		destU16[0] = this[Snes2bppTile.Length + 0];
		destU16[1] = this[Snes2bppTile.Length + 1];
		destU16[2] = this[Snes2bppTile.Length + 2];
		destU16[3] = this[Snes2bppTile.Length + 3];
		destU16[4] = this[Snes2bppTile.Length + 4];
		destU16[5] = this[Snes2bppTile.Length + 5];
		destU16[6] = this[Snes2bppTile.Length + 6];
		destU16[7] = this[Snes2bppTile.Length + 7];
	}

	/// <summary>
	/// Widens this tile to a 4BPP tile while also setting plane 4 of
	/// every non-transparent pixel.
	/// </summary>
	/// <inheritdoc cref="Widen(ref Snes4bppTile)" path="//param"/>
	public readonly void WidenHigh(scoped ref Snes4bppTile destination) {
		const ulong EveryOtherRow = 0xFF00_FF00_FF00_FF00;
		ulong lowPlanes, highPlanes;

		// TODO can this be made SIMD?

		//
		// rows 0, 1, 2, 3
		//

		// we'll read and write the first 8 bytes ourselves
		// because we care about actual the data as well
		lowPlanes = BinaryPrimitives.ReadUInt64LittleEndian(this);
		BinaryPrimitives.WriteUInt64LittleEndian(destination, lowPlanes);

		lowPlanes |= lowPlanes << 8; // merge bit planes 0 and 1
		lowPlanes &= EveryOtherRow; // mask to every other byte where bit plane 4 lies

		highPlanes = this[Snes2bppTile.Length + 0] |
			((ulong) this[Snes2bppTile.Length + 1] << 16) |
			((ulong) this[Snes2bppTile.Length + 2] << 32) |
			((ulong) this[Snes2bppTile.Length + 3] << 48);

		// add in the mask for bitplane 4 along with the actual data for bitplane 3
		highPlanes |= lowPlanes | (highPlanes << 8);
		BinaryPrimitives.WriteUInt64LittleEndian(destination[16..], highPlanes);

		//
		// rows 4, 5, 6, 7
		//

		lowPlanes = BinaryPrimitives.ReadUInt64LittleEndian(this[8..]);
		BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], lowPlanes);
		lowPlanes |= lowPlanes << 8;
		lowPlanes &= EveryOtherRow;
		highPlanes = this[Snes2bppTile.Length + 4] |
			((ulong) this[Snes2bppTile.Length + 5] << 16) |
			((ulong) this[Snes2bppTile.Length + 6] << 32) |
			((ulong) this[Snes2bppTile.Length + 7] << 48);
		highPlanes |= lowPlanes | (highPlanes << 8);
		BinaryPrimitives.WriteUInt64LittleEndian(destination[24..], highPlanes);
	}

	/// <summary>
	/// Widens a collection of tiles to 4 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Snes4bppTile[] WidenAllTo4bpp(ReadOnlySpan<Snes3bppTile> source) {
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
	public static void WidenAllTo4bpp(ReadOnlySpan<Snes3bppTile> source, Span<Snes4bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Snes3bppTile tile in source) {
			tile.Widen(ref destination[i++]);
		}
	}

	/// <summary>
	/// Widens a collection of tiles to 4 bits-per-pixel
	/// such that non-transparent indices have plane 4 set.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Snes4bppTile[] WidenAllTo4bppHigh(ReadOnlySpan<Snes3bppTile> source) {
		Snes4bppTile[] ret = GC.AllocateUninitializedArray<Snes4bppTile>(source.Length);
		WidenAllTo4bppHigh(source, ret);
		return ret;
	}

	/// <summary>
	/// Widens a collection of tiles to 4 bits-per-pixel.
	/// such that non-transparent indices have plane 4 set.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <param name="destination">The collection of tiles to write to.</param>
	/// <exception cref="ArgumentException">If <paramref name="destination"/> has fewer elements than <paramref name="source"/>.</exception>
	public static void WidenAllTo4bppHigh(ReadOnlySpan<Snes3bppTile> source, Span<Snes4bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Snes3bppTile tile in source) {
			tile.WidenHigh(ref destination[i++]);
		}
	}

	/// <summary>
	/// Widens a collection of tiles to 8 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Snes8bppTile[] WidenAllTo8bpp(ReadOnlySpan<Snes3bppTile> source) {
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
	public static void WidenAllTo8bpp(ReadOnlySpan<Snes3bppTile> source, Span<Snes8bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Snes3bppTile tile in source) {
			tile.Widen(ref destination[i++]);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void IndexedToSnes3bpp(scoped ref readonly byte indxRef, scoped ref byte convRef, int indexedIncrement) {
		ref byte convRef2 = ref Unsafe.Add(ref convRef, 16);

		for (int row = 0; row < 8; row++) {
			// big endian because rows and bitplanes are reverse of each other
			ulong indexedBytes = MemoryHelpers.ReadBE64(in indxRef);

			convRef = ConversionHelpers.BitExtract(indexedBytes, 0);
			convRef2 = ConversionHelpers.BitExtract(indexedBytes, 2);
			Unsafe.Add(ref convRef, 1) = ConversionHelpers.BitExtract(indexedBytes, 1);

			indxRef = ref Unsafe.AddRO(in indxRef, indexedIncrement);
			convRef = ref Unsafe.Add(ref convRef, 2);
			convRef2 = ref Unsafe.Add(ref convRef2, 1);
		}
	}
}
