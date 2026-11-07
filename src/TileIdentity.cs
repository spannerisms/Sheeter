namespace Sheeter;

/// <summary>
/// Represents basic information about a tile within a tilemap.
/// </summary>
public sealed record TileIdentity {
	/// <summary>
	/// Gets the offset of this tile in the list of tile names.
	/// </summary>
	public int NameOffset { get; init; }

	/// <summary>
	/// Gets the flip of this tile.
	/// </summary>
	public TileFlip Flip { get; init; }

	/// <summary>
	/// Gets whether this tile should be considered empty, irrespective of its tile name.
	/// </summary>
	public bool IsEmpty { get; init; }

	/// <summary>
	/// Represents the empty tile.
	/// </summary>
	public static readonly TileIdentity Empty = new() {
		IsEmpty = true
	};

	/// <summary>
	/// Creates a tilemap where each non empty tile is given a successive tile name,
	/// starting from 1. 
	/// </summary>
	/// <param name="tiles">The character tiles to use.</param>
	/// <returns>A new array of tilemap identities.</returns>
	public static TileIdentity[] CreateIncrementingTilemap(scoped ReadOnlySpan<GraphicsTile> tiles) {
		TileIdentity[] tilemap = new TileIdentity[tiles.Length];

		int indexOut = 0;
		int indexIn = 1;

		foreach (GraphicsTile tile in tiles) {
			tilemap[indexOut++] = !tile.IsEmpty()
				? new TileIdentity { NameOffset = indexIn++ }
				: Empty;
		}

		return tilemap;
	}

	/// <summary>
	/// Creates an optimized tilemap by reusing matching tiles, including via reflectiong, where possible.
	/// </summary>
	/// <param name="imageTiles">The image tiles to optimize.</param>
	/// <param name="graphicsTiles">The character tiles to use. This collection should already be pruned of duplicates.</param>
	/// <returns>A new array of tilemap identities.</returns>
	public static TileIdentity[] CreateOptimizedTilemap(scoped ReadOnlySpan<GraphicsTile> imageTiles, scoped ReadOnlySpan<GraphicsTile> graphicsTiles) {
		return CreateOptimizedTilemap(imageTiles, graphicsTiles, GraphicsTileComparer.Flipped);
	}

	/// <summary>
	/// Creates an optimized tilemap by reusing exact tiles where possible.
	/// </summary>
	/// <inheritdoc cref="CreateOptimizedTilemap(ReadOnlySpan{GraphicsTile}, ReadOnlySpan{GraphicsTile})" path="//param|//returns"/>
	public static TileIdentity[] CreateOptimizedTilemapNoReflection(scoped ReadOnlySpan<GraphicsTile> imageTiles, scoped ReadOnlySpan<GraphicsTile> graphicsTiles) {
		return CreateOptimizedTilemap(imageTiles, graphicsTiles, GraphicsTileComparer.Exact);
	}

	private static TileIdentity[] CreateOptimizedTilemap(scoped ReadOnlySpan<GraphicsTile> imageTiles, scoped ReadOnlySpan<GraphicsTile> graphicsTiles, IEqualityComparer<GraphicsTile> comparer) {
		int len = imageTiles.Length;

		TileIdentity[] tilemap = new TileIdentity[imageTiles.Length];

		// create a lookup for the tiles

		Dictionary<GraphicsTile, GraphicsId> dict = new(comparer);
		int currentName = 0;

		foreach (GraphicsTile tile in graphicsTiles) {
			if (dict.TryAdd(tile, new(tile, currentName))) {
				currentName++;
			}
		}

		// go through the image and find matches in the graphics list

		for (int i = 0; i < len; i++) {
			GraphicsTile tile = imageTiles[i];

			if (tile.IsEmpty()) {
				tilemap[i] = Empty;

			} else if (dict.TryGetValue(tile, out GraphicsId info)) {
				tilemap[i] = new TileIdentity {
					NameOffset = info.Name,
					Flip = tile.Matches(info.Tile).ToFlip()
				};

			} else {
				tilemap[i] = Empty;
			}
		}

		return tilemap;
	}

	/// <summary>
	/// Cheat the system and store a key with its value.
	/// </summary>
	private readonly struct GraphicsId(GraphicsTile tile, int name) {
		public readonly GraphicsTile Tile = tile;
		public readonly int Name = name;
	}
}
