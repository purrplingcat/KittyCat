using PurrplingCore.Toolkit.Content;
using System.Runtime.CompilerServices;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Content;

public class NameTests
{
    [Theory]
    [InlineData("Player", "player")]
    [InlineData("Player_42", "PLAYER_42")]
    [InlineData("Jm\u00e9no", "JM\u00c9NO")]
    [InlineData("\U00010428", "\U00010400")]
    public void EqualityIgnoresCaseAndPreservesEachOriginalSpelling(string first, string second)
    {
        var firstName = new Name(first);
        var secondName = new Name(second);

        Assert.True(firstName == secondName);
        Assert.False(firstName != secondName);
        Assert.True(firstName.Equals(secondName));
        Assert.True(firstName.Equals((object)secondName));
        Assert.Equal(firstName.GetHashCode(), secondName.GetHashCode());
        Assert.Equal(first, firstName.ToString());
        Assert.Equal(second, secondName.ToString());
        Assert.Single(new HashSet<Name> { firstName, secondName });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    [InlineData("\u00a0\u2003")]
    public void EmptyInputMatchesDefaultName(string? value)
    {
        var name = new Name(value);

        Assert.Equal(default(Name), name);
        Assert.Equal(Name.None, name);
        Assert.Equal(Name.None, new Name(ReadOnlySpan<char>.Empty));
        Assert.Equal(Name.None, new Name(value.AsSpan()));
        Assert.Equal(Name.None, new Name(value, 0));
        Assert.Equal(Name.None, new Name(value.AsSpan(), 0));
        Assert.Equal(0, name.GetHashCode());
        Assert.Equal(0, name.Number);
        Assert.Equal(string.Empty, name.PlainName);
        Assert.Equal(string.Empty, name.ToString());
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" \t\r\n", true)]
    [InlineData("\u2003", true)]
    [InlineData("Player", false)]
    [InlineData("Player_0", false)]
    [InlineData("_0", false)]
    [InlineData(" _0", false)]
    [InlineData("\u2003_0", false)]
    public void ConstructionAndNoneComparisonDoNotAllocate(string? value, bool expected)
    {
        var expectedName = new Name(value);
        _ = new Name(value.AsSpan());
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var matches = 0;

        for (var index = 0; index < 1000; index++)
        {
            var name = new Name(value.AsSpan());
            if (name == expectedName && (name == Name.None) == expected)
            {
                matches++;
            }
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal(1000, matches);
        Assert.Equal(0, allocatedBytes);
    }

    [Fact]
    public void NamesWithCollidingLegacyHashesRemainDistinct()
    {
        var first = new Name("costarring");
        var second = new Name("liquid");

        Assert.NotEqual(first, second);
        Assert.Equal("costarring", first.ToString());
        Assert.Equal("liquid", second.ToString());
    }

    [Fact]
    public void SpanConstructorPreservesOnlyTheSuppliedSlice()
    {
        const string wrapped = "[Player_42]";
        var name = new Name(wrapped.AsSpan(1, wrapped.Length - 2));

        Assert.Equal(new Name("player_42"), name);
        Assert.Equal("Player_42", name.ToString());
    }

    [Fact]
    public void DistinctNumericSuffixesRemainDistinct()
    {
        Assert.NotEqual(new Name("Player"), new Name("Player_0"));
        Assert.NotEqual(new Name("Player_1"), new Name("Player_01"));
        Assert.NotEqual(new Name("Player"), new Name(" Player"));
        Assert.False(new Name("Player").Equals("Player"));
        Assert.False(new Name("Player").Equals(null));
    }

    [Fact]
    public void ConcurrentConstructionPreservesIdentityAndOriginalSpelling()
    {
        var names = new Name[128];

        Parallel.For(0, names.Length, index =>
        {
            names[index] = new Name(index % 2 == 0 ? "ConcurrentName" : "CONCURRENTNAME");
        });

        for (var index = 0; index < names.Length; index++)
        {
            Assert.Equal(names[0], names[index]);
            Assert.Equal(index % 2 == 0 ? "ConcurrentName" : "CONCURRENTNAME", names[index].ToString());
        }
    }

    [Fact]
    public void ManyCaseVariantsShareIdentityAndReuseExactSpellings()
    {
        const int count = 256;
        var names = new Name[count];
        var spellings = new string[count];

        for (var index = 0; index < count; index++)
        {
            var characters = "abcdefgh_NameIndexCheck".ToCharArray();
            for (var characterIndex = 0; characterIndex < 8; characterIndex++)
            {
                if ((index & (1 << characterIndex)) != 0)
                {
                    characters[characterIndex] = char.ToUpperInvariant(characters[characterIndex]);
                }
            }

            spellings[index] = new string(characters);
            names[index] = new Name(spellings[index]);
        }

        for (var index = 0; index < count; index++)
        {
            var repeated = new Name(spellings[index].AsSpan());

            Assert.Equal(names[0], repeated);
            Assert.Equal(spellings[index], repeated.ToString());
            Assert.Same(names[index].ToString(), repeated.ToString());
        }
    }

    [Fact]
    public void ReusingAnExistingSpellingDoesNotAllocate()
    {
        const string spelling = "NameStorage_AllocationCheck";
        var expected = new Name(spelling);
        var originalText = expected.ToString();
        _ = new Name(spelling.AsSpan());
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var matches = 0;

        for (var index = 0; index < 1000; index++)
        {
            var repeated = new Name(spelling.AsSpan());
            if (repeated == expected && ReferenceEquals(originalText, repeated.ToString()))
            {
                matches++;
            }
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal(1000, matches);
        Assert.Equal(0, allocatedBytes);
    }

    [Fact]
    public void NameOccupiesTwelveBytesWithoutManagedReferences()
    {
        Assert.Equal(12, Unsafe.SizeOf<Name>());
        Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<Name>());
    }

    [Theory]
    [InlineData("Player", "Player", 0)]
    [InlineData("Player_0", "Player", 1)]
    [InlineData("Player_42", "Player", 43)]
    [InlineData("Player_999999999", "Player", 1000000000)]
    [InlineData("Player_2147483646", "Player", int.MaxValue)]
    [InlineData("Player_2147483647", "Player_2147483647", 0)]
    [InlineData("Player_2147483648", "Player_2147483648", 0)]
    [InlineData("Player_4294967294", "Player_4294967294", 0)]
    [InlineData("Player_4294967295", "Player_4294967295", 0)]
    [InlineData("Player_4294967296", "Player_4294967296", 0)]
    [InlineData("Player_1_2", "Player_1", 3)]
    [InlineData("_0", "", 1)]
    [InlineData("Player_01", "Player_01", 0)]
    [InlineData("Player_00", "Player_00", 0)]
    [InlineData("Player_", "Player_", 0)]
    [InlineData("Player_-1", "Player_-1", 0)]
    [InlineData("Player_+1", "Player_+1", 0)]
    [InlineData("Player_ 1", "Player_ 1", 0)]
    [InlineData("Player_1 ", "Player_1 ", 0)]
    [InlineData("Player_1\0", "Player_1\0", 0)]
    [InlineData("Player_9999999999", "Player_9999999999", 0)]
    [InlineData("Player_99999999999999999999", "Player_99999999999999999999", 0)]
    [InlineData("Player_\u0661", "Player_\u0661", 0)]
    public void NumericSuffixParsingPreservesOriginalText(string value, string plainName, int number)
    {
        var name = new Name(value);
        var fromSpan = new Name(value.AsSpan());

        Assert.Equal(plainName, name.PlainName);
        Assert.Equal(number, name.Number);
        Assert.Equal(value, name.ToString());
        Assert.Equal(name, fromSpan);
        Assert.Equal(number, fromSpan.Number);
        Assert.Equal(value, fromSpan.ToString());
        Assert.Equal(name, new Name(name.PlainName, name.Number));
    }

    [Fact]
    public void NumberedNamesShareTheirBaseText()
    {
        var plain = new Name("SharedNumberPool");
        var first = new Name("SharedNumberPool_1");
        var second = new Name("SharedNumberPool_2");

        Assert.Same(plain.PlainName, first.PlainName);
        Assert.Same(first.PlainName, second.PlainName);
        Assert.NotEqual(plain, first);
        Assert.NotEqual(first, second);
        Assert.Equal(0, plain.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(43)]
    [InlineData(int.MaxValue)]
    public void ExplicitNumbersMatchParsedNames(int number)
    {
        var expectedText = number == 0 ? "ExplicitNumber" : $"ExplicitNumber_{number - 1}";
        var name = new Name("ExplicitNumber", number);
        var parsed = new Name(expectedText);
        var fromSpan = new Name("ExplicitNumber".AsSpan(), number);

        Assert.Equal(number, name.Number);
        Assert.Equal("ExplicitNumber", name.PlainName);
        Assert.Equal(parsed, name);
        Assert.Equal(name, fromSpan);
        Assert.Equal(parsed.GetHashCode(), name.GetHashCode());
        Assert.Equal(expectedText, name.ToString());
    }

    [Theory]
    [InlineData("Player_01")]
    [InlineData("Player_42")]
    public void ExplicitNumberTreatsBaseTextLiterally(string plainName)
    {
        var name = new Name(plainName, 3);

        Assert.Equal(plainName, name.PlainName);
        Assert.Equal($"{plainName}_2", name.ToString());
        Assert.Equal(new Name($"{plainName}_2"), name);
    }

    [Fact]
    public void MaximumNumberDoesNotWrapOrEqualAnUnnumberedName()
    {
        var plain = new Name("NumberBoundary", 0);
        var maximum = new Name("NumberBoundary", int.MaxValue);

        Assert.Equal(int.MaxValue, maximum.Number);
        Assert.Equal("NumberBoundary_2147483646", maximum.ToString());
        Assert.Equal(new Name("NumberBoundary_2147483646"), maximum);
        Assert.NotEqual(plain, maximum);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NegativeInternalNumbersAreRejected(int number)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Name("Player", number));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Name("Player".AsSpan(), number));
    }

    [Theory]
    [InlineData("Player", 0)]
    [InlineData("Player_0", 1)]
    [InlineData("Player_1", 2)]
    [InlineData("Player_00", 0)]
    public void NumberUsesInternalEncodingAndRoundTrips(string value, int number)
    {
        var name = new Name(value);

        Assert.Equal(number, name.Number);
        Assert.Equal(value, name.ToString());
        Assert.Equal(name, new Name(name.PlainName, name.Number));
        Assert.NotEqual(new Name("Player"), new Name("Player_0"));
        Assert.NotEqual(new Name("Player_0"), new Name("Player_00"));
    }

    [Fact]
    public void NumberedNameConstructionDoesNotAllocateForAnExistingBase()
    {
        const string value = "NumberAllocationCheck_42";
        var expected = new Name(value);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var matches = 0;

        for (var index = 0; index < 1000; index++)
        {
            if (new Name(value.AsSpan()) == expected)
            {
                matches++;
            }
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal(1000, matches);
        Assert.Equal(0, allocatedBytes);
    }
}