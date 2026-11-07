namespace Sheeter;

/// <summary>
/// Represents a match result when comparing two indexed tiles.
/// </summary>
public enum MatchResult {
	/// <summary>
	/// The tiles do not match under any reflection transformation.
	/// </summary>
	None = 0,

	/// <summary>
	/// The tiles are identical.
	/// </summary>
	Same,

	/// <summary>
	/// The tiles are identical when one is flipped horizontally.
	/// </summary>
	HFlipped = TileFlip.HFlip,

	/// <summary>
	/// The tiles are identical when one is flipped vertically.
	/// </summary>
	VFlipped = TileFlip.VFlip,

	/// <summary>
	/// The tiles are identical when one is flipped both vertically and horizontally.
	/// </summary>
	VHFlipped = TileFlip.VHFlip,
}
