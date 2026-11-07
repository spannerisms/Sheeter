namespace Sheeter;

/// <summary>
/// Extension methods related to the Sheeter library.
/// </summary>
public static class SheeterExtensions {
	extension(MatchResult match) {
		/// <summary>
		/// Converts a match result to a corresponding flip.
		/// </summary>
		public TileFlip ToFlip() {
			if (match >= MatchResult.VFlipped) {
				return (TileFlip) match;

			} else {
				return TileFlip.None;
			}
		}
	}
}
