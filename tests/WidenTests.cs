using System.Runtime.Intrinsics;

namespace SheeterTests;

/// <summary>
/// Validates widening produces the same results as a longer conversion
/// </summary>
[TestClass]
public class WidenTests {
	const int WidenTestRuns = 20000;

	[TestMethod]
	public void TestWideningSnes2to3() {
		var tiles = Snes2bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes3bppTile testA = new();
		Snes3bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.Widen(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestWideningSnes2to4() {
		var tiles = Snes2bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes4bppTile testA = new();
		Snes4bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.Widen(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestWideningSnes2to8() {
		var tiles = Snes2bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes8bppTile testA = new();
		Snes8bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.Widen(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestWideningSnes3to4() {
		var tiles = Snes3bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes4bppTile testA = new();
		Snes4bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.Widen(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestWideningSnes3to4High() {
		var tiles = Snes3bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes4bppTile testA = new();
		Snes4bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.WidenHigh(ref testA);
			tile.ToIndexedTile(ref temp);

			if (Vector512.IsHardwareAccelerated) {
				Vector512<byte> ind = Vector512.Create<byte>(temp);
				Vector512<byte> mask = Vector512.GreaterThan(ind, Vector512<byte>.Zero);
				mask &= Vector512.Create<byte>(8);
				ind |= mask;
				ind.CopyTo(temp);

			} else if (Vector256.IsHardwareAccelerated) {
				DoOne(temp[..32]);
				DoOne(temp[32..]);

				static void DoOne(Span<byte> slice) {
					Vector256<byte> ind = Vector256.Create(slice);
					Vector256<byte> mask = Vector256.GreaterThan(ind, Vector256<byte>.Zero);
					mask &= Vector256.Create<byte>(8);
					ind |= mask;
					ind.CopyTo(slice);
				}

			} else {
				foreach (ref byte b in temp) {
					if (b != 0) {
						b |= 8;
					}
				}
			}


			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestWideningSnes3to8() {
		var tiles = Snes3bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes8bppTile testA = new();
		Snes8bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.Widen(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestWideningSnes4to8() {
		var tiles = Snes4bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes8bppTile testA = new();
		Snes8bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.Widen(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestWideningPacked1to2() {
		var tiles = Packed1bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Packed2bppTile testA = new();
		Packed2bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.Widen(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestWideningPacked1to4() {
		var tiles = Packed1bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Packed4bppTile testA = new();
		Packed4bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.Widen(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	[TestMethod]
	public void TestWideningPacked2to4() {
		var tiles = Packed2bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Packed4bppTile testA = new();
		Packed4bppTile testB = new();
		IndexedTile temp = new();

		foreach (ref readonly var tile in tiles) {
			tile.Widen(ref testA);
			tile.ToIndexedTile(ref temp);
			temp.ConvertTo(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}
	}

	// validate that all widening methods behave the same
	[TestMethod]
	public void TestWideningSnes3to4Fallback() {
		var tiles = Snes3bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes4bppTile testA = new();
		Snes4bppTile testB = new();

		foreach (ref readonly var tile in tiles) {
			WidenTest(in tile, ref testA);
			tile.Widen(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}

		[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "WidenFallback")]
		extern static void WidenTest(in Snes3bppTile tile, ref Snes4bppTile dest);
	}

	[TestMethod]
	public void TestWideningSnes3to4v128() {
		var tiles = Snes3bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes4bppTile testA = new();
		Snes4bppTile testB = new();

		foreach (ref readonly var tile in tiles) {
			WidenTest(in tile, ref testA);
			tile.Widen(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}

		[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "WidenVector128")]
		extern static void WidenTest(in Snes3bppTile tile, ref Snes4bppTile dest);
	}

	[TestMethod]
	public void TestWideningSnes3to4v256() {
		var tiles = Snes3bppTile.CreateSpanFromBytes(Common.TestData, 0, WidenTestRuns);

		Snes4bppTile testA = new();
		Snes4bppTile testB = new();

		foreach (ref readonly var tile in tiles) {
			WidenTest(in tile, ref testA);
			tile.Widen(ref testB);

			Assert.AreSequenceEqual<byte>(testA, testB);
		}

		[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "WidenVector256")]
		extern static void WidenTest(in Snes3bppTile tile, ref Snes4bppTile dest);
	}
}
