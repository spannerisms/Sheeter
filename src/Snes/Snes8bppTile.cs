using System.Diagnostics.CodeAnalysis;

namespace Sheeter.Snes;

/// <summary>
/// Represents a block of data containing a tile encoded at 8-bits-per-pixel
/// and interleaved by row per bit plane.
/// </summary>
/// <inheritdoc cref="IndexedTile" path="//remarks"/>
[InlineArray(Length)]
public struct Snes8bppTile : IConvertibleTile<Snes8bppTile> {
	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 8;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<Snes8bppTile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	static int ITileData<Snes8bppTile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<Snes8bppTile>.Length => Length;
	static ConversionType IConvertibleTile<Snes8bppTile>.ConversionType => ConversionType.SnesInterleaved;

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped ref IndexedTile destination) {
		ConversionHelpers.PlanarToIndexed<Snes8bppTile>(in _tileData, ref destination._tileData);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		ToIndexedTile(ref destination._indexedTile);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped IndexedTileViewport destination) {
		ConversionHelpers.PlanarToIndexed<Snes8bppTile>(in _tileData, destination);
	}

	/// <inheritdoc/>
	public readonly void CopyTo(scoped ref Snes8bppTile destination) {
		destination = this;
	}
}
