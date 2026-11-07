using System.Collections;

namespace Sheeter;

/// <summary>
/// Represents a collection of color palettes.
/// </summary>
public sealed class PaletteSet : IPalette {
	/// <summary>
	/// Gets the number of colors in each palette, including transparency.
	/// </summary>
	public int ColorsPerPalette { get; }

	private readonly List<SheetColor[]> _palettes = [];

	private SheetColor[]? _currentPalette = null;

	/// <summary>
	/// Gets the number of colors in the 
	/// </summary>
	public int CurrentSubPaletteSize { get; private set; }

	/// <summary>
	/// Gets the number of palettes currently in this palette set;
	/// </summary>
	public int Palettes => _palettes.Count;

	/// <summary>
	/// Gets or sets the minimum alpha level for a color in this palette set to be considered opaque.
	/// </summary>
	public byte OpacityThreshold { get; set; } = 1;

	SheetColor IPalette.this[int index] {
		get => _currentPalette?[index] ?? default;
		set => _currentPalette?[index] = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PaletteSet"/> class.
	/// </summary>
	/// <param name="paletteSize">The number of colors per palette (allowed range: [1, 256]).</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// If <paramref name="paletteSize"/> is not a value from 1 to 255, inclusive.
	/// </exception>
	public PaletteSet(int paletteSize) {
		if (paletteSize is < 1 or > 256) {
			ErrorHelpers.ThrowOutOfRangeArgument(nameof(paletteSize), "Palette size must be a positive number less than or equal to 256.");
		}
		
		ColorsPerPalette = paletteSize;
	}

	/// <summary>
	/// Clears all palettes and colors from this instance.
	/// </summary>
	public void Clear() {
		_palettes.Clear();
	}

	/// <summary>
	/// Adds a color to this palette set, starting a new palette if necessary.
	/// </summary>
	public void Add(SheetColor color) {
		if (_currentPalette is null || CurrentSubPaletteSize >= ColorsPerPalette) {
			_currentPalette = new SheetColor[ColorsPerPalette];
			_palettes.Add(_currentPalette);
			CurrentSubPaletteSize = 0;
		}

		_currentPalette[CurrentSubPaletteSize++] = color;
	}

	/// <summary>
	/// Adds every color in a range to this palette set, starting new palettes when necessary.
	/// </summary>
	public void AddMany(scoped ReadOnlySpan<SheetColor> colors) {
		foreach (SheetColor color in colors) {
			Add(color);
		}
	}

	/// <summary>
	/// Adds a new <see cref="SheetColor"/> to this palette, so long as it is not already present and not transparent,
	/// and so long as the addition of this color will not overflow into creating a new subpalette.
	/// </summary>
	/// <param name="color">The color to add.</param>
	/// <returns><see langword="true"/> if <paramref name="color"/> was added; otherwise, <see langword="false"/>.</returns>
	public bool AddRestricted(SheetColor color) {
		if (CurrentSubPaletteSize >= ColorsPerPalette) {
			return false;
		}

		if (HasColor(color)) {
			return false;
		}

		if (_currentPalette is null) {
			_currentPalette = new SheetColor[ColorsPerPalette];
			_currentPalette[0] = default;
			_palettes.Add(_currentPalette);
			CurrentSubPaletteSize = 1;
		}

		_currentPalette[CurrentSubPaletteSize++] = color;

		return true;
	}

	/// <summary>
	/// Checks whether or not a given <see cref="SheetColor"/> is already present in the palette and not transparent.
	/// </summary>
	/// <param name="color">The color to check.</param>
	/// <returns><see langword="true"/> if <paramref name="color"/> is found and not transparent; otherwise, <see langword="false"/>.</returns>
	public bool HasColor(SheetColor color) {
		return IndexOfSearch(color) is not 0;
	}

	/// <summary>
	/// <para>Checks whether or not a given <see cref="SheetColor"/> is already present in the palette and not transparent and returns its index.</para>
	/// <para>If <paramref name="color"/> has an alpha level below this instance's <see cref="OpacityThreshold"/>,
	/// it is considered transparent.</para>
	/// </summary>
	/// <param name="color">The color to search for.</param>
	/// <returns>The first index of <paramref name="color"/> if it is found and not transparent; otherwise, 0.</returns>
	public byte IndexOf(SheetColor color) {
		if (color.IsOpaque(OpacityThreshold)) {
			return IndexOfSearch(color);

		} else {
			return 0;
		}
	}

	private byte IndexOfSearch(SheetColor color) {
		foreach (var pal in _palettes) {
			int curMax;

			if (ReferenceEquals(pal, _currentPalette)) {
				curMax = CurrentSubPaletteSize;
			} else {
				curMax = ColorsPerPalette;
			}

			for (int i = 1; i < curMax; i++) {
				if (pal[i].EqualsIgnoringTransparency(color)) {
					return (byte) i;
				}
			}
		}

		return 0;
	}

	/// <summary>
	/// Gets an enumerator for this instance.
	/// </summary>
	public IEnumerator<SheetColor> GetEnumerator() => new Enumerator(this);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <summary>
	/// Enumerator for every color in this palette set.
	/// </summary>
	public sealed class Enumerator : IEnumerator<SheetColor> {
		private readonly PaletteSet _paletteSet;
		private int _colorIndex;
		private readonly int _maxColor;
		private int _paletteIndex = -1;
		private readonly int _maxPalette;

		private SheetColor[] _current = null!;

		internal Enumerator(PaletteSet paletteSet) {
			_paletteSet = paletteSet;
			_maxColor = paletteSet.ColorsPerPalette;
			_maxPalette = paletteSet.Palettes;
			Reset();
		}

		/// <inheritdoc/>
		public SheetColor Current {
			get {
				if (_current is null || (uint) _colorIndex >= _maxColor || (uint) _paletteIndex >= _maxPalette) {
					ErrorHelpers.ThrowInvalidOperation();
				}

				return _current[_colorIndex];
			}
		}

		object IEnumerator.Current => Current;

		/// <inheritdoc path="//summary|//returns"/>
		public bool MoveNext() {
			int index = _colorIndex + 1;

			if (index == _maxColor) {
				int pal = _paletteIndex + 1;

				if (pal >= _maxPalette) {
					return false;
				}

				index = 0;
				_paletteIndex = pal;
				_current = _paletteSet._palettes[pal];

			}

			_colorIndex = index;
			return true;
		}

		/// <inheritdoc path="//summary"/>
		public void Reset() {
			_colorIndex = _maxColor;
			_paletteIndex = -1;
		}

		void IDisposable.Dispose() { }
	}
}
