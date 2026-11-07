namespace SheeterTests;

[TestClass]
public class MiscTests {
	[TestMethod]
	public void TestInvariantHash() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, 3000);

		IndexedTile test = new();

		foreach (ref readonly var tile in tiles) {
			tile.CopyTo(ref test);
			int hash1 = GetInvariantHashCode(test);

			tile.CopyToHFlipped(ref test);
			int hash2 = GetInvariantHashCode(test);

			tile.CopyToVFlipped(ref test);
			int hash3 = GetInvariantHashCode(test);

			tile.CopyToVHFlipped(ref test);
			int hash4 = GetInvariantHashCode(test);

			Assert.AreEqual(hash1, hash2);
			Assert.AreEqual(hash1, hash3);
			Assert.AreEqual(hash1, hash4);
		}

		[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetInvariantHashCode")]
		extern static int GetInvariantHashCode(in IndexedTile tile);
	}
}
