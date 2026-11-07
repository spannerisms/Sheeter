using System.Buffers.Binary;

namespace SheeterTests;

/// <summary>
/// Compares the high-performance library methods to
/// slow and manual, but easily-validated versions.
/// </summary>
[TestClass]
public class ManualConversionTests {
	const int ManualTests = 1000;

	private static (ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong) GetRows(in IndexedTile tile) {
		return
		(
			BinaryPrimitives.ReadUInt64LittleEndian(tile.AsSpan().Slice(0 * 8, 8)),
			BinaryPrimitives.ReadUInt64LittleEndian(tile.AsSpan().Slice(1 * 8, 8)),
			BinaryPrimitives.ReadUInt64LittleEndian(tile.AsSpan().Slice(2 * 8, 8)),
			BinaryPrimitives.ReadUInt64LittleEndian(tile.AsSpan().Slice(3 * 8, 8)),
			BinaryPrimitives.ReadUInt64LittleEndian(tile.AsSpan().Slice(4 * 8, 8)),
			BinaryPrimitives.ReadUInt64LittleEndian(tile.AsSpan().Slice(5 * 8, 8)),
			BinaryPrimitives.ReadUInt64LittleEndian(tile.AsSpan().Slice(6 * 8, 8)),
			BinaryPrimitives.ReadUInt64LittleEndian(tile.AsSpan().Slice(7 * 8, 8))
		);
	}

	private static byte GetRowPlanes(int plane, ulong row) {
		int baseShift = 2 + plane;

		byte ret = 0;
		ret |= (byte) ((row << (7 - plane)) & 0x80);
		ret |= (byte) ((row >> ((9 * 0) + baseShift)) & 0x40);
		ret |= (byte) ((row >> ((9 * 1) + baseShift)) & 0x20);
		ret |= (byte) ((row >> ((9 * 2) + baseShift)) & 0x10);
		ret |= (byte) ((row >> ((9 * 3) + baseShift)) & 0x08);
		ret |= (byte) ((row >> ((9 * 4) + baseShift)) & 0x04);
		ret |= (byte) ((row >> ((9 * 5) + baseShift)) & 0x02);
		ret |= (byte) ((row >> ((9 * 6) + baseShift)) & 0x01);

		return ret;
	}

	[TestMethod]
	public void TestAgainstManualSnesMode7() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, ManualTests);

		SnesMode7Tile test = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertTo(ref test);

			// they're identical lol
			Assert.AreSequenceEqual<byte>(tile, test);
		}
	}

	[TestMethod]
	public void TestAgainstManualNes2bpp() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, ManualTests);

		Nes2bppTile test = new();
		Nes2bppTile conv = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertTo(ref test);

			var (row0, row1, row2, row3, row4, row5, row6, row7) = GetRows(in tile);

			conv[0x00] = GetRowPlanes(0, row0);
			conv[0x01] = GetRowPlanes(0, row1);
			conv[0x02] = GetRowPlanes(0, row2);
			conv[0x03] = GetRowPlanes(0, row3);
			conv[0x04] = GetRowPlanes(0, row4);
			conv[0x05] = GetRowPlanes(0, row5);
			conv[0x06] = GetRowPlanes(0, row6);
			conv[0x07] = GetRowPlanes(0, row7);

			conv[0x08] = GetRowPlanes(1, row0);
			conv[0x09] = GetRowPlanes(1, row1);
			conv[0x0A] = GetRowPlanes(1, row2);
			conv[0x0B] = GetRowPlanes(1, row3);
			conv[0x0C] = GetRowPlanes(1, row4);
			conv[0x0D] = GetRowPlanes(1, row5);
			conv[0x0E] = GetRowPlanes(1, row6);
			conv[0x0F] = GetRowPlanes(1, row7);

			Assert.AreSequenceEqual<byte>(conv, test);
		}
	}

	[TestMethod]
	public void TestAgainstManualSnes2bpp() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, ManualTests);

		Snes2bppTile test = new();
		Snes2bppTile conv = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertTo(ref test);

			var (row0, row1, row2, row3, row4, row5, row6, row7) = GetRows(in tile);

			conv[0x00] = GetRowPlanes(0, row0);
			conv[0x01] = GetRowPlanes(1, row0);
			conv[0x02] = GetRowPlanes(0, row1);
			conv[0x03] = GetRowPlanes(1, row1);
			conv[0x04] = GetRowPlanes(0, row2);
			conv[0x05] = GetRowPlanes(1, row2);
			conv[0x06] = GetRowPlanes(0, row3);
			conv[0x07] = GetRowPlanes(1, row3);

			conv[0x08] = GetRowPlanes(0, row4);
			conv[0x09] = GetRowPlanes(1, row4);
			conv[0x0A] = GetRowPlanes(0, row5);
			conv[0x0B] = GetRowPlanes(1, row5);
			conv[0x0C] = GetRowPlanes(0, row6);
			conv[0x0D] = GetRowPlanes(1, row6);
			conv[0x0E] = GetRowPlanes(0, row7);
			conv[0x0F] = GetRowPlanes(1, row7);

			Assert.AreSequenceEqual<byte>(conv, test);
		}
	}

	[TestMethod]
	public void TestAgainstManualSnes3bpp() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, ManualTests);

		Snes3bppTile test = new();
		Snes3bppTile conv = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertTo(ref test);

			var (row0, row1, row2, row3, row4, row5, row6, row7) = GetRows(in tile);

			conv[0x00] = GetRowPlanes(0, row0);
			conv[0x01] = GetRowPlanes(1, row0);
			conv[0x02] = GetRowPlanes(0, row1);
			conv[0x03] = GetRowPlanes(1, row1);
			conv[0x04] = GetRowPlanes(0, row2);
			conv[0x05] = GetRowPlanes(1, row2);
			conv[0x06] = GetRowPlanes(0, row3);
			conv[0x07] = GetRowPlanes(1, row3);

			conv[0x08] = GetRowPlanes(0, row4);
			conv[0x09] = GetRowPlanes(1, row4);
			conv[0x0A] = GetRowPlanes(0, row5);
			conv[0x0B] = GetRowPlanes(1, row5);
			conv[0x0C] = GetRowPlanes(0, row6);
			conv[0x0D] = GetRowPlanes(1, row6);
			conv[0x0E] = GetRowPlanes(0, row7);
			conv[0x0F] = GetRowPlanes(1, row7);

			conv[0x10] = GetRowPlanes(2, row0);
			conv[0x11] = GetRowPlanes(2, row1);
			conv[0x12] = GetRowPlanes(2, row2);
			conv[0x13] = GetRowPlanes(2, row3);
			conv[0x14] = GetRowPlanes(2, row4);
			conv[0x15] = GetRowPlanes(2, row5);
			conv[0x16] = GetRowPlanes(2, row6);
			conv[0x17] = GetRowPlanes(2, row7);

			Assert.AreSequenceEqual<byte>(conv, test);
		}
	}

	[TestMethod]
	public void TestAgainstManualSnes4bpp() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, ManualTests);

		Snes4bppTile test = new();
		Snes4bppTile conv = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertTo(ref test);

			var (row0, row1, row2, row3, row4, row5, row6, row7) = GetRows(in tile);

			conv[0x00] = GetRowPlanes(0, row0);
			conv[0x01] = GetRowPlanes(1, row0);
			conv[0x02] = GetRowPlanes(0, row1);
			conv[0x03] = GetRowPlanes(1, row1);
			conv[0x04] = GetRowPlanes(0, row2);
			conv[0x05] = GetRowPlanes(1, row2);
			conv[0x06] = GetRowPlanes(0, row3);
			conv[0x07] = GetRowPlanes(1, row3);

			conv[0x08] = GetRowPlanes(0, row4);
			conv[0x09] = GetRowPlanes(1, row4);
			conv[0x0A] = GetRowPlanes(0, row5);
			conv[0x0B] = GetRowPlanes(1, row5);
			conv[0x0C] = GetRowPlanes(0, row6);
			conv[0x0D] = GetRowPlanes(1, row6);
			conv[0x0E] = GetRowPlanes(0, row7);
			conv[0x0F] = GetRowPlanes(1, row7);

			conv[0x10] = GetRowPlanes(2, row0);
			conv[0x11] = GetRowPlanes(3, row0);
			conv[0x12] = GetRowPlanes(2, row1);
			conv[0x13] = GetRowPlanes(3, row1);
			conv[0x14] = GetRowPlanes(2, row2);
			conv[0x15] = GetRowPlanes(3, row2);
			conv[0x16] = GetRowPlanes(2, row3);
			conv[0x17] = GetRowPlanes(3, row3);

			conv[0x18] = GetRowPlanes(2, row4);
			conv[0x19] = GetRowPlanes(3, row4);
			conv[0x1A] = GetRowPlanes(2, row5);
			conv[0x1B] = GetRowPlanes(3, row5);
			conv[0x1C] = GetRowPlanes(2, row6);
			conv[0x1D] = GetRowPlanes(3, row6);
			conv[0x1E] = GetRowPlanes(2, row7);
			conv[0x1F] = GetRowPlanes(3, row7);

			Assert.AreSequenceEqual<byte>(conv, test);
		}
	}

	[TestMethod]
	public void TestAgainstManualSnes8bpp() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, ManualTests);

		Snes8bppTile test = new();
		Snes8bppTile conv = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertTo(ref test);

			var (row0, row1, row2, row3, row4, row5, row6, row7) = GetRows(in tile);

			conv[0x00] = GetRowPlanes(0, row0);
			conv[0x01] = GetRowPlanes(1, row0);
			conv[0x02] = GetRowPlanes(0, row1);
			conv[0x03] = GetRowPlanes(1, row1);
			conv[0x04] = GetRowPlanes(0, row2);
			conv[0x05] = GetRowPlanes(1, row2);
			conv[0x06] = GetRowPlanes(0, row3);
			conv[0x07] = GetRowPlanes(1, row3);

			conv[0x08] = GetRowPlanes(0, row4);
			conv[0x09] = GetRowPlanes(1, row4);
			conv[0x0A] = GetRowPlanes(0, row5);
			conv[0x0B] = GetRowPlanes(1, row5);
			conv[0x0C] = GetRowPlanes(0, row6);
			conv[0x0D] = GetRowPlanes(1, row6);
			conv[0x0E] = GetRowPlanes(0, row7);
			conv[0x0F] = GetRowPlanes(1, row7);

			conv[0x10] = GetRowPlanes(2, row0);
			conv[0x11] = GetRowPlanes(3, row0);
			conv[0x12] = GetRowPlanes(2, row1);
			conv[0x13] = GetRowPlanes(3, row1);
			conv[0x14] = GetRowPlanes(2, row2);
			conv[0x15] = GetRowPlanes(3, row2);
			conv[0x16] = GetRowPlanes(2, row3);
			conv[0x17] = GetRowPlanes(3, row3);

			conv[0x18] = GetRowPlanes(2, row4);
			conv[0x19] = GetRowPlanes(3, row4);
			conv[0x1A] = GetRowPlanes(2, row5);
			conv[0x1B] = GetRowPlanes(3, row5);
			conv[0x1C] = GetRowPlanes(2, row6);
			conv[0x1D] = GetRowPlanes(3, row6);
			conv[0x1E] = GetRowPlanes(2, row7);
			conv[0x1F] = GetRowPlanes(3, row7);

			conv[0x20] = GetRowPlanes(4, row0);
			conv[0x21] = GetRowPlanes(5, row0);
			conv[0x22] = GetRowPlanes(4, row1);
			conv[0x23] = GetRowPlanes(5, row1);
			conv[0x24] = GetRowPlanes(4, row2);
			conv[0x25] = GetRowPlanes(5, row2);
			conv[0x26] = GetRowPlanes(4, row3);
			conv[0x27] = GetRowPlanes(5, row3);

			conv[0x28] = GetRowPlanes(4, row4);
			conv[0x29] = GetRowPlanes(5, row4);
			conv[0x2A] = GetRowPlanes(4, row5);
			conv[0x2B] = GetRowPlanes(5, row5);
			conv[0x2C] = GetRowPlanes(4, row6);
			conv[0x2D] = GetRowPlanes(5, row6);
			conv[0x2E] = GetRowPlanes(4, row7);
			conv[0x2F] = GetRowPlanes(5, row7);

			conv[0x30] = GetRowPlanes(6, row0);
			conv[0x31] = GetRowPlanes(7, row0);
			conv[0x32] = GetRowPlanes(6, row1);
			conv[0x33] = GetRowPlanes(7, row1);
			conv[0x34] = GetRowPlanes(6, row2);
			conv[0x35] = GetRowPlanes(7, row2);
			conv[0x36] = GetRowPlanes(6, row3);
			conv[0x37] = GetRowPlanes(7, row3);

			conv[0x38] = GetRowPlanes(6, row4);
			conv[0x39] = GetRowPlanes(7, row4);
			conv[0x3A] = GetRowPlanes(6, row5);
			conv[0x3B] = GetRowPlanes(7, row5);
			conv[0x3C] = GetRowPlanes(6, row6);
			conv[0x3D] = GetRowPlanes(7, row6);
			conv[0x3E] = GetRowPlanes(6, row7);
			conv[0x3F] = GetRowPlanes(7, row7);

			Assert.AreSequenceEqual<byte>(conv, test);
		}
	}

	[TestMethod]
	public void TestAgainstManualPacked1bpp() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, ManualTests);

		Packed1bppTile test = new();
		Packed1bppTile conv = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertTo(ref test);

			for (int i = 0, j = 0; j < Packed1bppTile.Length; i += 8 / Packed1bppTile.BitsPerPixel, j++) {
				conv[j] = (byte) (
					((tile[i + 0] & 0x1) << 0) |
					((tile[i + 1] & 0x1) << 1) |
					((tile[i + 2] & 0x1) << 2) |
					((tile[i + 3] & 0x1) << 3) |
					((tile[i + 4] & 0x1) << 4) |
					((tile[i + 5] & 0x1) << 5) |
					((tile[i + 6] & 0x1) << 6) |
					((tile[i + 7] & 0x1) << 7)
				);
			}

			Assert.AreSequenceEqual<byte>(conv, test);
		}
	}

	[TestMethod]
	public void TestAgainstManualPacked2bpp() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, ManualTests);

		Packed2bppTile test = new();
		Packed2bppTile conv = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertTo(ref test);
			
			for (int i = 0, j = 0; j < Packed2bppTile.Length; i += 8 / Packed2bppTile.BitsPerPixel, j++) {
				conv[j] = (byte) (
					((tile[i + 0] & 0x3) << 0) |
					((tile[i + 1] & 0x3) << 2) |
					((tile[i + 2] & 0x3) << 4) |
					((tile[i + 3] & 0x3) << 6)
				);
			}

			Assert.AreSequenceEqual<byte>(conv, test);
		}
	}

	[TestMethod]
	public void TestAgainstManualPacked4bpp() {
		var tiles = IndexedTile.CreateSpanFromBytes(Common.TestData, 0, ManualTests);

		Packed4bppTile test = new();
		Packed4bppTile conv = new();

		foreach (ref readonly var tile in tiles) {
			tile.ConvertTo(ref test);

			for (int i = 0, j = 0; j < Packed4bppTile.Length; i += 8 / Packed4bppTile.BitsPerPixel, j++) {
				conv[j] = (byte) (
					((tile[i + 0] & 0xF) << 0) |
					((tile[i + 1] & 0xF) << 4)
				);
			}

			Assert.AreSequenceEqual<byte>(conv, test);
		}
	}
}
