using Sheeter.Nes;
using Sheeter.Snes;

namespace Sheeter;

internal static class Asserts {
	[Conditional("DEBUG")]
	public static void IsSnesPlanar(Type type) {
		Debug.Assert(
			type == typeof(Snes2bppTile) ||
			type == typeof(Snes4bppTile) ||
			type == typeof(Snes8bppTile)
		);
	}

	/// <summary>
	/// Refers to anything that can be handled by <see cref="ConversionHelpers.IndexedToPlanar(ref readonly byte, ref byte, int, int, ConversionType)"/>
	/// </summary>
	[Conditional("DEBUG")]
	public static void IsGenericPlanar(Type type) {
		Debug.Assert(
			type == typeof(Nes2bppTile) ||
			type == typeof(Snes2bppTile) ||
			type == typeof(Snes4bppTile) ||
			type == typeof(Snes8bppTile)
		);
	}

	[Conditional("DEBUG")]
	public static void IsSnes3bpp(Type type) {
		Debug.Assert(
			type == typeof(Snes3bppTile)
		);
	}

	[Conditional("DEBUG")]
	public static void IsNesPlanar(Type type) {
		Debug.Assert(
			type == typeof(Nes2bppTile)
		);
	}

	[Conditional("DEBUG")]
	public static void IsPackedFormat(Type type) {
		Debug.Assert(
			type == typeof(Packed1bppTile) ||
			type == typeof(Packed2bppTile) ||
			type == typeof(Packed4bppTile)
		);
	}
}
