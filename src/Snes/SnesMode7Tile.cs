using System.Diagnostics.CodeAnalysis;

namespace Sheeter.Snes;

/// <summary>
/// Represents a block of data containing a tile encoded at 8-bits-per-pixel.
/// </summary>
/// <inheritdoc cref="IndexedTile" path="//remarks"/>
[InlineArray(Length)]
public struct SnesMode7Tile : IConvertibleTile<SnesMode7Tile> {
	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 8;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<SnesMode7Tile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	static int ITileData<SnesMode7Tile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<SnesMode7Tile>.Length => Length;
	static ConversionType IConvertibleTile<SnesMode7Tile>.ConversionType => ConversionType.RawCopy;

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped ref IndexedTile destination) {
		AsSpan().CopyTo(destination);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		ToIndexedTile(ref destination._indexedTile);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped IndexedTileViewport destination) {
		IndexedTile.CopyToScalar(in _tileData, IndexedTile.TileDimension, ref destination._reference, destination._bytesPerRow);
	}

	/// <inheritdoc/>
	public readonly void CopyTo(scoped ref SnesMode7Tile destination) {
		destination = this;
	}
}
