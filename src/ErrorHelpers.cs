using System.Diagnostics.CodeAnalysis;

namespace Sheeter;

[StackTraceHidden]
internal static class ErrorHelpers {
	internal const string BadImageX = "X coordinate must be a non-negative number less than the image's width.";
	internal const string BadImageY = "Y coordinate must be a non-negative number less than the image's width.";
	internal const string BadTileRow = "Selected row must be a non-negative number less than 8.";

	internal const string TileOutOfRange = "Selected tile is out of bounds.";
	internal const string PixelOutOfRange = "Selected pixel is out of bounds.";

	internal const string InvalidTileFlip = "Invalid flip type.";

	[DoesNotReturn]
	internal static void ThrowBadArgument(string message) {
		throw new ArgumentException(message);
	}

	[DoesNotReturn]
	internal static void ThrowOutOfRangeArgument(string name, string message) {
		throw new ArgumentOutOfRangeException(name, message);
	}

	[DoesNotReturn]
	internal static void ThrowOutOfRangeArgument(string message) {
		throw new ArgumentOutOfRangeException(null, message);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidOperation() {
		throw new InvalidOperationException();
	}

	[DoesNotReturn]
	internal static void ThrowBecauseDestinationIsShort() {
		throw new ArgumentException("Destination is too short.");
	}

	internal static void ThrowIfDestinationTooShort(int sourceCount, int destinationCount) {
		if (sourceCount > destinationCount) {
			ThrowBecauseDestinationIsShort();
		}
	}

	internal static void ThrowIfBadViewPort(int sourceWidth, int sourceHeight, int x, int y, int width, int height) {
		if (width < 1 || height < 1 ||
			(uint) x > sourceWidth || (x + width) > sourceWidth ||
			(uint) y > sourceHeight || (y + height) > sourceHeight) {
			ThrowOutOfRangeArgument("Viewport escapes bounds of source.");
		}
	}
}
