namespace Sheeter;

/// <summary>
/// Defines methods for retrieving colors from a palette.
/// </summary>
public interface IPalette : IEnumerable<SheetColor> {
	/// <summary>
	/// Searches this palette for <paramref name="color"/> and returns the first index of it.
	/// If not found, this method will return 0.
	/// </summary>
	/// <remarks>
	/// A value of 0 is used as the default, because this is always the index
	/// of transparency in an indexed tile.
	/// Types implementing this interface should take care to not return -1.
	/// </remarks>
	/// <param name="color">The color to search for.</param>
	/// <returns>The first index of <paramref name="color"/> if it is found; otherwise, 0.</returns>
	public byte IndexOf(SheetColor color);

	/// <summary>
	/// Retrieves the color at the given index.
	/// </summary>
	/// <param name="index">The index to select.</param>
	/// <returns>A color.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The index is outside of the palette.</exception>
	public SheetColor this[int index] { get; set; }
}
