global using System.Diagnostics;

global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;

[assembly: CLSCompliant(true)]

namespace Sheeter;

/// <summary>
/// Contains generalized helper methods.
/// </summary>
internal static class ConversionHelpers {
	internal const ulong SplitUInt64Mask = 0x0101010101010101ul;
	internal const ulong PackMultiplier = 0x0102040810204080ul;

	internal const ulong SpreadMask2u16 = 0x0000FFFF_0000FFFF;
	internal const ulong SpreadMask2u8 = 0x000000FF_000000FF;

	internal const ulong SpreadMask4u8 = 0x00FF_00FF_00FF_00FF;
	internal const ulong SpreadMask4u4 = 0x000F_000F_000F_000F;

	internal const ulong SpreadMask8u4 = 0x0F0F0F0F0F0F0F0F;
	internal const ulong SpreadMask16u2 = 0x3333333333333333;

	/// <summary>
	/// Emulates PEXT with the same single bit selected in each byte.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static byte BitExtract(ulong row, int bit) {
		ulong mask = SplitUInt64Mask << bit;

		row &= mask;
		row *= PackMultiplier >> bit;
		return (byte) (row >> 56);
	}

	/// <summary>
	/// Emulates PDEP+BSWAP with the same single bit selected in each byte.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong PlaneDeposit(byte row, int bit) {
		return PlaneDepositTable[row] << bit;
	}

	//internal static byte ReverseBits(byte b) {
	//	if (ArmBase.Arm64.IsSupported) {
	//		return (byte) (ArmBase.Arm64.ReverseElementBits(b) >> 56);
	//
	//	} else {
	//		b = (byte) (((b >> 1) & 0x55) | ((b << 1) & 0xAA));
	//		b = (byte) (((b >> 2) & 0x33) | ((b << 2) & 0xCC));
	//		b = (byte) ((b >> 4) | (b << 4));
	//		return b;
	//	}
	//}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (int shift, ulong mask0, ulong mask1, ulong mask2) GetBmi2EmulationMasks(int bits) {
		if (bits is 2) {
			return (6, SplitUInt64Mask * 0x3, SpreadMask4u4, SpreadMask2u8);
		}

		if (bits is 4) {
			return (4, SplitUInt64Mask * 0xF, SpreadMask4u8, SpreadMask2u16);
		}

		return (0, 0, 0, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void PackBits(scoped ref readonly byte indxRef, scoped ref byte convRef, int bitsPerPixel) {
		ulong indexed = MemoryHelpers.ReadLE64(in indxRef);

		Debug.Assert(bitsPerPixel is 1 or 2 or 4);

		if (bitsPerPixel is 1) {
			// Using this accomplishes the same thing
			// but is much more efficient for 1bpp.
			convRef = BitExtract(indexed, 0);

		} else {
			var (shift, mask0, mask1, mask2) = GetBmi2EmulationMasks(bitsPerPixel);

			indexed &= mask0;
			indexed |= indexed >> shift;

			indexed &= mask1;
			indexed |= indexed >> (shift * 2);

			indexed &= mask2;
			indexed |= indexed >> (shift * 4);

			if (bitsPerPixel is 2) MemoryHelpers.WriteLE(ref convRef, (ushort) indexed);
			if (bitsPerPixel is 4) MemoryHelpers.WriteLE(ref convRef, (uint) indexed);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SpreadBits(scoped ref readonly byte convRef, scoped ref byte indxRef, int bitsPerPixel) {
		Debug.Assert(bitsPerPixel is 1 or 2 or 4);

		if (bitsPerPixel is 1) {
			// using this accomplishes the same thing
			// but is much more efficient for 1bpp.
			// PlaneDeposit also swaps the order of the data
			// so we need to reverse endianness before writing.
			// It's not worth having a separate table just for this tiny optimization
			// especially since this is a fairly niche format.
			MemoryHelpers.WriteBE(ref indxRef, PlaneDeposit(convRef, 0));

		} else {
			ulong indexed =
				(bitsPerPixel is 2) ? MemoryHelpers.ReadLE16(in convRef) :
				(bitsPerPixel is 4) ? MemoryHelpers.ReadLE32(in convRef) : 0;

			var (shift, mask0, mask1, mask2) = GetBmi2EmulationMasks(bitsPerPixel);

			indexed |= indexed << (shift * 4);
			indexed &= mask2;

			indexed |= indexed << (shift * 2);
			indexed &= mask1;

			indexed |= indexed << shift;
			indexed &= mask0;

			MemoryHelpers.WriteLE(ref indxRef, indexed);
		}
	}

	/// <summary>
	/// Helper for widening SNES tiles to a larger size.
	/// </summary>
	/// <remarks>
	/// <para><typeparamref name="TFrom"/> must be a multiple of 2 in length.</para>
	/// <para><typeparamref name="TTo"/> must be larger than <typeparamref name="TFrom"/>.</para>
	/// </remarks>
	internal static unsafe void WidenSnesHelper<TFrom, TTo>(scoped ref readonly TFrom source, scoped ref TTo destination)
			where TFrom : unmanaged, IConvertibleTile<TFrom>
			where TTo : unmanaged, IConvertibleTile<TTo> {
		Debug.Assert(sizeof(TTo) > sizeof(TFrom));
		Debug.Assert(sizeof(TFrom) % 2 == 0);

		// clear the bytes that are unused in the source format
		// The compiler doesn't know these are always InlineArrays,
		// so we have to use unsafe code.
		MemoryMarshal.CreateSpan(
			ref Unsafe.Add(ref destination.Reference, sizeof(TFrom)),
			sizeof(TTo) - sizeof(TFrom)
		).Clear();

		// ditto re:unsafe
		Unsafe.WriteUnaligned(ref destination.Reference, source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void PackedToIndexed(scoped ref readonly byte convRef, scoped ref byte indxRef, int indexedIncrement, int bitsPerPixel) {
		for (int i = 0; i < 8; i++) {
			SpreadBits(in convRef, ref indxRef, bitsPerPixel);

			indxRef = ref Unsafe.Add(ref indxRef, indexedIncrement);
			convRef = ref Unsafe.AddRO(in convRef, bitsPerPixel);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void IndexedToPacked(scoped ref readonly byte indxRef, scoped ref byte convRef, int indexedIncrement, int bitsPerPixel) {
		for (int i = 0; i < 8; i++) {
			PackBits(in indxRef, ref convRef, bitsPerPixel);

			indxRef = ref Unsafe.AddRO(in indxRef, indexedIncrement);
			convRef = ref Unsafe.Add(ref convRef, bitsPerPixel);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void PlanarToIndexed(scoped ref readonly byte convRef, scoped ref byte indxRef, int indexedIncrement, int bitsPerPixel, ConversionType mode) {
		int increment;
		int plane2Offset, plane3Offset, plane4Offset, plane5Offset, plane6Offset, plane7Offset, plane8Offset;

		if (mode is ConversionType.SnesInterleaved) {
			increment = 2;
			(plane2Offset, plane3Offset, plane4Offset, plane5Offset, plane6Offset, plane7Offset, plane8Offset) = GetSnesPlanarOffsets();

		} else if (mode is ConversionType.NesInterleaved) {
			increment = 1;
			(plane2Offset, plane3Offset, plane4Offset, plane5Offset, plane6Offset, plane7Offset, plane8Offset) = GetNesPlanarOffsets();

		} else if (mode is ConversionType.LinearPlanar) {
			increment = bitsPerPixel;
			(plane2Offset, plane3Offset, plane4Offset, plane5Offset, plane6Offset, plane7Offset, plane8Offset) = GetLinearPlanarOffsets();

		} else {
			return; // impossible branch
		}

		for (int row = 0; row < 8; row++) {
			ulong rowIndexed /*             */ = PlaneDeposit(convRef, 0);
			if (bitsPerPixel >= 2) rowIndexed |= PlaneDeposit(Unsafe.AddRO(in convRef, plane2Offset), 1);
			if (bitsPerPixel >= 3) rowIndexed |= PlaneDeposit(Unsafe.AddRO(in convRef, plane3Offset), 2);
			if (bitsPerPixel >= 4) rowIndexed |= PlaneDeposit(Unsafe.AddRO(in convRef, plane4Offset), 3);
			if (bitsPerPixel >= 5) rowIndexed |= PlaneDeposit(Unsafe.AddRO(in convRef, plane5Offset), 4);
			if (bitsPerPixel >= 6) rowIndexed |= PlaneDeposit(Unsafe.AddRO(in convRef, plane6Offset), 5);
			if (bitsPerPixel >= 7) rowIndexed |= PlaneDeposit(Unsafe.AddRO(in convRef, plane7Offset), 6);
			if (bitsPerPixel >= 8) rowIndexed |= PlaneDeposit(Unsafe.AddRO(in convRef, plane8Offset), 7);

			MemoryHelpers.WriteLE(ref indxRef, rowIndexed);

			indxRef = ref Unsafe.Add(ref indxRef, indexedIncrement);
			convRef = ref Unsafe.AddRO(in convRef, increment);
		}
	}

	internal static void PlanarToIndexed<T>(scoped ref readonly byte convRef, scoped ref byte indxRef)
			where T : unmanaged, IConvertibleTile<T> {
		PlanarToIndexed(in convRef, ref indxRef, IndexedTile.TileDimension, T.BitsPerPixel, T.ConversionType);
	}

	internal static void PlanarToIndexed<T>(scoped ref readonly byte convRef, scoped IndexedTileViewport indexedData)
			where T : unmanaged, IConvertibleTile<T> {
		PlanarToIndexed(in convRef, ref indexedData._reference, indexedData._bytesPerRow, T.BitsPerPixel, T.ConversionType);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void IndexedToPlanar(scoped ref readonly byte indxRef, scoped ref byte convRef, int indexedIncrement, int bitsPerPixel, ConversionType mode) {
		int increment;
		int plane2Offset, plane3Offset, plane4Offset, plane5Offset, plane6Offset, plane7Offset, plane8Offset;

		if (mode is ConversionType.SnesInterleaved) {
			increment = 2;
			(plane2Offset, plane3Offset, plane4Offset, plane5Offset, plane6Offset, plane7Offset, plane8Offset) = GetSnesPlanarOffsets();

		} else if (mode is ConversionType.NesInterleaved) {
			increment = 1;
			(plane2Offset, plane3Offset, plane4Offset, plane5Offset, plane6Offset, plane7Offset, plane8Offset) = GetNesPlanarOffsets();

		} else if (mode is ConversionType.LinearPlanar) {
			increment = bitsPerPixel;
			(plane2Offset, plane3Offset, plane4Offset, plane5Offset, plane6Offset, plane7Offset, plane8Offset) = GetLinearPlanarOffsets();

		} else {
			return; // impossible branch
		}

		for (int row = 0; row < 8; row++) {
			// big endian because rows and bitplanes are reverse of each other
			ulong indexedBytes = MemoryHelpers.ReadBE64(in indxRef);

			if (bitsPerPixel >= 1) /*          */ convRef /*          */ = BitExtract(indexedBytes, 0);
			if (bitsPerPixel >= 2) Unsafe.Add(ref convRef, plane2Offset) = BitExtract(indexedBytes, 1);
			if (bitsPerPixel >= 3) Unsafe.Add(ref convRef, plane3Offset) = BitExtract(indexedBytes, 2);
			if (bitsPerPixel >= 4) Unsafe.Add(ref convRef, plane4Offset) = BitExtract(indexedBytes, 3);
			if (bitsPerPixel >= 5) Unsafe.Add(ref convRef, plane5Offset) = BitExtract(indexedBytes, 4);
			if (bitsPerPixel >= 6) Unsafe.Add(ref convRef, plane6Offset) = BitExtract(indexedBytes, 5);
			if (bitsPerPixel >= 7) Unsafe.Add(ref convRef, plane7Offset) = BitExtract(indexedBytes, 6);
			if (bitsPerPixel >= 8) Unsafe.Add(ref convRef, plane8Offset) = BitExtract(indexedBytes, 7);

			indxRef = ref Unsafe.AddRO(in indxRef, indexedIncrement);
			convRef = ref Unsafe.Add(ref convRef, increment);
		}
	}

	/// <summary>
	/// Calculates the number of bytes used when packing, including padding.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int PackedSize(int length, int bitsPerPixel) {
		return ((length * bitsPerPixel) + 7) / 8;
	}

	/// <summary>
	/// Converts arbitrary data without tiling it.
	/// </summary>
	internal static byte[] PackUntiled<T>(scoped ReadOnlySpan<byte> indexedData)
			where T : unmanaged, IConvertibleTile<T> {
		Asserts.IsPackedFormat(typeof(T));

		byte[] ret = GC.AllocateUninitializedArray<byte>(PackedSize(indexedData.Length, T.BitsPerPixel));
		PackUntiled<T>(indexedData, ret);
		return ret;
	}

	internal static void PackUntiled<T>(scoped ReadOnlySpan<byte> indexedData, scoped Span<byte> crunchedData)
			where T : unmanaged, IConvertibleTile<T> {
		Asserts.IsPackedFormat(typeof(T));

		const int ChunkSize = sizeof(ulong);
		int bitsPerPixel = T.BitsPerPixel;
		int remaining = indexedData.Length;

		ErrorHelpers.ThrowIfDestinationTooShort(PackedSize(indexedData.Length, bitsPerPixel), crunchedData.Length);

		ref readonly byte indxRef = ref MemoryMarshal.GetReference(indexedData);
		ref byte convRef = ref MemoryMarshal.GetReference(crunchedData);

		while (remaining >= ChunkSize) {
			PackBits(in indxRef, ref convRef, bitsPerPixel);

			indxRef = ref Unsafe.AddRO(in indxRef, ChunkSize);
			convRef = ref Unsafe.Add(ref convRef, bitsPerPixel);

			remaining -= ChunkSize;
		}

		// clean up remainder
		if (remaining > 0) {
			Span<byte> tmpIndx = stackalloc byte[ChunkSize];
			Span<byte> tmpConv = stackalloc byte[bitsPerPixel];

			ref byte tmpConvRef = ref MemoryMarshal.GetReference(tmpConv);
			ref byte tmpIndxRef = ref MemoryMarshal.GetReference(tmpIndx);

			tmpIndx.Clear();

			Unsafe.CopyBlockUnaligned(ref tmpIndxRef, in indxRef, (uint) remaining);

			PackBits(in tmpIndxRef, ref tmpConvRef, bitsPerPixel);

			Unsafe.CopyBlockUnaligned(ref convRef, in tmpConvRef, (uint) PackedSize(remaining, bitsPerPixel));
		}
	}

	/// <summary>
	/// Converts arbitrary data without tiling it.
	/// </summary>
	internal static byte[] SpreadUntiled<T>(scoped ReadOnlySpan<byte> crunchedData)
			where T : unmanaged, IConvertibleTile<T> {
		Asserts.IsPackedFormat(typeof(T));

		byte[] ret = GC.AllocateUninitializedArray<byte>(crunchedData.Length * 8 / T.BitsPerPixel);
		SpreadUntiled<T>(crunchedData, ret);
		return ret;
	}

	internal static void SpreadUntiled<T>(scoped ReadOnlySpan<byte> crunchedData, scoped Span<byte> indexedData)
			where T : unmanaged, IConvertibleTile<T> {
		Asserts.IsPackedFormat(typeof(T));

		const int ChunkSize = sizeof(ulong);
		int bitsPerPixel = T.BitsPerPixel;
		int remaining = crunchedData.Length;

		ErrorHelpers.ThrowIfDestinationTooShort(remaining * 8 / bitsPerPixel, indexedData.Length);

		ref byte indxRef = ref MemoryMarshal.GetReference(indexedData);
		ref readonly byte convRef = ref MemoryMarshal.GetReference(crunchedData);

		// TODO can the JIT tell that both of these are always in bounds?
		while (remaining >= bitsPerPixel) {
			SpreadBits(in convRef, ref indxRef, bitsPerPixel);

			indxRef = ref Unsafe.Add(ref indxRef, ChunkSize);
			convRef = ref Unsafe.AddRO(in convRef, bitsPerPixel);

			remaining -= bitsPerPixel;
		}

		// clean up remainder
		if (remaining > 0) {
			Span<byte> tmpIndx = stackalloc byte[ChunkSize];
			Span<byte> tmpConv = stackalloc byte[bitsPerPixel];

			ref byte tmpConvRef = ref MemoryMarshal.GetReference(tmpConv);
			ref byte tmpIndxRef = ref MemoryMarshal.GetReference(tmpIndx);

			tmpConv.Clear();

			Unsafe.CopyBlockUnaligned(ref tmpConvRef, in convRef, (uint) remaining);

			SpreadBits(in tmpConvRef, ref tmpIndxRef, bitsPerPixel);

			Unsafe.CopyBlockUnaligned(ref indxRef, in tmpIndxRef, (uint) (remaining * 8 / bitsPerPixel));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (int p2, int p3, int p4, int p5, int p6, int p7, int p8) GetSnesPlanarOffsets() {
		return (1, 16, 17, 32, 33, 48, 49);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (int p2, int p3, int p4, int p5, int p6, int p7, int p8) GetNesPlanarOffsets() {
		return (8, 16, 24, 32, 40, 48, 56);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (int p2, int p3, int p4, int p5, int p6, int p7, int p8) GetLinearPlanarOffsets() {
		return (1, 2, 3, 4, 5, 6, 7);
	}

	// left here because I don't feel like regenerating it if I need it.
	//internal static ReadOnlySpan<ulong> BitDepositTable => [
	//	0x0000000000000000, 0x0000000000000001, 0x0000000000000100, 0x0000000000000101, 0x0000000000010000, 0x0000000000010001, 0x0000000000010100, 0x0000000000010101,
	//	0x0000000001000000, 0x0000000001000001, 0x0000000001000100, 0x0000000001000101, 0x0000000001010000, 0x0000000001010001, 0x0000000001010100, 0x0000000001010101,
	//	0x0000000100000000, 0x0000000100000001, 0x0000000100000100, 0x0000000100000101, 0x0000000100010000, 0x0000000100010001, 0x0000000100010100, 0x0000000100010101,
	//	0x0000000101000000, 0x0000000101000001, 0x0000000101000100, 0x0000000101000101, 0x0000000101010000, 0x0000000101010001, 0x0000000101010100, 0x0000000101010101,
	//	0x0000010000000000, 0x0000010000000001, 0x0000010000000100, 0x0000010000000101, 0x0000010000010000, 0x0000010000010001, 0x0000010000010100, 0x0000010000010101,
	//	0x0000010001000000, 0x0000010001000001, 0x0000010001000100, 0x0000010001000101, 0x0000010001010000, 0x0000010001010001, 0x0000010001010100, 0x0000010001010101,
	//	0x0000010100000000, 0x0000010100000001, 0x0000010100000100, 0x0000010100000101, 0x0000010100010000, 0x0000010100010001, 0x0000010100010100, 0x0000010100010101,
	//	0x0000010101000000, 0x0000010101000001, 0x0000010101000100, 0x0000010101000101, 0x0000010101010000, 0x0000010101010001, 0x0000010101010100, 0x0000010101010101,
	//	0x0001000000000000, 0x0001000000000001, 0x0001000000000100, 0x0001000000000101, 0x0001000000010000, 0x0001000000010001, 0x0001000000010100, 0x0001000000010101,
	//	0x0001000001000000, 0x0001000001000001, 0x0001000001000100, 0x0001000001000101, 0x0001000001010000, 0x0001000001010001, 0x0001000001010100, 0x0001000001010101,
	//	0x0001000100000000, 0x0001000100000001, 0x0001000100000100, 0x0001000100000101, 0x0001000100010000, 0x0001000100010001, 0x0001000100010100, 0x0001000100010101,
	//	0x0001000101000000, 0x0001000101000001, 0x0001000101000100, 0x0001000101000101, 0x0001000101010000, 0x0001000101010001, 0x0001000101010100, 0x0001000101010101,
	//	0x0001010000000000, 0x0001010000000001, 0x0001010000000100, 0x0001010000000101, 0x0001010000010000, 0x0001010000010001, 0x0001010000010100, 0x0001010000010101,
	//	0x0001010001000000, 0x0001010001000001, 0x0001010001000100, 0x0001010001000101, 0x0001010001010000, 0x0001010001010001, 0x0001010001010100, 0x0001010001010101,
	//	0x0001010100000000, 0x0001010100000001, 0x0001010100000100, 0x0001010100000101, 0x0001010100010000, 0x0001010100010001, 0x0001010100010100, 0x0001010100010101,
	//	0x0001010101000000, 0x0001010101000001, 0x0001010101000100, 0x0001010101000101, 0x0001010101010000, 0x0001010101010001, 0x0001010101010100, 0x0001010101010101,
	//	0x0100000000000000, 0x0100000000000001, 0x0100000000000100, 0x0100000000000101, 0x0100000000010000, 0x0100000000010001, 0x0100000000010100, 0x0100000000010101,
	//	0x0100000001000000, 0x0100000001000001, 0x0100000001000100, 0x0100000001000101, 0x0100000001010000, 0x0100000001010001, 0x0100000001010100, 0x0100000001010101,
	//	0x0100000100000000, 0x0100000100000001, 0x0100000100000100, 0x0100000100000101, 0x0100000100010000, 0x0100000100010001, 0x0100000100010100, 0x0100000100010101,
	//	0x0100000101000000, 0x0100000101000001, 0x0100000101000100, 0x0100000101000101, 0x0100000101010000, 0x0100000101010001, 0x0100000101010100, 0x0100000101010101,
	//	0x0100010000000000, 0x0100010000000001, 0x0100010000000100, 0x0100010000000101, 0x0100010000010000, 0x0100010000010001, 0x0100010000010100, 0x0100010000010101,
	//	0x0100010001000000, 0x0100010001000001, 0x0100010001000100, 0x0100010001000101, 0x0100010001010000, 0x0100010001010001, 0x0100010001010100, 0x0100010001010101,
	//	0x0100010100000000, 0x0100010100000001, 0x0100010100000100, 0x0100010100000101, 0x0100010100010000, 0x0100010100010001, 0x0100010100010100, 0x0100010100010101,
	//	0x0100010101000000, 0x0100010101000001, 0x0100010101000100, 0x0100010101000101, 0x0100010101010000, 0x0100010101010001, 0x0100010101010100, 0x0100010101010101,
	//	0x0101000000000000, 0x0101000000000001, 0x0101000000000100, 0x0101000000000101, 0x0101000000010000, 0x0101000000010001, 0x0101000000010100, 0x0101000000010101,
	//	0x0101000001000000, 0x0101000001000001, 0x0101000001000100, 0x0101000001000101, 0x0101000001010000, 0x0101000001010001, 0x0101000001010100, 0x0101000001010101,
	//	0x0101000100000000, 0x0101000100000001, 0x0101000100000100, 0x0101000100000101, 0x0101000100010000, 0x0101000100010001, 0x0101000100010100, 0x0101000100010101,
	//	0x0101000101000000, 0x0101000101000001, 0x0101000101000100, 0x0101000101000101, 0x0101000101010000, 0x0101000101010001, 0x0101000101010100, 0x0101000101010101,
	//	0x0101010000000000, 0x0101010000000001, 0x0101010000000100, 0x0101010000000101, 0x0101010000010000, 0x0101010000010001, 0x0101010000010100, 0x0101010000010101,
	//	0x0101010001000000, 0x0101010001000001, 0x0101010001000100, 0x0101010001000101, 0x0101010001010000, 0x0101010001010001, 0x0101010001010100, 0x0101010001010101,
	//	0x0101010100000000, 0x0101010100000001, 0x0101010100000100, 0x0101010100000101, 0x0101010100010000, 0x0101010100010001, 0x0101010100010100, 0x0101010100010101,
	//	0x0101010101000000, 0x0101010101000001, 0x0101010101000100, 0x0101010101000101, 0x0101010101010000, 0x0101010101010001, 0x0101010101010100, 0x0101010101010101,
	//];
	internal static ReadOnlySpan<ulong> PlaneDepositTable => [
		0x0000000000000000, 0x0100000000000000, 0x0001000000000000, 0x0101000000000000, 0x0000010000000000, 0x0100010000000000, 0x0001010000000000, 0x0101010000000000,
		0x0000000100000000, 0x0100000100000000, 0x0001000100000000, 0x0101000100000000, 0x0000010100000000, 0x0100010100000000, 0x0001010100000000, 0x0101010100000000,
		0x0000000001000000, 0x0100000001000000, 0x0001000001000000, 0x0101000001000000, 0x0000010001000000, 0x0100010001000000, 0x0001010001000000, 0x0101010001000000,
		0x0000000101000000, 0x0100000101000000, 0x0001000101000000, 0x0101000101000000, 0x0000010101000000, 0x0100010101000000, 0x0001010101000000, 0x0101010101000000,
		0x0000000000010000, 0x0100000000010000, 0x0001000000010000, 0x0101000000010000, 0x0000010000010000, 0x0100010000010000, 0x0001010000010000, 0x0101010000010000,
		0x0000000100010000, 0x0100000100010000, 0x0001000100010000, 0x0101000100010000, 0x0000010100010000, 0x0100010100010000, 0x0001010100010000, 0x0101010100010000,
		0x0000000001010000, 0x0100000001010000, 0x0001000001010000, 0x0101000001010000, 0x0000010001010000, 0x0100010001010000, 0x0001010001010000, 0x0101010001010000,
		0x0000000101010000, 0x0100000101010000, 0x0001000101010000, 0x0101000101010000, 0x0000010101010000, 0x0100010101010000, 0x0001010101010000, 0x0101010101010000,
		0x0000000000000100, 0x0100000000000100, 0x0001000000000100, 0x0101000000000100, 0x0000010000000100, 0x0100010000000100, 0x0001010000000100, 0x0101010000000100,
		0x0000000100000100, 0x0100000100000100, 0x0001000100000100, 0x0101000100000100, 0x0000010100000100, 0x0100010100000100, 0x0001010100000100, 0x0101010100000100,
		0x0000000001000100, 0x0100000001000100, 0x0001000001000100, 0x0101000001000100, 0x0000010001000100, 0x0100010001000100, 0x0001010001000100, 0x0101010001000100,
		0x0000000101000100, 0x0100000101000100, 0x0001000101000100, 0x0101000101000100, 0x0000010101000100, 0x0100010101000100, 0x0001010101000100, 0x0101010101000100,
		0x0000000000010100, 0x0100000000010100, 0x0001000000010100, 0x0101000000010100, 0x0000010000010100, 0x0100010000010100, 0x0001010000010100, 0x0101010000010100,
		0x0000000100010100, 0x0100000100010100, 0x0001000100010100, 0x0101000100010100, 0x0000010100010100, 0x0100010100010100, 0x0001010100010100, 0x0101010100010100,
		0x0000000001010100, 0x0100000001010100, 0x0001000001010100, 0x0101000001010100, 0x0000010001010100, 0x0100010001010100, 0x0001010001010100, 0x0101010001010100,
		0x0000000101010100, 0x0100000101010100, 0x0001000101010100, 0x0101000101010100, 0x0000010101010100, 0x0100010101010100, 0x0001010101010100, 0x0101010101010100,
		0x0000000000000001, 0x0100000000000001, 0x0001000000000001, 0x0101000000000001, 0x0000010000000001, 0x0100010000000001, 0x0001010000000001, 0x0101010000000001,
		0x0000000100000001, 0x0100000100000001, 0x0001000100000001, 0x0101000100000001, 0x0000010100000001, 0x0100010100000001, 0x0001010100000001, 0x0101010100000001,
		0x0000000001000001, 0x0100000001000001, 0x0001000001000001, 0x0101000001000001, 0x0000010001000001, 0x0100010001000001, 0x0001010001000001, 0x0101010001000001,
		0x0000000101000001, 0x0100000101000001, 0x0001000101000001, 0x0101000101000001, 0x0000010101000001, 0x0100010101000001, 0x0001010101000001, 0x0101010101000001,
		0x0000000000010001, 0x0100000000010001, 0x0001000000010001, 0x0101000000010001, 0x0000010000010001, 0x0100010000010001, 0x0001010000010001, 0x0101010000010001,
		0x0000000100010001, 0x0100000100010001, 0x0001000100010001, 0x0101000100010001, 0x0000010100010001, 0x0100010100010001, 0x0001010100010001, 0x0101010100010001,
		0x0000000001010001, 0x0100000001010001, 0x0001000001010001, 0x0101000001010001, 0x0000010001010001, 0x0100010001010001, 0x0001010001010001, 0x0101010001010001,
		0x0000000101010001, 0x0100000101010001, 0x0001000101010001, 0x0101000101010001, 0x0000010101010001, 0x0100010101010001, 0x0001010101010001, 0x0101010101010001,
		0x0000000000000101, 0x0100000000000101, 0x0001000000000101, 0x0101000000000101, 0x0000010000000101, 0x0100010000000101, 0x0001010000000101, 0x0101010000000101,
		0x0000000100000101, 0x0100000100000101, 0x0001000100000101, 0x0101000100000101, 0x0000010100000101, 0x0100010100000101, 0x0001010100000101, 0x0101010100000101,
		0x0000000001000101, 0x0100000001000101, 0x0001000001000101, 0x0101000001000101, 0x0000010001000101, 0x0100010001000101, 0x0001010001000101, 0x0101010001000101,
		0x0000000101000101, 0x0100000101000101, 0x0001000101000101, 0x0101000101000101, 0x0000010101000101, 0x0100010101000101, 0x0001010101000101, 0x0101010101000101,
		0x0000000000010101, 0x0100000000010101, 0x0001000000010101, 0x0101000000010101, 0x0000010000010101, 0x0100010000010101, 0x0001010000010101, 0x0101010000010101,
		0x0000000100010101, 0x0100000100010101, 0x0001000100010101, 0x0101000100010101, 0x0000010100010101, 0x0100010100010101, 0x0001010100010101, 0x0101010100010101,
		0x0000000001010101, 0x0100000001010101, 0x0001000001010101, 0x0101000001010101, 0x0000010001010101, 0x0100010001010101, 0x0001010001010101, 0x0101010001010101,
		0x0000000101010101, 0x0100000101010101, 0x0001000101010101, 0x0101000101010101, 0x0000010101010101, 0x0100010101010101, 0x0001010101010101, 0x0101010101010101,
	];
}
