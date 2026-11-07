namespace Sheeter;

/// <summary>
/// Defines methods and properties for handling graphics tile data
/// that can be converted to and from indexed graphics.
/// </summary>
public interface IConvertibleTile<TSelf> : ITileData<TSelf> where TSelf : unmanaged, IConvertibleTile<TSelf> {
	/// <summary>
	/// Internal: Used to categorize types by conversion method.
	/// </summary>
	internal static abstract ConversionType ConversionType { get; }

	/// <summary>
	/// Converts the data in this tile to indexed data in <paramref name="destination"/>.
	/// </summary>
	/// <param name="destination">The destination of the indexed data to write to.</param>
	public void ToIndexedTile(scoped ref IndexedTile destination);

	/// <inheritdoc cref="ToIndexedTile(ref IndexedTile)"/>
	public void ToIndexedTile(GraphicsTile destination);

	/// <inheritdoc cref="ToIndexedTile(ref IndexedTile)"/>
	public void ToIndexedTile(scoped IndexedTileViewport destination);
}
