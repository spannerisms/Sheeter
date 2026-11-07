namespace SheeterTests;

/// <summary>
/// Validates that conversion round trips result in the original data.
/// </summary>
[TestClass]
public class RoundTripTests {
	public static void TestRoundTrip<T>() where T : unmanaged, IConvertibleTile<T> {
		var tiles = Common.GetTestData<T>(10000);

		IndexedTile temp = new();
		T tmp2 = new();

		foreach (ref readonly var tile in tiles) {
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref tmp2);

			Assert.AreSequenceEqual(tile.AsSpan(), tmp2.AsSpan());
		}
	}

	[TestMethod] public void TestRoundTripNes2() => TestRoundTrip<Nes2bppTile>();
	[TestMethod] public void TestRoundTripSnes2() => TestRoundTrip<Snes2bppTile>();
	[TestMethod] public void TestRoundTripSnes3() => TestRoundTrip<Snes3bppTile>();
	[TestMethod] public void TestRoundTripSnes4() => TestRoundTrip<Snes4bppTile>();
	[TestMethod] public void TestRoundTripSnes8() => TestRoundTrip<Snes8bppTile>();
	[TestMethod] public void TestRoundTripSnesMode7() => TestRoundTrip<SnesMode7Tile>();
	[TestMethod] public void TestRoundTripPacked1() => TestRoundTrip<Packed1bppTile>();
	[TestMethod] public void TestRoundTripPacked2() => TestRoundTrip<Packed2bppTile>();
	[TestMethod] public void TestRoundTripPacked4() => TestRoundTrip<Packed4bppTile>();

	[TestMethod]
	public void TestNesToSnesRoundTrip() {
		var tiles = Nes2bppTile.CreateSpanFromBytes(Common.TestData, 0, 20000);

		Nes2bppTile test = new();
		Snes2bppTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertToSnes2bpp(ref temp);
			temp.ConvertToNes2bpp(ref test);

			Assert.AreSequenceEqual<byte>(tile, test);
		}
	}

	[TestMethod]
	public void TestSnesToNesRoundTrip() {
		var tiles = Snes2bppTile.CreateSpanFromBytes(Common.TestData, 0, 20000);

		Snes2bppTile test = new();
		Nes2bppTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertToNes2bpp(ref temp);
			temp.ConvertToSnes2bpp(ref test);

			Assert.AreSequenceEqual<byte>(tile, test);
		}
	}
}
