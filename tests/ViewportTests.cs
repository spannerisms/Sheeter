namespace SheeterTests;

[TestClass]
public class ViewportTests {
	private static void TestViewportMatches<T>() where T : unmanaged, IConvertibleTile<T> {
		var tiles = Common.GetTestData<T>(10000);

		using IndexedBitmap img = Common.GetTestIndexedImage(8, 8);

		IndexedTileViewport testA = img.GetTile(0, 0);
		IndexedTile testB = new();

		foreach (ref readonly var tile in tiles) {
			tile.ToIndexedTile(ref testB);
			tile.ToIndexedTile(testA);

			Assert.IsTrue(testB.Equals(testA));
		}
	}

	[TestMethod] public void TestViewPortTileNes2() => TestViewportMatches<Nes2bppTile>();
	[TestMethod] public void TestViewPortTileSnes2() => TestViewportMatches<Snes2bppTile>();
	[TestMethod] public void TestViewPortTileSnes3() => TestViewportMatches<Snes3bppTile>();
	[TestMethod] public void TestViewPortTileSnes4() => TestViewportMatches<Snes4bppTile>();
	[TestMethod] public void TestViewPortTileSnes8() => TestViewportMatches<Snes8bppTile>();
	[TestMethod] public void TestViewPortTileSnesMode7() => TestViewportMatches<SnesMode7Tile>();
	[TestMethod] public void TestViewPortTilePacked1() => TestViewportMatches<Packed1bppTile>();
	[TestMethod] public void TestViewPortTilePacked2() => TestViewportMatches<Packed2bppTile>();
	[TestMethod] public void TestViewPortTilePacked4() => TestViewportMatches<Packed4bppTile>();

}
