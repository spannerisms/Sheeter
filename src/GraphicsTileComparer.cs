using System.Diagnostics.CodeAnalysis;

namespace Sheeter;

internal static class GraphicsTileComparer {
	public static readonly IEqualityComparer<GraphicsTile> Exact = new GraphicsTilesEqualComparer();
	public static readonly IEqualityComparer<GraphicsTile> Flipped = new GraphicsTilesMatchComparer();

	private class GraphicsTilesEqualComparer : IEqualityComparer<GraphicsTile> {
		public bool Equals(GraphicsTile? x, GraphicsTile? y) {
			return x?.Equals(y) ?? false;
		}

		public int GetHashCode([DisallowNull] GraphicsTile tile) {
			return tile.GetHashCode();
		}
	}

	private class GraphicsTilesMatchComparer : IEqualityComparer<GraphicsTile> {
		public bool Equals(GraphicsTile? x, GraphicsTile? y) {
			if (x is not null && y is not null) {
				return x.Matches(in y._indexedTile) is not MatchResult.None;

			} else {
				return false;
			}
		}

		public int GetHashCode([DisallowNull] GraphicsTile tile) {
			return tile._indexedTile.GetInvariantHashCode();
		}
	}
}
