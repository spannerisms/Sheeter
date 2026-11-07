# Sheeter

Sheeter is a high-performance .NET library for converting retro game graphics formats to and from indexed pixel data. Sheeter is available from nuget.org as the package `Spannerisms.Sheeter`.

## Basic usage

The Sheeter library contains various structure types to represent tile data, for example `Snes4bppTile`. Before using this library, make sure you are familiar with [Span types](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/) and [ref variables](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/declarations#reference-variables)

> [!IMPORTANT]
> The tile structs are large, unmanaged types. It is generally best to only use them in arrays or spans. If tiles need to be handled individually, prefer doing so with the [ref keyword](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/ref). Most of this library is built on passing tiles by reference; however, you should keep these concerns in mind when writing your own routines.

### Convert an image

1. Create a `SheeterBitmap` instance using `new SheeterBitmap(file)` to load an image from a file. *Note: This type is disposable.*
1. Create a `PaletteSet` and populate it to map colors to indices.
1. Use `SheeterImage.CreateIndexedTiles` to create a collection of indexed-color 8x8 tiles from the image.
1. Use `GraphicsTile.ConvertAllTiles<TFormat>` to convert to the desired tile format.

*Example:*

```csharp
using Sheeter;
using Sheeter.Snes;

private static readonly PaletteSet CommonPalette = ...;

public byte[] CreateSpriteSheet(string path) {
    using SheeterBitmap image = new(path);

    TiledBitmapViewport sheetView = new(image, x: 0, y: 0, tilesWidth: 16, tilesHeight: 4);

    GraphicsTile[] indexedTiles = sheetView.CreateGraphicsTiles(CommonPalette);

    Snes3bppTile[] convertedTiles = indexedTiles.ConvertAllTo<Snes3bppTile>();

    return convertedTiles.AsBytes();
}

```

### Convert ROM data

1. Use one of the `FromBytes` extension methods available to the tile format types.
1. Pass the span to `GraphicsTile.ConvertAllTilesToIndexed`.

## Example usage

### Get indexed tiles from ROM data

*Example:*

```csharp
using Sheeter;
using Sheeter.Snes;

public static GraphicsTile[] Read4bpp(byte[] rom, int offset, int tileCount) {
    ReadOnlySpan<Snes4bppTile> tiles = Snes4bppTile.CreateSpanFromBytes(rom, offset, tileCount);

    return GraphicsTile.ConvertAllTiles<Snes4bppTile>(tiles);
}
```

## Notes

- `Packed1bppTile` implicitly works for NES, SNES, and several other interleaved formats.
- Gameboy and Gameboy Color use `Snes2bppTile`.
- PC Engine uses `Snes4bppTile`.

## References

- https://mrclick.zophar.net/TilEd/download/consolegfx.txt
- https://www.nesdev.org/wiki/PPU_palettes
