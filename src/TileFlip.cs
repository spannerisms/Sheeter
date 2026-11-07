namespace Sheeter;

/// <summary>
/// Represents a reflection.
/// </summary>
[Flags]
public enum TileFlip {
	/// <summary>
	/// No flip.
	/// </summary>
	None = 0x0000,

	/// <summary>
	/// Horizontal flip.
	/// </summary>
	HFlip = 0x4000,

	/// <summary>
	/// Vertical flip.
	/// </summary>
	VFlip = 0x8000,

	/// <summary>
	/// Vertical and horizontal flip.
	/// </summary>
	VHFlip = HFlip|VFlip,
}
