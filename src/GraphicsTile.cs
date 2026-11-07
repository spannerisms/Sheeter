using System.Buffers;
using System.ComponentModel;

namespace Sheeter;

/// <summary>
/// Represents an 8-by-8 pixel square of indexed color values.
/// <para>
/// This is a managed wrapper for <see cref="IndexedTile"/>
/// to faciliate more direct manipulation without the performance concerns
/// inherent to large struct types.
/// </para>
/// </summary>
public sealed class GraphicsTile : IEquatable<GraphicsTile> {
	/// <inheritdoc cref="ITileData{TSelf}.Length"/>
	public const int Length = IndexedTile.Length;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	internal IndexedTile _indexedTile;

	/// <summary>
	/// Creates a new, empty, managed graphics tile.
	/// </summary>
	public GraphicsTile() {
		_indexedTile = default;
	}

	/// <summary>
	/// Creates a new managed indexed tile with a copy of <paramref name="tile"/>.
	/// </summary>
	/// <param name="tile">The tile to copy.</param>
	public GraphicsTile(scoped in IndexedTile tile) {
		_indexedTile = tile;
	}

	/// <inheritdoc cref="GraphicsTile(in IndexedTile)"/>
	public GraphicsTile(scoped IndexedTileViewport tile) {
		tile.CopyTo(ref _indexedTile);
	}

	/// <summary>
	/// Creates a new managed indexed tile with a deep copy of <paramref name="data"/>.
	/// </summary>
	/// <param name="data">The indexed data to copy. This collection must contain exactly 64 bytes.</param>
	/// <exception cref="ArgumentException">If <paramref name="data"/> is not exactly 64 bytes.</exception>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public GraphicsTile(scoped ReadOnlySpan<byte> data) {
		if (data.Length is not Length) {
			ErrorHelpers.ThrowBadArgument("Input data must be exactly 64 bytes.");
		}

		data.CopyTo(_indexedTile);
	}

	/// <summary>
	/// Gets or sets the pixel at the specified offset into this tile.
	/// </summary>
	/// <param name="i">The index to select [0..63].</param>
	/// <exception cref="IndexOutOfRangeException">If the selected pixel is outside of the tile.</exception>
	public byte this[int i] {
		get => _indexedTile[i];
		set => _indexedTile[i] = value;
	}

	/// <summary>
	/// Gets or sets the pixel at the given position in this tile.
	/// </summary>
	/// <param name="x">The X coordinate of the pixel to select [0..7].</param>
	/// <param name="y">The Y coordinate of the pixel to select [0..7].</param>
	/// <exception cref="IndexOutOfRangeException">If the selected pixel is outside of the tile.</exception>
	public byte this[int x, int y] {
		get => _indexedTile[x, y];
		set => _indexedTile[x, y] = value;
	}

	/// <summary>
	/// Returns a managed pointer to the underlying tile data of this instance.
	/// </summary>
	/// <returns>A managed <see cref="IndexedTile"/> pointer.</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public ref IndexedTile GetDataReference() {
		return ref _indexedTile;
	}

	/// <inheritdoc cref="IndexedTile.ConvertTo{TFormat}(ref TFormat)"/>
	public void ConvertTo<TFormat>(scoped ref TFormat destination)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		_indexedTile.ConvertTo(ref destination);
	}

	/// <inheritdoc cref="IndexedTile.CopyTo(ref IndexedTile)"/>
	public void CopyTo(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		_indexedTile.CopyTo(ref destination._indexedTile);
	}

	/// <inheritdoc cref="IndexedTile.CopyTo(ref IndexedTile)"/>
	public void CopyTo(scoped ref IndexedTile destination) {
		_indexedTile.CopyTo(ref destination);
	}

	/// <inheritdoc cref="IndexedTile.CopyTo(ref IndexedTile)"/>
	public void CopyTo(scoped IndexedTileViewport destination) {
		_indexedTile.CopyTo(destination);
	}

	/// <inheritdoc cref="IndexedTile.CopyToFlipped(ref IndexedTile, TileFlip)"/>
	public void CopyToFlipped(GraphicsTile destination, TileFlip flip) {
		ArgumentNullException.ThrowIfNull(destination);
		_indexedTile.CopyToFlipped(ref destination._indexedTile, flip);
	}

	/// <inheritdoc cref="IndexedTile.CopyToFlipped(ref IndexedTile, TileFlip)"/>
	public void CopyToFlipped(scoped ref IndexedTile destination, TileFlip flip) {
		_indexedTile.CopyToFlipped(ref destination, flip);
	}

	/// <inheritdoc cref="IndexedTile.CopyToFlipped(ref IndexedTile, TileFlip)"/>
	public void CopyToFlipped(scoped IndexedTileViewport destination, TileFlip flip) {
		_indexedTile.CopyToFlipped(destination, flip);
	}

	/// <inheritdoc cref="IndexedTile.CopyToHFlipped(ref IndexedTile)"/>
	public void CopyToHFlipped(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		_indexedTile.CopyToHFlipped(ref destination._indexedTile);
	}

	/// <inheritdoc cref="IndexedTile.CopyToHFlipped(ref IndexedTile)"/>
	public void CopyToHFlipped(scoped ref IndexedTile destination) {
		_indexedTile.CopyToHFlipped(ref destination);
	}

	/// <inheritdoc cref="IndexedTile.CopyToHFlipped(ref IndexedTile)"/>
	public void CopyToHFlipped(scoped IndexedTileViewport destination) {
		_indexedTile.CopyToHFlipped(destination);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVFlipped(ref IndexedTile)"/>
	public void CopyToVFlipped(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		_indexedTile.CopyToVFlipped(ref destination._indexedTile);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVFlipped(ref IndexedTile)"/>
	public void CopyToVFlipped(scoped ref IndexedTile destination) {
		_indexedTile.CopyToVFlipped(ref destination);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVFlipped(ref IndexedTile)"/>
	public void CopyToVFlipped(scoped IndexedTileViewport destination) {
		_indexedTile.CopyToVFlipped(destination);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVHFlipped(ref IndexedTile)"/>
	public void CopyToVHFlipped(GraphicsTile destination) {
		ArgumentNullException.ThrowIfNull(destination);
		_indexedTile.CopyToVHFlipped(ref destination._indexedTile);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVHFlipped(ref IndexedTile)"/>
	public void CopyToVHFlipped(scoped ref IndexedTile destination) {
		_indexedTile.CopyToVHFlipped(ref destination);
	}

	/// <inheritdoc cref="IndexedTile.CopyToVHFlipped(ref IndexedTile)"/>
	public void CopyToVHFlipped(scoped IndexedTileViewport destination) {
		_indexedTile.CopyToVHFlipped(destination);
	}

	/// <inheritdoc cref="IndexedTile.AsSpan"/>
	public ReadOnlySpan<byte> AsSpan() {
		return _indexedTile;
	}

	/// <inheritdoc cref="IndexedTile.IsEmpty"/>
	public bool IsEmpty() {
		return _indexedTile.IsEmpty();
	}

	/// <inheritdoc cref="IndexedTile.Matches(ref readonly IndexedTile)"/>
	public MatchResult Matches(GraphicsTile other) {
		ArgumentNullException.ThrowIfNull(other);
		return _indexedTile.Matches(in other._indexedTile);
	}

	/// <inheritdoc cref="IndexedTile.Matches(ref readonly IndexedTile)"/>
	public MatchResult Matches(scoped ref readonly IndexedTile other) {
		return _indexedTile.Matches(in other);
	}

	/// <inheritdoc cref="IndexedTile.Matches(ref readonly IndexedTile)"/>
	public MatchResult Matches(scoped IndexedTileViewport other) {
		return _indexedTile.Matches(other);
	}

	/// <inheritdoc cref="IndexedTile.Equals(ref readonly IndexedTile)"/>
	[OverloadResolutionPriority(1)]
	public bool Equals(GraphicsTile? other) {
		if (other is null) {
			return false;

		} else {
			return Matches(other) is MatchResult.Same;
		}
	}

	/// <inheritdoc cref="IndexedTile.Equals(ref readonly IndexedTile)"/>
	[OverloadResolutionPriority(1)]
	public bool Equals(scoped /* using `in` to snipe `Equals(object?)` */ in IndexedTile other) {
		return _indexedTile.Equals(in other);
	}

	/// <inheritdoc cref="IndexedTile.Equals(ref readonly IndexedTile)"/>
	[OverloadResolutionPriority(1)]
	public bool Equals(scoped IndexedTileViewport other) {
		return _indexedTile.Equals(other);
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj switch {
		GraphicsTile g => Equals(g),
		_ => false
	};

	/// <inheritdoc/>
	public override int GetHashCode() {
		HashCode h = new();
		h.AddBytes(_indexedTile);
		return h.ToHashCode();
	}

	/// <summary>
	/// Prunes an array of tiles of empty entries.
	/// </summary>
	/// <param name="tiles">The array of tiles to prune.</param>
	/// <returns>A new array containing only the non-empty members of <paramref name="tiles"/>.</returns>
	public static GraphicsTile[] RemoveEmptyTiles(scoped ReadOnlySpan<GraphicsTile> tiles) {
		GraphicsTile[] tmp = ArrayPool<GraphicsTile>.Shared.Rent(tiles.Length);

		try {
			int newSize = 0;

			foreach (GraphicsTile item in tiles) {
				if (!item.IsEmpty()) {
					tmp[newSize++] = item;
				}
			}

			GraphicsTile[] ret = tmp[..newSize];
			return ret;

		} finally {
			ArrayPool<GraphicsTile>.Shared.Return(tmp);
		}
	}

	/// <inheritdoc cref="RemoveDuplicateTiles(ReadOnlySpan{GraphicsTile})"/>
	[OverloadResolutionPriority(-1)]
	public static GraphicsTile[] RemoveEmptyTiles(IEnumerable<GraphicsTile> tiles) {
		GraphicsTile[] tmp = ArrayPool<GraphicsTile>.Shared.Rent(tiles.Count());

		try {
			int newSize = 0;

			foreach (GraphicsTile item in tiles) {
				if (!item.IsEmpty()) {
					tmp[newSize++] = item;
				}
			}

			GraphicsTile[] ret = tmp[..newSize];
			return ret;

		} finally {
			ArrayPool<GraphicsTile>.Shared.Return(tmp);
		}
	}

	/// <summary>
	/// Prunes a collection of tiles of duplicate entries.
	/// </summary>
	/// <param name="tiles">The collection of tiles to prune.</param>
	/// <returns>A new array containing only the unique members of <paramref name="tiles"/>.</returns>
	public static GraphicsTile[] RemoveDuplicateTiles(scoped ReadOnlySpan<GraphicsTile> tiles) {
		return RemoveMatchingTiles(tiles, GraphicsTileComparer.Exact);
	}

	/// <summary>
	/// Prunes a collection of tiles of duplicate entries, including those that match via reflection.
	/// </summary>
	/// <param name="tiles">The collection of tiles to prune.</param>
	/// <returns>A new array containing only the unique members of <paramref name="tiles"/>.</returns>
	public static GraphicsTile[] RemoveMatchingTiles(scoped ReadOnlySpan<GraphicsTile> tiles) {
		return RemoveMatchingTiles(tiles, GraphicsTileComparer.Flipped);
	}

	/// <summary>
	/// Prunes a collection of tiles of both empty and duplicate entries.
	/// </summary>
	/// <param name="tiles">The collection of tiles to prune.</param>
	/// <returns>A new array containing only the unique, non-empty members of <paramref name="tiles"/>.</returns>
	public static GraphicsTile[] RemoveEmptyAndDuplicateTiles(scoped ReadOnlySpan<GraphicsTile> tiles) {
		return RemoveEmptyAndMatchingTiles(tiles, GraphicsTileComparer.Exact);
	}

	/// <summary>
	/// Prunes a collection of tiles of both empty and duplicate entries, including those that match via reflection.
	/// </summary>
	/// <param name="tiles">The collection of tiles to prune.</param>
	/// <returns>A new array containing only the unique, non-empty members of <paramref name="tiles"/>.</returns>
	public static GraphicsTile[] RemoveEmptyAndMatchingTiles(scoped ReadOnlySpan<GraphicsTile> tiles) {
		return RemoveEmptyAndMatchingTiles(tiles, GraphicsTileComparer.Flipped);
	}

	private static GraphicsTile[] RemoveMatchingTiles(scoped ReadOnlySpan<GraphicsTile> tiles, IEqualityComparer<GraphicsTile> comparer) {
		GraphicsTile[] tmp = ArrayPool<GraphicsTile>.Shared.Rent(tiles.Length);

		HashSet<GraphicsTile> tester = new(comparer);

		try {
			int newSize = 0;

			foreach (GraphicsTile item in tiles) {
				if (tester.Add(item)) {
					tmp[newSize++] = item;
				}
			}

			GraphicsTile[] ret = tmp[..newSize];
			return ret;

		} finally {
			ArrayPool<GraphicsTile>.Shared.Return(tmp);
		}
	}

	private static GraphicsTile[] RemoveEmptyAndMatchingTiles(scoped ReadOnlySpan<GraphicsTile> tiles, IEqualityComparer<GraphicsTile> comparer) {
		GraphicsTile[] tmp = ArrayPool<GraphicsTile>.Shared.Rent(tiles.Length);

		HashSet<GraphicsTile> tester = new(comparer);

		try {
			int newSize = 0;

			foreach (GraphicsTile item in tiles) {
				if (!item.IsEmpty()) {
					if (tester.Add(item)) {
						tmp[newSize++] = item;
					}
				}
			}

			GraphicsTile[] ret = tmp[..newSize];
			return ret;

		} finally {
			ArrayPool<GraphicsTile>.Shared.Return(tmp);
		}
	}

	/// <inheritdoc cref="RemoveDuplicateTiles(ReadOnlySpan{GraphicsTile})"/>
	[OverloadResolutionPriority(-1)]
	public static GraphicsTile[] RemoveDuplicateTiles(IEnumerable<GraphicsTile> tiles) {
		return RemoveMatchingTiles(tiles, GraphicsTileComparer.Exact);
	}

	/// <inheritdoc cref="RemoveMatchingTiles(ReadOnlySpan{GraphicsTile})"/>
	[OverloadResolutionPriority(-1)]
	public static GraphicsTile[] RemoveMatchingTiles(IEnumerable<GraphicsTile> tiles) {
		return RemoveMatchingTiles(tiles, GraphicsTileComparer.Flipped);
	}

	/// <inheritdoc cref="RemoveEmptyAndDuplicateTiles(ReadOnlySpan{GraphicsTile})"/>
	[OverloadResolutionPriority(-1)]
	public static GraphicsTile[] RemoveEmptyAndDuplicateTiles(IEnumerable<GraphicsTile> tiles) {
		return RemoveEmptyAndMatchingTiles(tiles, GraphicsTileComparer.Exact);
	}

	/// <inheritdoc cref="RemoveEmptyAndMatchingTiles(ReadOnlySpan{GraphicsTile})"/>
	[OverloadResolutionPriority(-1)]
	public static GraphicsTile[] RemoveEmptyAndMatchingTiles(IEnumerable<GraphicsTile> tiles) {
		return RemoveEmptyAndMatchingTiles(tiles, GraphicsTileComparer.Flipped);
	}

	[OverloadResolutionPriority(-1)]
	private static GraphicsTile[] RemoveMatchingTiles(IEnumerable<GraphicsTile> tiles, IEqualityComparer<GraphicsTile> comparer) {
		GraphicsTile[] tmp = ArrayPool<GraphicsTile>.Shared.Rent(tiles.Count());

		HashSet<GraphicsTile> tester = new(comparer);

		try {
			int newSize = 0;

			foreach (GraphicsTile item in tiles) {
				if (tester.Add(item)) {
					tmp[newSize++] = item;
				}
			}

			GraphicsTile[] ret = tmp[..newSize];
			return ret;

		} finally {
			ArrayPool<GraphicsTile>.Shared.Return(tmp);
		}
	}

	[OverloadResolutionPriority(-1)]
	private static GraphicsTile[] RemoveEmptyAndMatchingTiles(IEnumerable<GraphicsTile> tiles, IEqualityComparer<GraphicsTile> comparer) {
		GraphicsTile[] tmp = ArrayPool<GraphicsTile>.Shared.Rent(tiles.Count());

		HashSet<GraphicsTile> tester = new(comparer);

		try {
			int newSize = 0;

			foreach (GraphicsTile item in tiles) {
				if (!item.IsEmpty()) {
					if (tester.Add(item)) {
						tmp[newSize++] = item;
					}
				}
			}

			GraphicsTile[] ret = tmp[..newSize];
			return ret;

		} finally {
			ArrayPool<GraphicsTile>.Shared.Return(tmp);
		}
	}

	/// <summary>
	/// Creates managed objects for every unmanaged tile in <paramref name="source"/>.
	/// </summary>
	/// <param name="source">The source collection of indexed tiles.</param>
	/// <returns>A new array of tiles.</returns>
	public static GraphicsTile[] CreateFromUnmanaged(scoped ReadOnlySpan<IndexedTile> source) {
		GraphicsTile[] ret = new GraphicsTile[source.Length];

		int i = 0;
		foreach (ref readonly IndexedTile tile in source) {
			ret[i++] = new(in tile);
		}

		return ret;
	}

	/// <inheritdoc cref="IndexedTile.ConvertAllTo{TFormat}(ReadOnlySpan{IndexedTile}, Span{TFormat})"/>
	public static void ConvertAllTo<TFormat>(scoped ReadOnlySpan<GraphicsTile> source, scoped Span<TFormat> destination)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		ErrorHelpers.ThrowIfDestinationTooShort(source.Length, destination.Length);

		int i = 0;
		foreach (GraphicsTile block in source) {
			block.ConvertTo(ref destination[i++]);
		}
	}

	/// <inheritdoc cref="IndexedTile.ConvertAllTo{TFormat}(ReadOnlySpan{IndexedTile})"/>
	public static TFormat[] ConvertAllTo<TFormat>(scoped ReadOnlySpan<GraphicsTile> source)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		TFormat[] ret = GC.AllocateUninitializedArray<TFormat>(source.Length);
		ConvertAllTo(source, ret);
		return ret;
	}

	/// <inheritdoc cref="IndexedTile.ConvertAllTo{TFormat}(ReadOnlySpan{IndexedTile})"/>
	[OverloadResolutionPriority(-1)]
	public static TFormat[] ConvertAllTo<TFormat>(IEnumerable<GraphicsTile> source)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		TFormat[] ret = GC.AllocateUninitializedArray<TFormat>(source.Count());

		int i = 0;
		foreach (GraphicsTile block in source) {
			block.ConvertTo(ref ret[i++]);
		}

		return ret;
	}

	/// <summary>
	/// Converts all tile data in a collection to tile data in a new collection.
	/// </summary>
	/// <typeparam name="TFormat">The tile type to convert to.</typeparam>
	/// <param name="source">The collection of tiles to convert.</param>
	/// <returns>A new array of <see cref="GraphicsTile"/> objects.</returns>
	public static GraphicsTile[] DeconvertAllTiles<TFormat>(scoped ReadOnlySpan<TFormat> source)
			where TFormat : unmanaged, IConvertibleTile<TFormat> {
		GraphicsTile[] ret = new GraphicsTile[source.Length];

		int i = 0;
		foreach (ref readonly TFormat block in source) {
			GraphicsTile tmp = new();
			block.ToIndexedTile(ref tmp._indexedTile);
			ret[i++] = tmp;
		}

		return ret;
	}

	/// <summary>
	/// Returns an emumerator for this instance.
	/// </summary>
	public Span<byte>.Enumerator GetEnumerator() {
		return ((Span<byte>) _indexedTile).GetEnumerator();
	}

	/// <summary>
	/// Creates a deep copy of this instance.
	/// </summary>
	/// <returns>A new tile.</returns>
	public GraphicsTile Clone() {
		return new(in _indexedTile);
	}
}
