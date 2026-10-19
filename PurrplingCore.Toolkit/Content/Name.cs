using System.Diagnostics;
using System.Globalization;

namespace PurrplingCore.Toolkit.Content;

/// <summary>
/// An interned, case-insensitive name that preserves its original spelling.
/// </summary>
/// <remarks>
/// Names occupy twelve bytes and compare by a process-local integer identity and number.
/// Canonical numeric suffixes are stored separately, allowing numbered names to share their text.
/// Interned strings are retained for the process lifetime; persist the text, not the hash code.
/// A default name or an empty or whitespace-only input represents <see cref="None"/> and formats as an empty string.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct Name : IEquatable<Name>
{
    public static readonly Name None = default;

    private readonly int _comparisonId;
    private readonly int _displayId;
    private readonly uint _number;

    /// <summary>
    /// Gets the internal instance number: zero means no suffix, and one represents "_0".
    /// </summary>
    public int Number => (int)_number;

    /// <summary>
    /// Gets the original spelling of the name without its numeric suffix.
    /// </summary>
    public string PlainName => NamePool.GetString(_displayId);

    public Name(string? value) : this(value.AsSpan())
    {
    }

    public Name(ReadOnlySpan<char> value)
    {
        if (value.IsWhiteSpace())
        {
            value = [];
        }

        _number = SplitNumber(ref value);
        NamePool.Intern(value, out _comparisonId, out _displayId);
    }

    /// <summary>
    /// Creates a name from literal base text and an internal instance number.
    /// Zero means no suffix; positive values format as one less than the supplied number.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The internal number is negative.</exception>
    public Name(string? value, int internalNumber) : this(value.AsSpan(), internalNumber)
    {
    }

    /// <summary>
    /// Creates a name from literal base text and an internal instance number.
    /// Zero means no suffix; positive values format as one less than the supplied number.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The internal number is negative.</exception>
    public Name(ReadOnlySpan<char> value, int internalNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(internalNumber);
        if (internalNumber == 0 && value.IsWhiteSpace())
        {
            value = ReadOnlySpan<char>.Empty;
        }

        _number = (uint)internalNumber;
        NamePool.Intern(value, out _comparisonId, out _displayId);
    }

    public bool IsNone => this == None;

    public override string ToString()
    {
        var text = NamePool.GetString(_displayId);
        return _number != 0
            ? string.Create(CultureInfo.InvariantCulture, $"{text}_{_number - 1}")
            : text;
    }

    public override int GetHashCode() => _number != 0
        ? HashCode.Combine(_comparisonId, _number)
        : _comparisonId;

    public bool Equals(Name other) => _comparisonId == other._comparisonId && _number == other._number;

    public override bool Equals(object? obj) => obj is Name other && Equals(other);

    public static bool operator ==(Name left, Name right) => left.Equals(right);

    public static bool operator !=(Name left, Name right) => !left.Equals(right);

    private static uint SplitNumber(ref ReadOnlySpan<char> value)
    {
        // Only the final underscore can introduce a numeric suffix.
        var separator = value.LastIndexOf('_');
        if (separator < 0)
        {
            return 0;
        }

        // Keep empty suffixes, leading zeros and overlong numbers as literal text.
        var suffix = value[(separator + 1)..];
        if (suffix.IsEmpty || suffix.Length > 10 || (suffix.Length > 1 && suffix[0] == '0'))
        {
            return 0;
        }

        // Match FName's signed range, reserving room for the internal +1 offset.
        const uint maxSuffix = int.MaxValue - 1;
        uint number = 0;
        foreach (var character in suffix)
        {
            if (!char.IsAsciiDigit(character))
            {
                return 0;
            }

            var digit = (uint)(character - '0');
            if (number > maxSuffix / 10 || (number == maxSuffix / 10 && digit > maxSuffix % 10))
            {
                return 0;
            }

            number = number * 10 + digit;
        }

        // Reserve zero for "no number"; leave the original text untouched unless parsing succeeds.
        value = value[..separator];
        return number + 1;
    }
}