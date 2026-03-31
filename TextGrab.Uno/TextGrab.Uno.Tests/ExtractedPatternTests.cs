using System.Text.RegularExpressions;
using TextGrab.Shared;

namespace TextGrab.Uno.Tests;

public class ExtractedPatternTests
{
    [Test]
    public void Constructor_GeneratesAllPrecisionLevels()
    {
        // Given
        string input = "Abc123";

        // When
        ExtractedPattern extractedPattern = new(input);

        // Then
        Assert.That(extractedPattern, Is.Not.Null);
        Assert.That(extractedPattern.OriginalText, Is.EqualTo(input));
        Assert.That(extractedPattern.AllPatterns.Count, Is.EqualTo(6)); // 0-5 = 6 levels
        Assert.That(extractedPattern.IgnoreCase, Is.False); // Default should be case-sensitive
    }

    [Test]
    public void Constructor_WithIgnoreCase_GeneratesAllPrecisionLevels()
    {
        // Given
        string input = "Abc123";

        // When
        ExtractedPattern extractedPattern = new(input, ignoreCase: true);

        // Then
        Assert.That(extractedPattern, Is.Not.Null);
        Assert.That(extractedPattern.OriginalText, Is.EqualTo(input));
        Assert.That(extractedPattern.AllPatterns.Count, Is.EqualTo(6)); // 0-5 = 6 levels
        Assert.That(extractedPattern.IgnoreCase, Is.True);
    }

    [TestCase("Abc123", 0, @"\S+")]
    [TestCase("Abc123", 1, @"\w+")]
    [TestCase("Abc123", 2, @"\w{3}\w{3}")]
    [TestCase("Abc123", 3, @"[A-Za-z]{3}\d{3}")]
    [TestCase("Abc123", 4, @"(?i)Abc123")]
    [TestCase("Abc123", 5, @"Abc123")]
    public void GetPattern_ReturnsCorrectPatternForEachLevel(string input, int level, string expectedPattern)
    {
        // Given
        ExtractedPattern extractedPattern = new(input);

        // When
        string actualPattern = extractedPattern.GetPattern(level);

        // Then
        Assert.That(actualPattern, Is.EqualTo(expectedPattern));
    }

    [TestCase(-1)]
    [TestCase(6)]
    [TestCase(10)]
    public void GetPattern_WithInvalidLevel_ReturnsDefaultLevel(int invalidLevel)
    {
        // Given
        string input = "Test123";
        ExtractedPattern extractedPattern = new(input);
        string expectedPattern = extractedPattern.GetPattern(ExtractedPattern.DefaultPrecisionLevel);

        // When
        string actualPattern = extractedPattern.GetPattern(invalidLevel);

        // Then
        Assert.That(actualPattern, Is.EqualTo(expectedPattern));
    }

    [Test]
    public void GetPattern_CalledMultipleTimes_ReturnsSamePattern()
    {
        // Given
        string input = "Hello123";
        ExtractedPattern extractedPattern = new(input);

        // When - Call multiple times
        string pattern1 = extractedPattern.GetPattern(3);
        string pattern2 = extractedPattern.GetPattern(3);
        string pattern3 = extractedPattern.GetPattern(3);

        // Then - Should always return the same pre-generated pattern
        Assert.That(pattern2, Is.EqualTo(pattern1));
        Assert.That(pattern3, Is.EqualTo(pattern2));
    }

    [Test]
    public void AllPatterns_ContainsAllSixLevels()
    {
        // Given
        ExtractedPattern extractedPattern = new("Test");

        // When
        IReadOnlyDictionary<int, string> allPatterns = extractedPattern.AllPatterns;

        // Then
        Assert.That(allPatterns.Keys, Does.Contain(0));
        Assert.That(allPatterns.Keys, Does.Contain(1));
        Assert.That(allPatterns.Keys, Does.Contain(2));
        Assert.That(allPatterns.Keys, Does.Contain(3));
        Assert.That(allPatterns.Keys, Does.Contain(4));
        Assert.That(allPatterns.Keys, Does.Contain(5));
    }

    [TestCase(0, "Any Text")]
    [TestCase(1, "Words")]
    [TestCase(2, "Length")]
    [TestCase(3, "Types")]
    [TestCase(4, "Per Char")]
    [TestCase(5, "Exact")]
    public void GetLevelLabel_ReturnsCorrectLabel(int level, string expectedLabel)
    {
        // When
        string actualLabel = ExtractedPattern.GetLevelLabel(level);

        // Then
        Assert.That(actualLabel, Is.EqualTo(expectedLabel));
    }

    [Test]
    public void GetLevelDescription_ReturnsDescriptionForAllLevels()
    {
        // Given/When/Then
        for (int level = 0; level <= 5; level++)
        {
            string description = ExtractedPattern.GetLevelDescription(level);
            Assert.That(string.IsNullOrWhiteSpace(description), Is.False);
            Assert.That(description, Does.Not.Contain("Unknown"));
        }
    }

    [Test]
    public void GetLevelDescription_WithInvalidLevel_ReturnsUnknownMessage()
    {
        // When
        string description = ExtractedPattern.GetLevelDescription(99);

        // Then
        Assert.That(description, Does.Contain("Unknown"));
    }

    [Test]
    public void EmptyString_GeneratesValidPatterns()
    {
        // Given
        ExtractedPattern extractedPattern = new("");

        // When/Then - Should not throw and should return empty patterns
        for (int level = 0; level <= 5; level++)
        {
            string pattern = extractedPattern.GetPattern(level);
            Assert.That(pattern, Is.Not.Null);
        }
    }

    [TestCase("(123)-555-6789", 3, @"(\()\d{3}(\))-\d{3}-\d{4}")]
    [TestCase("Hello World!", 3, @"[A-Za-z]{5}\s[A-Za-z]{5}!")]
    [TestCase("ab12ab12ab12ab12ab12", 3, @"([A-Za-z]{2}\d{2}){5}")]
    [TestCase("Test", 4, @"(?i)Test")]
    [TestCase("ABC", 4, @"(?i)ABC")]
    [TestCase("A.B", 5, @"A\.B")]
    public void ComplexPatterns_GeneratedCorrectly(string input, int level, string expectedPattern)
    {
        // Given
        ExtractedPattern extractedPattern = new(input);

        // When
        string actualPattern = extractedPattern.GetPattern(level);

        // Then
        Assert.That(actualPattern, Is.EqualTo(expectedPattern));
    }

    [Test]
    public void AllPatterns_IsReadOnly()
    {
        // Given
        ExtractedPattern extractedPattern = new("Test");

        // When
        IReadOnlyDictionary<int, string> allPatterns = extractedPattern.AllPatterns;

        // Then
        Assert.That(allPatterns, Is.AssignableTo<IReadOnlyDictionary<int, string>>());
    }

    [Test]
    public void Constants_HaveCorrectValues()
    {
        // Then
        Assert.That(ExtractedPattern.MinPrecisionLevel, Is.EqualTo(0));
        Assert.That(ExtractedPattern.MaxPrecisionLevel, Is.EqualTo(5));
        Assert.That(ExtractedPattern.DefaultPrecisionLevel, Is.EqualTo(3));
    }

    [Test]
    public void PrecisionLevels_MatchCountDecreases_FromLevel0ToLevel5()
    {
        // Given - A large block of text with various patterns
        string largeText = @"
Hello World! This is a test of the pattern matching system.
Test123 and ABC456 are examples of mixed text.
Email: test@example.com and phone: (123)-456-7890
More words: hello, HELLO, HeLLo - case variations
Numbers: 123, 456, 789
Special chars: @#$%^&*()
test test TEST Test
abc ABC Abc
Mixed: Test123, ABC456, xyz789
URL: https://example.com/path?query=value
Multiple  spaces   and	tabs
Line1
Line2
Line3
The quick brown fox jumps over the lazy dog.
UPPERCASE TEXT AND lowercase text and MiXeD CaSe TeXt.
test123 test456 test789 pattern123 pattern456
Same word repeated: test test test test test
";

        // Extract pattern from a common word "test"
        string searchTerm = "test";
        ExtractedPattern extractedPattern = new(searchTerm);

        // When - Count matches at each precision level
        Dictionary<int, int> matchCountsByLevel = [];

        for (int level = 0; level <= 5; level++)
        {
            string pattern = extractedPattern.GetPattern(level);
            MatchCollection matches = Regex.Matches(largeText, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            matchCountsByLevel[level] = matches.Count;
        }

        // Then - Verify match counts follow expected precision patterns

        // Verify level 2 is more restrictive than level 1
        Assert.That(matchCountsByLevel[2], Is.LessThanOrEqualTo(matchCountsByLevel[1]),
          $"Level 2 (Length) should match at most as many as Level 1 (Words). L1={matchCountsByLevel[1]}, L2={matchCountsByLevel[2]}");

        // Verify level 3 is more restrictive than level 2
        Assert.That(matchCountsByLevel[3], Is.LessThanOrEqualTo(matchCountsByLevel[2]),
                   $"Level 2 (Length) should match at least as many as Level 3 (Types). L2={matchCountsByLevel[2]}, L3={matchCountsByLevel[3]}");

        // Verify level 4 is case-insensitive but position-specific
        Assert.That(matchCountsByLevel[4], Is.GreaterThan(0), "Level 4 should find at least some matches");

        // Level 5 is exact match
        Assert.That(matchCountsByLevel[5], Is.GreaterThan(0), "Level 5 should find at least some exact matches");

        // Level 5 should generally be most restrictive
        Assert.That(matchCountsByLevel[5], Is.LessThanOrEqualTo(matchCountsByLevel[4]),
       $"Level 5 (Exact) should match at most as many as Level 4 (Per Char). L4={matchCountsByLevel[4]}, L5={matchCountsByLevel[5]}");
    }

    [Test]
    public void PrecisionLevels_SpecificPattern_MatchCountValidation()
    {
        // Given - Text with a specific repeating pattern
        string text = @"
ABC123 abc123 AbC123 ABC456 test123 TEST123
XYZ789 xyz789 XyZ789 pattern123
DATA001 data001 DaTa001 INFO999
";

        // Extract pattern from "ABC123"
        string searchTerm = "ABC123";
        ExtractedPattern extractedPattern = new(searchTerm);

        // When - Count matches at each level
        Dictionary<int, int> matchCounts = [];
        for (int level = 0; level <= 5; level++)
        {
            string pattern = extractedPattern.GetPattern(level);
            MatchCollection matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            matchCounts[level] = matches.Count;
        }

        // Then - Verify expected behavior
        Assert.That(matchCounts[0], Is.GreaterThan(10), "Level 0 should match many non-whitespace sequences");
        Assert.That(matchCounts[1], Is.GreaterThan(10), "Level 1 should match many word sequences");
        Assert.That(matchCounts[2], Is.LessThanOrEqualTo(matchCounts[1]), "Level 2 should be more restrictive than Level 1");
        Assert.That(matchCounts[3], Is.LessThanOrEqualTo(matchCounts[2]), "Level 3 should be more restrictive than Level 2");
        Assert.That(matchCounts[4], Is.GreaterThanOrEqualTo(3), $"Level 4 should match at least 3 case variations, got {matchCounts[4]}");
        Assert.That(matchCounts[4], Is.LessThan(matchCounts[3]), "Level 4 should be more restrictive than Level 3");
        Assert.That(matchCounts[5], Is.GreaterThanOrEqualTo(1), $"Level 5 should match at least once, got {matchCounts[5]}");
        Assert.That(matchCounts[5], Is.LessThanOrEqualTo(matchCounts[4]), "Level 5 should be most restrictive");
        Assert.That(matchCounts[5], Is.EqualTo(matchCounts[4]));
    }

    [Test]
    public void PrecisionLevels_DemonstrateHierarchy_WithSimpleText()
    {
        // Given - Simple repeating text to demonstrate precision hierarchy clearly
        string text = "test Test TEST teST test123 testing best rest";

        string searchTerm = "test";
        ExtractedPattern extractedPattern = new(searchTerm);

        // When - Count matches at each level with case-insensitive search
        Dictionary<int, int> matchCounts = [];
        for (int level = 0; level <= 5; level++)
        {
            string pattern = extractedPattern.GetPattern(level);
            MatchCollection matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            matchCounts[level] = matches.Count;
        }

        // Then - Verify the expected precision hierarchy
        Assert.That(matchCounts[0], Is.EqualTo(8));
        Assert.That(matchCounts[1], Is.EqualTo(8));
        Assert.That(matchCounts[2], Is.EqualTo(8));
        Assert.That(matchCounts[3], Is.EqualTo(8));
        Assert.That(matchCounts[4], Is.EqualTo(6));
        Assert.That(matchCounts[5], Is.EqualTo(6));

        // Verify hierarchy: each level should be same or more restrictive than previous
        Assert.That(matchCounts[1], Is.LessThanOrEqualTo(matchCounts[0]), "Level 1 should be <= Level 0");
        Assert.That(matchCounts[2], Is.LessThanOrEqualTo(matchCounts[1]), "Level 2 should be <= Level 1");
        Assert.That(matchCounts[3], Is.LessThanOrEqualTo(matchCounts[2]), "Level 3 should be <= Level 2");
        Assert.That(matchCounts[4], Is.LessThanOrEqualTo(matchCounts[3]), "Level 4 should be <= Level 3");
        Assert.That(matchCounts[5], Is.LessThanOrEqualTo(matchCounts[4]), "Level 5 should be <= Level 4");
    }

    // DetermineStartingLevel Tests

    [TestCase("a", 5)]
    [TestCase("AB", 4)]
    [TestCase("xyz", 4)]
    [TestCase("test", 4)]
    public void DetermineStartingLevel_ShortText_ReturnsHighPrecision(string input, int expectedLevel)
    {
        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(input);

        // Then
        Assert.That(actualLevel, Is.EqualTo(expectedLevel));
    }

    [Test]
    public void DetermineStartingLevel_LongText_ReturnsLowerPrecision()
    {
        // Given - Text longer than 25 characters
        string longText = "This is a very long string that exceeds the limit";

        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(longText);

        // Then
        Assert.That(actualLevel, Is.EqualTo(2));
    }

    [TestCase("123", 2)]
    [TestCase("4567", 2)]
    [TestCase("999", 2)]
    [TestCase("12345", 2)]
    public void DetermineStartingLevel_PureNumbers_ReturnsLengthFlexible(string input, int expectedLevel)
    {
        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(input);

        // Then
        Assert.That(actualLevel, Is.EqualTo(expectedLevel));
    }

    [TestCase("ABC-123", 3)]
    [TestCase("user_456", 3)]
    [TestCase("ID:789", 3)]
    [TestCase("file.txt", 3)]
    public void DetermineStartingLevel_AlphanumericWithDelimiters_ReturnsSeparatorAgnostic(string input, int expectedLevel)
    {
        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(input);

        // Then
        Assert.That(actualLevel, Is.EqualTo(expectedLevel));
    }

    [TestCase("the quick brown", 1)]
    [TestCase("one two three four", 1)]
    [TestCase("hello world again", 1)]
    public void DetermineStartingLevel_MultipleWords_ReturnsStructureOnly(string input, int expectedLevel)
    {
        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(input);

        // Then
        Assert.That(actualLevel, Is.EqualTo(expectedLevel));
    }

    [TestCase("user123", 3)]
    [TestCase("AB12CD", 3)]
    [TestCase("test456", 3)]
    public void DetermineStartingLevel_AlphanumericMixed_ReturnsCharacterClass(string input, int expectedLevel)
    {
        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(input);

        // Then
        Assert.That(actualLevel, Is.EqualTo(expectedLevel));
    }

    [TestCase("Hello", 4)]
    [TestCase("World", 4)]
    [TestCase("Test", 4)]
    [TestCase("Testing", 4)]
    public void DetermineStartingLevel_SimpleWord_ReturnsCaseInsensitive(string input, int expectedLevel)
    {
        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(input);

        // Then
        Assert.That(actualLevel, Is.EqualTo(expectedLevel));
    }

    [TestCase("#42", 4)]
    [TestCase("@joe", 4)]
    [TestCase("v1.2", 3)]
    [TestCase("$USD", 4)]
    [TestCase("#hashtag", 3)]
    public void DetermineStartingLevel_SpecialCharsShort_ReturnsSeparatorAgnostic(string input, int expectedLevel)
    {
        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(input);

        // Then
        Assert.That(actualLevel, Is.EqualTo(expectedLevel));
    }

    [TestCase("", 3)]
    [TestCase("   ", 3)]
    [TestCase(null, 3)]
    public void DetermineStartingLevel_EmptyOrWhitespace_ReturnsDefault(string? input, int expectedLevel)
    {
        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(input!);

        // Then
        Assert.That(actualLevel, Is.EqualTo(expectedLevel));
    }

    [TestCase("123-456-7890", 3)]
    [TestCase("XX-YY-ZZ", 3)]
    [TestCase("ID_001_ABC", 3)]
    public void DetermineStartingLevel_RepeatingPatterns_ReturnsCharacterClass(string input, int expectedLevel)
    {
        // When
        int actualLevel = ExtractedPattern.DetermineStartingLevel(input);

        // Then
        Assert.That(actualLevel, Is.EqualTo(expectedLevel));
    }

    [Test]
    public void DetermineStartingLevel_RealWorldExamples_ProduceSensibleDefaults()
    {
        // Given - Various real-world examples
        Dictionary<string, int> testCases = new()
        {
            // Short exact matches
            { "5", 5 },
            { "OK", 4 },

            // IDs and codes
            { "USR123", 3 },
            { "ABC-DEF", 3 },
            { "item_42", 3 },

            // Numbers
            { "42", 2 },
            { "12345", 2 },

            // Words
            { "name", 4 },
            { "hello world", 3 },
            { "one two three", 1 },

            // Long strings
            { "This is a longer string that exceeds the limit", 2 },

            // Special characters
            { "@user", 3 },
            { "v2.0.1", 3 },
            { "#tag", 4 },
        };

        // When/Then
        foreach (KeyValuePair<string, int> testCase in testCases)
        {
            int actualLevel = ExtractedPattern.DetermineStartingLevel(testCase.Key);
            Assert.That(actualLevel, Is.EqualTo(testCase.Value),
                $"Input '{testCase.Key}' expected level {testCase.Value} but got {actualLevel}");
        }
    }

    [Test]
    public void DetermineStartingLevel_EdgeCases_HandledGracefully()
    {
        // Given - Edge cases
        Assert.That(ExtractedPattern.DetermineStartingLevel(""), Is.EqualTo(3));
        Assert.That(ExtractedPattern.DetermineStartingLevel(null!), Is.EqualTo(3));
        Assert.That(ExtractedPattern.DetermineStartingLevel("   "), Is.EqualTo(3));
        Assert.That(ExtractedPattern.DetermineStartingLevel("\t"), Is.EqualTo(3));
        Assert.That(ExtractedPattern.DetermineStartingLevel(" a "), Is.EqualTo(5));
    }

    // Case Sensitivity Tests

    [TestCase("Abc123", 0, false, @"\S+")]
    [TestCase("Abc123", 0, true, @"(?i)\S+")]
    [TestCase("Abc123", 1, false, @"\w+")]
    [TestCase("Abc123", 1, true, @"(?i)\w+")]
    [TestCase("Abc123", 2, false, @"\w{3}\w{3}")]
    [TestCase("Abc123", 2, true, @"(?i)\w{3}\w{3}")]
    [TestCase("Abc123", 3, false, @"[A-Za-z]{3}\d{3}")]
    [TestCase("Abc123", 3, true, @"(?i)[A-Za-z]{3}\d{3}")]
    [TestCase("Abc123", 4, false, @"(?i)Abc123")]
    [TestCase("Abc123", 4, true, @"(?i)Abc123")]
    [TestCase("Abc123", 5, false, @"Abc123")]
    [TestCase("Abc123", 5, true, @"Abc123")]
    public void ExtractSimplePattern_WithCaseSensitivity_IncludesCorrectFlag(
        string input, int level, bool ignoreCase, string expectedPattern)
    {
        // When
        string actualPattern = StringMethods.ExtractSimplePattern(input, level, ignoreCase);

        // Then
        Assert.That(actualPattern, Is.EqualTo(expectedPattern));
    }

    [Test]
    public void ExtractSimplePattern_CaseInsensitive_MatchesDifferentCases()
    {
        // Given
        string input = "Test";
        string text = "test TEST TeSt Test testing";

        // When - Generate case-insensitive pattern
        string pattern = StringMethods.ExtractSimplePattern(input, 4, ignoreCase: true);
        MatchCollection matches = Regex.Matches(text, pattern);

        // Then - Should match all case variations
        Assert.That(matches.Count, Is.EqualTo(5));
        Assert.That(pattern, Does.Contain("(?i)"));
    }

    [Test]
    public void ExtractSimplePattern_CaseSensitive_MatchesExactCase()
    {
        // Given
        string input = "Test";
        string text = "test TEST TeSt Test testing";

        // When - Generate case-sensitive pattern (default)
        string pattern = StringMethods.ExtractSimplePattern(input, 5, ignoreCase: false);
        MatchCollection matches = Regex.Matches(text, pattern);

        // Then - Should match only exact case
        Assert.That(matches.Count, Is.EqualTo(1));
        Assert.That(pattern, Does.Not.Contain("(?i)"));
    }

    [TestCase("Hello", 0, true)]
    [TestCase("World123", 1, true)]
    [TestCase("Test42", 2, true)]
    [TestCase("ABC", 3, true)]
    [TestCase("xyz", 4, true)]
    public void ExtractSimplePattern_AllLevels_SupportCaseInsensitiveFlag(string input, int level, bool ignoreCase)
    {
        // When
        string pattern = StringMethods.ExtractSimplePattern(input, level, ignoreCase);

        // Then
        if (ignoreCase)
        {
            Assert.That(pattern, Does.StartWith("(?i)"));
        }
        else
        {
            Assert.That(pattern, Does.Not.Contain("(?i)"));
        }
    }

    [Test]
    public void ExtractSimplePattern_DefaultCaseSensitivity_IsFalse()
    {
        // Given
        string input = "Test";

        // When - Call without specifying ignoreCase (using default)
        string pattern = StringMethods.ExtractSimplePattern(input, 3);

        // Then - Default should be case-sensitive (no flag)
        Assert.That(pattern, Does.Not.Contain("(?i)"));
    }

    [Test]
    public void ExtractSimplePattern_CaseInsensitive_CrossPlatformCompatible()
    {
        // Given
        string input = "Test123";
        string text = "test123 TEST123 TeSt123 Test123";

        // When - Generate pattern with inline flag
        string pattern = StringMethods.ExtractSimplePattern(input, 4);

        // Create regex without RegexOptions to verify inline flag works standalone
        Regex regex = new(pattern);
        MatchCollection matches = regex.Matches(text);

        // Then - Inline flag should make it case-insensitive without needing RegexOptions
        Assert.That(matches.Count, Is.EqualTo(4));
        Assert.That(pattern, Does.Contain("(?i)"));
    }

    [TestCase("ABC", "abc ABC aBc", 3, true)]
    [TestCase("ABC", "abc ABC aBc", 3, false)]
    [TestCase("123", "123 456", 2, true)]
    [TestCase("Test", "test TEST", 4, true)]
    public void ExtractSimplePattern_CaseFlag_AffectsMatchBehavior(
        string input, string text, int level, bool ignoreCase)
    {
        // When
        string pattern = StringMethods.ExtractSimplePattern(input, level, ignoreCase);
        Regex regex = new(pattern);
        int matchCount = regex.Matches(text).Count;

        // Then - Case-insensitive should find more or equal matches
        if (ignoreCase)
        {
            Assert.That(matchCount, Is.GreaterThan(0), "Case-insensitive pattern should find matches");
        }

        // Verify flag presence
        Assert.That(pattern.Contains("(?i)"), Is.EqualTo(ignoreCase));
    }

    [Test]
    public void ExtractSimplePattern_ComplexPattern_WithCaseInsensitivity()
    {
        // Given
        string input = "Hello World!";
        string text = "hello world! HELLO WORLD! HeLLo WoRLd!";

        // When - Level 3 with case insensitivity
        string pattern = StringMethods.ExtractSimplePattern(input, 3, ignoreCase: true);
        Regex regex = new(pattern);
        MatchCollection matches = regex.Matches(text);

        // Then
        Assert.That(pattern, Does.Contain("(?i)"));
        Assert.That(matches.Count, Is.EqualTo(3));
    }

    [Test]
    public void ExtractedPattern_WithIgnoreCase_AllPatternsHaveFlag()
    {
        // Given
        string input = "Abc123";
        ExtractedPattern extractedPattern = new(input, ignoreCase: true);

        // When/Then - All patterns should have the (?i) flag
        for (int level = 0; level <= 4; level++)
        {
            string pattern = extractedPattern.GetPattern(level);
            Assert.That(pattern, Does.StartWith("(?i)"),
                $"Level {level} pattern should start with (?i) but got: {pattern}");
        }
    }
}
