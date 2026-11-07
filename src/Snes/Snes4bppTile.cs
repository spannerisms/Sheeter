using System.Diagnostics.CodeAnalysis;

namespace Sheeter.Snes;

/// <summary>
/// Represents a block of data containing a tile encoded at 4-bits-per-pixel
/// and interleaved by row per bit plane.
/// </summary>
/// <inheritdoc cref="IndexedTile" path="//remarks"/>
[InlineArray(Length)]
public struct Snes4bppTile : IConvertibleTile<Snes4bppTile> {
	/// <inheritdoc cref="ITileData{TSelf}.BitsPerPixel"/>
	public const int BitsPerPixel = 4;

	/// <inheritdoc cref="ITileData{TSelf}.ColorsPerPixel"/>
	public const int ColorsPerPixel = 1 << BitsPerPixel;

	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = BitsPerPixel * 8;

	internal byte _tileData;

	readonly ref byte ITileData<Snes4bppTile>.Reference {
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef(in _tileData);
	}

	/// <inheritdoc/>
	[UnscopedRef]
	public readonly ReadOnlySpan<byte> AsSpan() => this;

	static int ITileData<Snes4bppTile>.BitsPerPixel => BitsPerPixel;
	static int ITileData<Snes4bppTile>.Length => Length;
	static ConversionType IConvertibleTile<Snes4bppTile>.ConversionType => ConversionType.SnesInterleaved;

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped ref IndexedTile destination) {
		ConversionHelpers.PlanarToIndexed<Snes4bppTile>(in _tileData, ref destination._tileData);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		ToIndexedTile(ref destination._indexedTile);
	}

	/// <inheritdoc/>
	public readonly void ToIndexedTile(scoped IndexedTileViewport destination) {
		ConversionHelpers.PlanarToIndexed<Snes4bppTile>(in _tileData, destination);
	}

	/// <inheritdoc/>
	public readonly void CopyTo(scoped ref Snes4bppTile destination) {
		destination = this;
	}

	/// <inheritdoc cref="Packed1bppTile.Widen(ref Packed2bppTile)"/>
	public readonly void Widen(scoped ref Snes8bppTile destination) {
		ConversionHelpers.WidenSnesHelper(in this, ref destination);
	}

	/// <summary>
	/// Widens a collection of tiles to 8 bits-per-pixel.
	/// </summary>
	/// <param name="source">The collection of tiles to widen.</param>
	/// <returns>A new array of tiles.</returns>
	public static Snes8bppTile[] WidenAllTo8bpp(ReadOnlySpan<Snes4bppTile> source) {
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
	public static void WidenAllTo8bpp(ReadOnlySpan<Snes4bppTile> source, Span<Snes8bppTile> destination) {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (ref readonly Snes4bppTile tile in source) {
			tile.Widen(ref destination[i++]);
		}
	}
}
