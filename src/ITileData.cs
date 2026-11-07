using System.Diagnostics.CodeAnalysis;

namespace Sheeter;

/// <summary>
/// Defines methods and properties for handling graphics tile data in various formats.
/// </summary>
public interface ITileData<TSelf> where TSelf : unmanaged, ITileData<TSelf> {
	/// <summary>
	/// The number of bits used for each pixel in this format.
	/// </summary>
	public static abstract int BitsPerPixel { get; }

	/// <summary>
	/// The length in bytes of a tile in this format.
	/// </summary>
	public static virtual int Length => Unsafe.SizeOf<TSelf>();

	/// <summary>
	/// The number of colors each pixel can represent in this format.
	/// </summary>
	public static virtual int ColorsPerPixel => 1 << TSelf.BitsPerPixel;

	/// <summary>
	/// Internal: Gets a reference to this tile as a managed byte pointer.
	/// </summary>
	internal abstract ref byte Reference { get; }

	/// <summary>
	/// Creates a read-only span over the data in this tile.
	/// </summary>
	/// <returns>A read-only span.</returns>
	// Everything that implements this interface is an inline array.
	// But because we can't tell the compiler that, we need a way to easily get a span.
	// [UnscopedRef] so that implementing types can simply return `this`.
	[UnscopedRef]
	public ReadOnlySpan<byte> AsSpan();

	/// <summary>
	/// Copies the data in this tile to <paramref name="destination"/>.
	/// </summary>
	/// <param name="destination">The destination to copy to.</param>
	public void CopyTo(scoped ref TSelf destination);
}
