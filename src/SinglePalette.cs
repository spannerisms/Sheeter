using System.Collections;

namespace Sheeter;

/// <summary>
/// Represents a single palette with a fixed number of colors.
/// </summary>
public abstract class SinglePalette : IPalette {
	private protected abstract int Length { get; }
	private protected abstract Span<SheetColor> Colors { get; }

	/// <inheritdoc/>
	public SheetColor this[int index] {
		get => Colors[index];
		set => Colors[index] = value;
	}

	/// <summary>
	/// Creates a new palette instance.
	/// </summary>
	private protected SinglePalette() { }

	/// <summary>
	/// Clones this instance to a new palette.
	/// </summary>
	/// <returns>A new palette with the same colors.</returns>
	public abstract SinglePalette Clone();

	/// <inheritdoc/>
	public byte IndexOf(SheetColor color) {
		int ret = Colors.IndexOf(color);
		return (byte) (ret == -1 ? 0 : ret);
	}

	/// <inheritdoc/>
	public IEnumerator<SheetColor> GetEnumerator() => new SinglePaletteEnumerator(this, Length);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Represents a palette with 4 colors.
/// </summary>
public sealed class Palette4 : SinglePalette {
	private InlineArray4<SheetColor> _palette;
	private protected override int Length => 4;
	private protected override Span<SheetColor> Colors => _palette;

	/// <inheritdoc/>
	public Palette4() { }

	private Palette4(Palette4 source) {
		_palette = source._palette;
	}

	/// <inheritdoc/>
	public override Palette4 Clone() => new(this);
}

/// <summary>
/// Represents a palette with 8 colors.
/// </summary>
public sealed class Palette8 : SinglePalette {
	private InlineArray8<SheetColor> _palette;
	private protected override int Length => 8;
	private protected override Span<SheetColor> Colors => _palette;

	/// <inheritdoc/>
	public Palette8() { }

	private Palette8(Palette8 source) {
		_palette = source._palette;
	}

	/// <inheritdoc/>
	public override Palette8 Clone() => new(this);
}

/// <summary>
/// Represents a palette with 16 colors.
/// </summary>
public sealed class Palette16 : SinglePalette {
	private InlineArray16<SheetColor> _palette;
	private protected override int Length => 16;
	private protected override Span<SheetColor> Colors => _palette;

	/// <inheritdoc/>
	public Palette16() { }

	private Palette16(Palette16 source) {
		_palette = source._palette;
	}

	/// <inheritdoc/>
	public override Palette16 Clone() => new(this);
}

/// <summary>
/// Represents a palette with 256 colors.
/// </summary>
public sealed class Palette256 : SinglePalette {
	private Colors256 _palette;
	private protected override int Length => 256;
	private protected override Span<SheetColor> Colors => _palette;

	/// <inheritdoc/>
	public Palette256() { }

	private Palette256(Palette256 source) {
		_palette = source._palette;
	}

	/// <inheritdoc/>
	public override Palette256 Clone() => new(this);

	[InlineArray(256)]
	private struct Colors256 {
		internal SheetColor _color;
	}

}

/// <summary>
/// Internal type for enumerating built-in fixed-sized palette types.
/// </summary>
file sealed class SinglePaletteEnumerator : IEnumerator<SheetColor> {
	private readonly IPalette _palette;
	private int _index = -1;
	private readonly int _maxLength;

	internal SinglePaletteEnumerator(IPalette palette, int maxColors) {
		_palette = palette;
		_maxLength = maxColors;
	}

	public SheetColor Current {
		get {
			if ((uint) _index >= _maxLength) {
				ErrorHelpers.ThrowInvalidOperation();
			}

			return _palette[_index];
		}
	}

	object IEnumerator.Current => Current;

	SheetColor IEnumerator<SheetColor>.Current => throw new NotImplementedException();

	public bool MoveNext() {
		int index = _index + 1;

		if (index == _maxLength) {
			return false;
		}

		_index = index;
		return true;
	}

	void IEnumerator.Reset() => _index = -1;
	void IDisposable.Dispose() { }
}
