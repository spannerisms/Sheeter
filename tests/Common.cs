global using System.Runtime.CompilerServices;

global using Sheeter;
global using Sheeter.Nes;
global using Sheeter.Snes;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace SheeterTests;

static class Common {
	internal static ReadOnlySpan<byte> TestData => _testData;

	internal static IndexedBitmap GetTestIndexedImage(int w, int h) {
		IndexedBitmap ret = new(w, h);
		new Random().NextBytes(ret.GetPixelSpan());
		return ret;
	}

	internal static ReadOnlySpan<T> GetTestData<T>(int count) where T : unmanaged {
		return System.Runtime.InteropServices.MemoryMarshal.Cast<byte, T>(TestData.Slice(0, Unsafe.SizeOf<T>() * count));
	}

	private static readonly byte[] _testData = GC.AllocateUninitializedArray<byte>(0x1000000, true);

	static Common() {
		new Random().NextBytes(_testData);
	}
}
