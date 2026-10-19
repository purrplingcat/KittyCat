namespace PurrplingCore.Toolkit.Content;

internal static class NamePool
{
    private static readonly List<string> _strings = [string.Empty];
    private static readonly HashSet<int> _index = new(new IndexComparer(_strings));
    private static readonly Lock _sync = new();

    public static void Intern(ReadOnlySpan<char> value, out int comparisonId, out int displayId)
    {
        if (value.IsEmpty)
        {
            comparisonId = 0;
            displayId = 0;
            return;
        }

        lock (_sync)
        {
            var lookup = _index.GetAlternateLookup<LookupKey>();
            var hasComparison = lookup.TryGetValue(new LookupKey(value, ignoreCase: true), out var comparisonKey);

            if (lookup.TryGetValue(new LookupKey(value, ignoreCase: false), out displayId))
            {
                comparisonId = -comparisonKey;
                return;
            }

            displayId = _strings.Count;
            _strings.Add(value.ToString());
            _index.Add(displayId);

            if (!hasComparison)
            {
                comparisonKey = -displayId;
                _index.Add(comparisonKey);
            }

            comparisonId = -comparisonKey;
        }
    }

    public static string GetString(int displayId)
    {
        if (displayId == 0)
        {
            return string.Empty;
        }

        lock (_sync)
        {
            return _strings[displayId];
        }
    }

    private readonly ref struct LookupKey(ReadOnlySpan<char> value, bool ignoreCase)
    {
        public ReadOnlySpan<char> Value { get; } = value;
        public bool IgnoreCase { get; } = ignoreCase;

        public StringComparison Comparison => IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    /// <summary>
    /// Resolves pool IDs for hashing; negative keys represent case-insensitive identities.
    /// </summary>
    private sealed class IndexComparer(List<string> strings) :
        IEqualityComparer<int>,
        IAlternateEqualityComparer<LookupKey, int>
    {
        public bool Equals(int left, int right)
        {
            return Equals(GetKey(left), right);
        }

        public int GetHashCode(int key)
        {
            return GetHashCode(GetKey(key));
        }

        public bool Equals(LookupKey alternate, int key)
        {
            return alternate.IgnoreCase == (key < 0)
                && alternate.Value.Equals(strings[Math.Abs(key)], alternate.Comparison);
        }

        public int GetHashCode(LookupKey alternate)
        {
            return HashCode.Combine(
                string.GetHashCode(alternate.Value, alternate.Comparison),
                alternate.IgnoreCase);
        }

        int IAlternateEqualityComparer<LookupKey, int>.Create(LookupKey alternate)
        {
            throw new NotSupportedException("Entries must be added through NamePool.Intern.");
        }

        private LookupKey GetKey(int key)
        {
            return new LookupKey(strings[Math.Abs(key)], ignoreCase: key < 0);
        }
    }
}