using System.Buffers.Binary;

namespace Sheeter;

[DebuggerStepThrough]
internal static class MemoryHelpers {
	[StackTraceHidden]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Span<T> UnsafeCreateSpanFromBytes<T>(ReadOnlySpan<byte> source, int offset, int count) where T : unmanaged {
		return MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref Unsafe.Add(ref MemoryMarshal.GetReference(source), offset)), count);
	}

	[StackTraceHidden]
	internal static Span<T> CreateSpanFromBytes<T>(ReadOnlySpan<byte> source, int offset, int count) where T : unmanaged {
		if (source.Length < (Unsafe.SizeOf<T>() * count) + offset) {
			ErrorHelpers.ThrowBadArgument("Source is too short.");
		}

		return UnsafeCreateSpanFromBytes<T>(source, offset, count);
	}

	[StackTraceHidden]
	internal static Span<T> CreateSpanFromBytes<T>(ReadOnlySpan<byte> source) where T : unmanaged {
		int size = Unsafe.SizeOf<T>();
		var (count, rem) = Math.DivRem(source.Length, size);

		if (rem is not 0) {
			ErrorHelpers.ThrowBadArgument($"Source must be a length that is a multiple of the target size ({size}).");
		}

		return UnsafeCreateSpanFromBytes<T>(source, 0, count);
	}

	[StackTraceHidden]
	internal static Span<T> TryCreateSpanFromBytes<T>(ReadOnlySpan<byte> source, int offset, int count, out bool success) where T : unmanaged {
		success = source.Length >= (Unsafe.SizeOf<T>() * count) + offset;
		return success ? UnsafeCreateSpanFromBytes<T>(source, 0, count) : [];
	}

	[StackTraceHidden]
	internal static Span<T> TryCreateSpanFromBytes<T>(ReadOnlySpan<byte> source, out bool success) where T : unmanaged {
		var (count, rem) = Math.DivRem(source.Length, Unsafe.SizeOf<T>());
		success = rem == 0;
		return success ? UnsafeCreateSpanFromBytes<T>(source, 0, count) : [];
	}

	extension(NativeMemory) {
		internal unsafe static T* AllocNice<T>(int ct) where T : unmanaged {
			return (T*) NativeMemory.AlignedAlloc((nuint) (sizeof(T) * ct), 64);
		}
	}

	extension(Unsafe) {
		/// <summary>
		/// <see cref="Unsafe.Add{T}(ref T, int)"/>, but readonly.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static ref readonly byte AddRO(ref readonly byte source, int elementOffset) {
			return ref Unsafe.Add(ref Unsafe.AsRef(in source), elementOffset);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ref T GetPixelRef<T>(ref T origin, int x, int y, int stride) {
		return ref Unsafe.Add(ref origin, x + (y * stride));
	}

	/// <summary>
	/// Unaligned, little-endian read.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ushort ReadLE16(scoped ref readonly byte reference) {
		return BitConverter.IsLittleEndian
			? Unsafe.ReadUnaligned<ushort>(in reference)
			: BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(in reference));
	}

	/// <inheritdoc cref="ReadLE16(ref readonly byte)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint ReadLE32(scoped ref readonly byte reference) {
		return BitConverter.IsLittleEndian
			? Unsafe.ReadUnaligned<uint>(in reference)
			: BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(in reference));
	}

	/// <inheritdoc cref="ReadLE16(ref readonly byte)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong ReadLE64(scoped ref readonly byte reference) {
		return BitConverter.IsLittleEndian
			? Unsafe.ReadUnaligned<ulong>(in reference)
			: BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(in reference));
	}

	/// <summary>
	/// Unaligned, little-endian write.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void WriteLE(scoped ref byte reference, ushort value) {
		Unsafe.WriteUnaligned(ref reference,
			BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value)
		);
	}

	/// <inheritdoc cref="WriteLE(ref byte, ushort)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void WriteLE(scoped ref byte reference, uint value) {
		Unsafe.WriteUnaligned(ref reference,
			BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value)
		);
	}

	/// <inheritdoc cref="WriteLE(ref byte, ushort)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void WriteLE(scoped ref byte reference, ulong value) {
		Unsafe.WriteUnaligned(ref reference,
			BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value)
		);
	}

	/// <summary>
	/// Unaligned, big-endian write.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void WriteBE(scoped ref byte reference, ulong value) {
		Unsafe.WriteUnaligned(ref reference,
			BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value
		);
	}

	/// <summary>
	/// Unaligned, big-endian read.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong ReadBE64(scoped ref readonly byte reference) {
		return BitConverter.IsLittleEndian
			? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(in reference))
			: Unsafe.ReadUnaligned<ulong>(in reference);
	}
}
