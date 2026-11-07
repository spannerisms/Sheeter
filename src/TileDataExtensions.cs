namespace Sheeter;

/// <summary>
/// Provides extension methods for <see cref="ITileData{TSelf}"/> and collections of.
/// </summary>
public static class TileDataExtensions {

	extension<TFormat>(TFormat _) where TFormat : unmanaged, ITileData<TFormat> {
		/// <summary>
		/// Reinterprets a span of bytes as a span of tiles.
		/// </summary>
		/// <param name="source">The span of bytes to reinterpret.</param>
		/// <returns>A new span of tiles.</returns>
		/// <remarks>
		/// The length of <paramref name="source"/> must be
		/// a multiple of <typeparamref name="TFormat"/>.Length.
		/// </remarks>
		[OverloadResolutionPriority(1)]
		public static Span<TFormat> CreateSpanFromBytes(Span<byte> source) {
			return MemoryHelpers.CreateSpanFromBytes<TFormat>(source);
		}

		/// <inheritdoc cref="CreateSpanFromBytes{TFormat}(Span{byte})"/>
		public static ReadOnlySpan<TFormat> CreateSpanFromBytes(ReadOnlySpan<byte> source) {
			return MemoryHelpers.CreateSpanFromBytes<TFormat>(source);
		}

		/// <summary>
		/// Reinterprets a span of bytes as a span of tiles.
		/// </summary>
		/// <param name="source">The span of bytes to reinterpret.</param>
		/// <param name="offset">The offset into the span to start at.</param>
		/// <param name="count">The number of tiles to create from the span.</param>
		/// <returns>A new span of tiles.</returns>
		/// <exception cref="ArgumentException">If the span is an invalid length.</exception>
		[OverloadResolutionPriority(1)]
		public static Span<TFormat> CreateSpanFromBytes(Span<byte> source, int offset, int count) {
			return MemoryHelpers.CreateSpanFromBytes<TFormat>(source, offset, count);
		}

		/// <inheritdoc cref="CreateSpanFromBytes{TFormat}(Span{byte}, int, int)"/>
		public static ReadOnlySpan<TFormat> CreateSpanFromBytes(ReadOnlySpan<byte> source, int offset, int count) {
			return MemoryHelpers.CreateSpanFromBytes<TFormat>(source, offset, count);
		}

		/// <summary>
		/// Tries to reinterpret a span of bytes as a span of tiles.
		/// </summary>
		/// <param name="source">The span of bytes to reinterpret.</param>
		/// <param name="offset">The offset into the span to start at.</param>
		/// <param name="count">The number of tiles to create from the span.</param>
		/// <param name="tiles">
		/// If this conversion is successful, this will contain a span of the resulting conversion.
		/// </param>
		/// <returns>
		/// <see langword="true"/> if there were enough bytes in <paramref name="source"/> to create a span.
		/// </returns>
		[OverloadResolutionPriority(1)]
		public static bool TryCreateSpanFromBytes(Span<byte> source, int offset, int count, out Span<TFormat> tiles) {
			tiles = MemoryHelpers.TryCreateSpanFromBytes<TFormat>(source, offset, count, out bool ret);
			return ret;
		}

		/// <inheritdoc cref="TryCreateSpanFromBytes{TFormat}(Span{byte}, int, int, out Span{TFormat})"/>
		public static bool TryCreateSpanFromBytes(ReadOnlySpan<byte> source, int offset, int count, out ReadOnlySpan<TFormat> tiles) {
			tiles = MemoryHelpers.TryCreateSpanFromBytes<TFormat>(source, offset, count, out bool ret);
			return ret;
		}

		/// <inheritdoc cref="TryCreateSpanFromBytes{TFormat}(Span{byte}, int, int, out Span{TFormat})"/>
		/// <remarks>
		/// This requires that the length of <paramref name="source"/> be a multiple of
		/// the <typeparamref name="TFormat"/>.Length.
		/// </remarks>
		[OverloadResolutionPriority(1)]
		public static bool TryCreateSpanFromBytes(Span<byte> source, out Span<TFormat> tiles) {
			tiles = MemoryHelpers.TryCreateSpanFromBytes<TFormat>(source, out bool ret);
			return ret;
		}

		/// <inheritdoc cref="TryCreateSpanFromBytes{TFormat}(Span{byte}, out Span{TFormat})"/>
		public static bool TryCreateSpanFromBytes(ReadOnlySpan<byte> source, out ReadOnlySpan<TFormat> tiles) {
			tiles = MemoryHelpers.TryCreateSpanFromBytes<TFormat>(source, out bool ret);
			return ret;
		}
	}

	extension<TFormat>(ReadOnlySpan<TFormat> tiles) where TFormat : unmanaged, ITileData<TFormat> {
		/// <summary>
		/// Creates a read-only span of bytes over a collection of tiles.
		/// </summary>
		/// <returns>A read-only span.</returns>
		public ReadOnlySpan<byte> AsBytes() {
			return MemoryMarshal.AsBytes(tiles);
		}
	}

	extension<TFormat>(Span<TFormat> tiles) where TFormat : unmanaged, ITileData<TFormat> {
		/// <summary>
		/// Creates a span of bytes over a collection of tiles.
		/// </summary>
		/// <returns>A span.</returns>
		[OverloadResolutionPriority(1)]
		public Span<byte> AsBytes() {
			return MemoryMarshal.AsBytes(tiles);
		}
	}

	extension<TFormat>(TFormat[] tiles) where TFormat : unmanaged, ITileData<TFormat> {
		/// <inheritdoc cref="AsBytes{TFormat}(Span{TFormat})"/>
		public Span<byte> AsBytes() {
			ArgumentNullException.ThrowIfNull(tiles);
			return tiles.AsSpan().AsBytes();
		}
	}

	extension<TFormat>(scoped ReadOnlySpan<TFormat> tiles) where TFormat : unmanaged, IConvertibleTile<TFormat> {
		/// <inheritdoc cref="IndexedTile.ConvertAllToIndexed{TFormat}(ReadOnlySpan{TFormat})"/>
		public IndexedTile[] ConvertAllToIndexed() {
			return IndexedTile.ConvertAllToIndexed(tiles);
		}

		/// <inheritdoc cref="IndexedTile.ConvertAllToIndexed{TFormat}(ReadOnlySpan{TFormat}, Span{IndexedTile})"/>
		public void ConvertAllToIndexed(scoped Span<IndexedTile> destination) {
			IndexedTile.ConvertAllToIndexed(tiles, destination);
		}
	}

	extension<TFormat>(scoped Span<TFormat> tiles) where TFormat : unmanaged, IConvertibleTile<TFormat> {
		/// <inheritdoc cref="IndexedTile.ConvertAllToIndexed{TFormat}(ReadOnlySpan{TFormat})"/>
		public IndexedTile[] ConvertAllToIndexed() {
			return IndexedTile.ConvertAllToIndexed(tiles);
		}

		/// <inheritdoc cref="IndexedTile.ConvertAllToIndexed{TFormat}(ReadOnlySpan{TFormat}, Span{IndexedTile})"/>
		public void ConvertAllToIndexed(scoped Span<IndexedTile> destination) {
			IndexedTile.ConvertAllToIndexed(tiles, destination);
		}
	}

	extension<TFormat>(TFormat[] tiles) where TFormat : unmanaged, IConvertibleTile<TFormat> {
		/// <inheritdoc cref="IndexedTile.ConvertAllToIndexed{TFormat}(ReadOnlySpan{TFormat})"/>
		public IndexedTile[] ConvertAllToIndexed() {
			ArgumentNullException.ThrowIfNull(tiles);
			return IndexedTile.ConvertAllToIndexed(tiles);
		}

		/// <inheritdoc cref="IndexedTile.ConvertAllToIndexed{TFormat}(ReadOnlySpan{TFormat}, Span{IndexedTile})"/>
		public void ConvertAllToIndexed(scoped Span<IndexedTile> destination) {
			ArgumentNullException.ThrowIfNull(tiles);
			IndexedTile.ConvertAllToIndexed(tiles, destination);
		}
	}

	extension(scoped ReadOnlySpan<IndexedTile> tiles) {
		/// <inheritdoc cref="IndexedTile.ConvertAllTo{TFormat}(ReadOnlySpan{IndexedTile})"/>
		public TFormat[] ConvertAllTo<TFormat>() where TFormat : unmanaged, IConvertibleTile<TFormat> {
			return IndexedTile.ConvertAllTo<TFormat>(tiles);
		}

		/// <inheritdoc cref="IndexedTile.ConvertAllTo{TFormat}(ReadOnlySpan{IndexedTile}, Span{TFormat})"/>
		public void ConvertAllTo<TFormat>(scoped Span<TFormat> destination) where TFormat : unmanaged, IConvertibleTile<TFormat> {
			IndexedTile.ConvertAllTo(tiles, destination);
		}
	}

	extension(scoped Span<IndexedTile> tiles) {
		/// <inheritdoc cref="IndexedTile.ConvertAllTo{TFormat}(ReadOnlySpan{IndexedTile})"/>
		public TFormat[] ConvertAllTo<TFormat>() where TFormat : unmanaged, IConvertibleTile<TFormat> {
			return IndexedTile.ConvertAllTo<TFormat>(tiles);
		}

		/// <inheritdoc cref="IndexedTile.ConvertAllTo{TFormat}(ReadOnlySpan{IndexedTile}, Span{TFormat})"/>
		public void ConvertAllTo<TFormat>(scoped Span<TFormat> destination) where TFormat : unmanaged, IConvertibleTile<TFormat> {
			IndexedTile.ConvertAllTo(tiles, destination);
		}
	}

	extension(IndexedTile[] tiles) {
		/// <inheritdoc cref="IndexedTile.ConvertAllTo{T}(ReadOnlySpan{IndexedTile})"/>
		public TFormat[] ConvertAllTo<TFormat>() where TFormat : unmanaged, IConvertibleTile<TFormat> {
			ArgumentNullException.ThrowIfNull(tiles);
			return IndexedTile.ConvertAllTo<TFormat>(tiles);
		}

		/// <inheritdoc cref="IndexedTile.ConvertAllTo{TFormat}(ReadOnlySpan{IndexedTile}, Span{TFormat})"/>
		public void ConvertAllTo<TFormat>(scoped Span<TFormat> destination) where TFormat : unmanaged, IConvertibleTile<TFormat> {
			ArgumentNullException.ThrowIfNull(tiles);
			IndexedTile.ConvertAllTo(tiles, destination);
		}
	}

	extension(scoped ReadOnlySpan<GraphicsTile> tiles) {
		/// <inheritdoc cref="GraphicsTile.ConvertAllTo{T}(ReadOnlySpan{GraphicsTile})"/>
		public TFormat[] ConvertAllTo<TFormat>() where TFormat : unmanaged, IConvertibleTile<TFormat> {
			return GraphicsTile.ConvertAllTo<TFormat>(tiles);
		}

		/// <inheritdoc cref="GraphicsTile.ConvertAllTo{TFormat}(ReadOnlySpan{GraphicsTile}, Span{TFormat})"/>
		public void ConvertAllTo<TFormat>(scoped Span<TFormat> destination) where TFormat : unmanaged, IConvertibleTile<TFormat> {
			GraphicsTile.ConvertAllTo(tiles, destination);
		}
	}

	extension(scoped Span<GraphicsTile> tiles) {
		/// <inheritdoc cref="GraphicsTile.ConvertAllTo{T}(ReadOnlySpan{GraphicsTile})"/>
		public TFormat[] ConvertAllTo<TFormat>() where TFormat : unmanaged, IConvertibleTile<TFormat> {
			return GraphicsTile.ConvertAllTo<TFormat>(tiles);
		}

		/// <inheritdoc cref="GraphicsTile.ConvertAllTo{TFormat}(ReadOnlySpan{GraphicsTile}, Span{TFormat})"/>
		public void ConvertAllTo<TFormat>(scoped Span<TFormat> destination) where TFormat : unmanaged, IConvertibleTile<TFormat> {
			GraphicsTile.ConvertAllTo(tiles, destination);
		}
	}

	extension(GraphicsTile[] tiles) {
		/// <inheritdoc cref="GraphicsTile.ConvertAllTo{T}(ReadOnlySpan{GraphicsTile})"/>
		public TFormat[] ConvertAllTo<TFormat>() where TFormat : unmanaged, IConvertibleTile<TFormat> {
			ArgumentNullException.ThrowIfNull(tiles);
			return GraphicsTile.ConvertAllTo<TFormat>(tiles);
		}

		/// <inheritdoc cref="GraphicsTile.ConvertAllTo{TFormat}(ReadOnlySpan{GraphicsTile}, Span{TFormat})"/>
		public void ConvertAllTo<TFormat>(scoped Span<TFormat> destination) where TFormat : unmanaged, IConvertibleTile<TFormat> {
			ArgumentNullException.ThrowIfNull(tiles);
			GraphicsTile.ConvertAllTo(tiles, destination);
		}
	}
}
