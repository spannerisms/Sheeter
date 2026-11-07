namespace SheeterTests;

/// <summary>
/// Validates converting from one format to another produces the same results
/// as using an indexed tile middle man.
/// </summary>
[TestClass]
public class ReformatTests {
	[TestMethod]
	public void TestNesToSnes() {
		var tiles = Nes2bppTile.CreateSpanFromBytes(Common.TestData, 0, 20000);

		Snes2bppTile testA = new();
		Snes2bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertToSnes2bpp(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestNesToSnesScalar() {
		var tiles = Nes2bppTile.CreateSpanFromBytes(Common.TestData, 0, 20000);

		Snes2bppTile testA = new();
		Snes2bppTile testB = new();

		foreach (ref readonly var tile in tiles) {
			ConvertScalar(tile, in testA);
			tile.ConvertToSnes2bpp(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}

		[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ConvertToSnesScalar")]
		extern static void ConvertScalar(in Nes2bppTile tile, in Snes2bppTile dest);
	}

	[TestMethod]
	public void TestSnesToNes() {
		var tiles = Snes2bppTile.CreateSpanFromBytes(Common.TestData, 0, 20000);

		Nes2bppTile testA = new();
		Nes2bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertToNes2bpp(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestSnesToNesScalar() {
		var tiles = Snes2bppTile.CreateSpanFromBytes(Common.TestData, 0, 20000);

		Nes2bppTile testA = new();
		Nes2bppTile testB = new();

		foreach (ref readonly var tile in tiles) {
			ConvertScalar(tile, in testA);
			tile.ConvertToNes2bpp(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}

		[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ConvertToNesScalar")]
		extern static void ConvertScalar(in Snes2bppTile tile, in Nes2bppTile dest);
	}
}
