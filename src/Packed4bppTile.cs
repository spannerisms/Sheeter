using System.Diagnostics.CodeAnalysis;

namespace Sheeter;

/// <summary>
/// Represents a block of data containing a tile encoded at 4-bits-per-pixel
/// with 2 pixels encoded in each byte.
/// </summary>
/// <inheritdoc cref="IndexedTile" path="//remarks"/>
[InlineArray(Length)]
public struct Packed4bppTile : IConvertibleTile<Packed4bppTile> {
	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 4;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<Packed4bppTile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	static int ITileData<Packed4bppTile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<Packed4bppTile>.Length => Length;
	static ConversionType IConvertibleTile<Packed4bppTile>.ConversionType => ConversionType.PackedLE;

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
	public readonly void CopyTo(scoped ref Packed4bppTile destination) {
		destination = this;
	}

	/// <inheritdoc cref="Packed2bppTile.PackUntiled(ReadOnlySpan{byte}, Span{byte})"/>
	public static void PackUntiled(scoped ReadOnlySpan<byte> source, scoped Span<byte> destination) {
		ConversionHelpers.PackUntiled<Packed4bppTile>(source, destination);
	}

	/// <inheritdoc cref="Packed2bppTile.PackUntiled(ReadOnlySpan{byte})"/>
	public static byte[] PackUntiled(scoped ReadOnlySpan<byte> source) {
		return ConversionHelpers.PackUntiled<Packed4bppTile>(source);
	}

	/// <inheritdoc cref="Packed2bppTile.SpreadUntiled(ReadOnlySpan{byte}, Span{byte})"/>
	public static void SpreadUntiled(scoped ReadOnlySpan<byte> source, scoped Span<byte> destination) {
		ConversionHelpers.SpreadUntiled<Packed2bppTile>(source, destination);
	}

	/// <inheritdoc cref="Packed2bppTile.SpreadUntiled(ReadOnlySpan{byte})"/>
	public static byte[] SpreadUntiled(scoped ReadOnlySpan<byte> source) {
		return ConversionHelpers.SpreadUntiled<Packed2bppTile>(source);
	}
}
