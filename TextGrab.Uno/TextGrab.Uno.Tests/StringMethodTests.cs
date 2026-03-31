using System.Text;
using TextGrab.Shared;

namespace TextGrab.Uno.Tests;

/// <summary>
/// Ported from Tests/StringMethodTests.cs (WPF xUnit).
/// Many StringMethods split by Environment.NewLine (\r\n on Windows),
/// so multi-line test inputs use explicit \r\n to be deterministic
/// regardless of source-file line endings.
/// </summary>
public class StringMethodTests
{
    [Test]
    public void MakeMultiLineStringSingleLine()
    {
        string bodyOfText = "\r\n\r\nThis has\r\nmultiple\r\nlines\r\n\r\n\r\n";
        string lineOfText = "This has multiple lines";
        Assert.That(bodyOfText.MakeStringSingleLine(), Is.EqualTo(lineOfText));
    }

    [TestCase("", "")]
    [TestCase("is", "This is test string data")]
    [TestCase("and", "Hello and How do you do?")]
    [TestCase("a", "What a wonderful world!")]
    [TestCase("me", "Take me out to the ballgame")]
    public void ReturnWordAtCursorPositionSix(string expectedWord, string fullLine)
    {
        (int start, int length) = fullLine.CursorWordBoundaries(6);
        string singleWordAtSix = fullLine.Substring(start, length);
        Assert.That(singleWordAtSix, Is.EqualTo(expectedWord));
    }

    // Explicit \r\n and trailing spaces to match WPF original exactly
    private static string multiLineInput =
        "Hello this is lots \r\nof text which has several lines\r\nand some spaces at the ends of line \r\nto throw off any easy check";

    [TestCase("Hello", "", " this ...")]
    [TestCase("lots", "Hello this is ", " ...")]
    [TestCase("of", "...", " text ...")]
    [TestCase("several", "...h has ", " lines...")]
    public void ReturnPreviewsFromWord(string firstWord, string expectedLeftPreview, string expectedRightPreview)
    {
        int length = firstWord.Length;
        int previewLength = 6;

        int cursorPosition = multiLineInput.IndexOf(firstWord);

        string PreviewLeft = StringMethods.GetCharactersToLeftOfNewLine(ref multiLineInput, cursorPosition, previewLength);
        string PreviewRight = StringMethods.GetCharactersToRightOfNewLine(ref multiLineInput, cursorPosition + length, previewLength);

        Assert.That(PreviewLeft, Is.EqualTo(expectedLeftPreview));
        Assert.That(PreviewRight, Is.EqualTo(expectedRightPreview));
    }

    [TestCase(15, "lots")]
    [TestCase(21, "of")]
    [TestCase(53, "and")]
    [TestCase(118, "check")]
    [TestCase(0, "Hello")]
    [TestCase(1000, "check")]
    [TestCase(-10, "Hello")]
    [TestCase(-1, "Hello")]
    [TestCase(10, "this")]
    public void ReturnWordAtCursorWithNewLines(int cursorPosition, string expectedWord)
    {
        string actualWord = multiLineInput.GetWordAtCursorPosition(cursorPosition);
        Assert.That(actualWord, Is.EqualTo(expectedWord));
    }

    [TestCase("", "")]
    [TestCase("Hello, world! 0123456789", "Hello, world! olz3hSb7Bg")]
    [TestCase("Foo 4r b4r", "Foo hr bhr")]
    [TestCase("B4zz5 9zzl3", "BhzzS gzzl3")]
    [TestCase("abcdefghijklmnop", "abcdefghijklmnop")]
    public void TryFixToLetters_ReplacesDigitsWithLetters_AsExpected(string input, string expected)
    {
        string result = input.TryFixToLetters();
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("", "")]
    [TestCase("he11o there", "hello there")]
    [TestCase("my number is l23456789o", "my number is 1234567890")]
    public void TryFixNumOrLetters(string input, string expected)
    {
        string result = input.TryFixEveryWordLetterNumberErrors();
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("", "")]
    [TestCase("Hello, world! 0123456789", "4e110, w0r1d! 0123456789")]
    [TestCase("Foo 4r b4r", "F00 4r 64r")]
    [TestCase("B4zzS 9zzl3", "84225 92213")]
    [TestCase("abcdefghijklmnopqrs", "a60def941jk1mn0pqr5")]
    public void TryFixToLetters_ReplacesLettersWithDigits_AsExpected(string input, string expected)
    {
        string result = input.TryFixToNumbers();
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void RemoveDuplicateLines_AsExpected()
    {
        string inputString =
            "This is a line\r\nThis is a line\r\nThis is a line\r\nThis is a line\r\n" +
            "Another Line\r\nAnother Line\r\nThis is a line";

        string expectedString = "This is a line\r\nAnother Line";

        string actualString = inputString.RemoveDuplicateLines();
        Assert.That(actualString, Is.EqualTo(expectedString));
    }

    [TestCase("", "")]
    [TestCase("A<>B<>C", "A-B-C")]
    [TestCase("abc+123/def:*", "abc-123-def-")]
    [TestCase("@TheJoeFin", "-TheJoeFin")]
    [TestCase("Hello World!", "Hello-World-")]
    [TestCase("Nothing", "Nothing")]
    [TestCase("   ", "-")]
    [TestCase("-----", "-")]
    public void ReplaceReservedCharacters(string inputString, string expectedString)
    {
        string actualString = inputString.ReplaceReservedCharacters();
        Assert.That(actualString, Is.EqualTo(expectedString));
    }

    [TestCase("", @"", 3)]
    [TestCase("Hello World!", @"[A-Za-z]{5}\s[A-Za-z]{5}!", 3)]
    [TestCase("123-555-6789", @"\d{3}-\d{3}-\d{4}", 3)]
    [TestCase("(123)-555-6789", @"(\()\d{3}(\))-\d{3}-\d{4}", 3)]
    [TestCase("Abc123456-99", @"[A-Za-z]{3}\d{6}-\d{2}", 3)]
    [TestCase("ab12ab12ab12ab12ab12", @"([A-Za-z]{2}\d{2}){5}", 3)]
    [TestCase("Abc123", @"\S+", 0)]
    [TestCase("Hello World", @"\S+", 0)]
    [TestCase("Abc123", @"\w+", 1)]
    [TestCase("Test456", @"\w+", 1)]
    [TestCase("Abc123", @"\w{3}\w{3}", 2)]
    [TestCase("Hello", @"\w{5}", 2)]
    [TestCase("Abc", @"(?i)Abc", 4)]
    [TestCase("123", @"(?i)123", 4)]
    [TestCase("Test", @"(?i)Test", 4)]
    [TestCase("Abc123", @"Abc123", 5)]
    [TestCase("Test", @"Test", 5)]
    [TestCase("Hello World!", @"Hello\ World!", 5)]
    public void ExtractSimplePatternFromEachString(string inputString, string expectedString, int precisionLevel)
    {
        string actualString = inputString.ExtractSimplePattern(precisionLevel);
        Assert.That(actualString, Is.EqualTo(expectedString));
    }

    [TestCase("", false)]
    [TestCase("test@example.com", true)]
    [TestCase("test@example.co", true)]
    [TestCase("test@example.", false)]
    [TestCase("joe@TextGrab.net", true)]
    [TestCase("joe@Text Grab.net", false)]
    public void TestIsValidEmailAddress(string inputString, bool expectedIsValid)
    {
        Assert.That(inputString.IsValidEmailAddress(), Is.EqualTo(expectedIsValid));
    }

    [Test]
    public void TestGetLineStartAndLength()
    {
        string inputString =
            "Don't Forget to do\r\nthe method just the way\r\n" +
            "The quick brown fox\r\njumped over the lazy\r\nbrown dog";

        (int start, int length) = inputString.GetStartAndLengthOfLineAtPosition(20);
        string actualString = inputString.Substring(start, length);

        string expectedString = "the method just the way\r\n";
        Assert.That(actualString, Is.EqualTo(expectedString));
    }

    [Test]
    public void TestUnstackGroups()
    {
        string inputString =
            "1\r\n2\r\n3\r\n4\r\n5\r\na\r\nb\r\nc\r\nd\r\ne\r\njan\r\nfeb\r\nmar\r\napr\r\nmay";

        string acualString = inputString.UnstackGroups(5);

        string expectedString =
            "1\ta\tjan\r\n2\tb\tfeb\r\n3\tc\tmar\r\n4\td\tapr\r\n5\te\tmay";

        Assert.That(acualString, Is.EqualTo(expectedString));
    }

    [Test]
    public void TestUnstackString()
    {
        string inputString =
            "1\r\na\r\njan\r\n2\r\nb\r\nfeb\r\n3\r\nc\r\nmar\r\n4\r\nd\r\napr\r\n5\r\ne\r\nmay";

        string acualString = inputString.UnstackStrings(3);

        string expectedString =
            "1\ta\tjan\r\n2\tb\tfeb\r\n3\tc\tmar\r\n4\td\tapr\r\n5\te\tmay";

        Assert.That(acualString, Is.EqualTo(expectedString));
    }

    [TestCase("The quick brown fox", "fox", "The quick brown ")]
    [TestCase("jumped over over the lazy", "over", "jumped   the lazy")]
    [TestCase("brown dogs and what not", "o", "brwn dgs and what nt")]
    public void TestRemoveThisString(string inputString, string remove, string expected)
    {
        Assert.That(inputString.RemoveAllInstancesOf(remove), Is.EqualTo(expected));
    }

    [TestCase("The quick brown fox", "fox brown quick The\r\n")]
    [TestCase("jumped over the lazy", "lazy the over jumped\r\n")]
    [TestCase("brown dogs and what not", "not what and dogs brown\r\n")]
    public void TestReverseString(string inputString, string expected)
    {
        StringBuilder sb = new(inputString);
        sb.ReverseWordsForRightToLeft();
        Assert.That(sb.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void TestReverseStringMultiline()
    {
        string inputString = "brown dogs\r\nand what not";
        string expected = "dogs brown\r\nnot what and\r\n";
        StringBuilder sb = new(inputString);
        sb.ReverseWordsForRightToLeft();
        Assert.That(sb.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void TestRemoveFromEachLines_Beginning()
    {
        string inputString = "hello there\r\ngeneral kenobi";
        string expected = "lo there\r\neral kenobi\r\n";
        Assert.That(inputString.RemoveFromEachLine(3, SpotInLine.Beginning), Is.EqualTo(expected));
    }

    [Test]
    public void TestRemoveFromEachLines_End_TwoLines()
    {
        string inputString = "hello there\r\ngeneral kenobi";
        string expected = "hello th\r\ngeneral ken\r\n";
        Assert.That(inputString.RemoveFromEachLine(3, SpotInLine.End), Is.EqualTo(expected));
    }

    [Test]
    public void TestRemoveFromEachLines_End_ThreeLines()
    {
        string inputString = "hello there\r\ngeneral kenobi\r\nyou are a bold one!";
        string expected = "hello th\r\ngeneral ken\r\nyou are a bold o\r\n";
        Assert.That(inputString.RemoveFromEachLine(3, SpotInLine.End), Is.EqualTo(expected));
    }

    [Test]
    public void TestRemoveFromEachLines_End_WithShortLine()
    {
        string inputString = "hello there\r\ngeneral kenobi\r\n22\r\nyou are a bold one!";
        string expected = "hello th\r\ngeneral ken\r\n\r\nyou are a bold o\r\n";
        Assert.That(inputString.RemoveFromEachLine(3, SpotInLine.End), Is.EqualTo(expected));
    }

    [Test]
    public void TestAddToEachLines_Beginning()
    {
        string inputString = "hello there\r\ngeneral kenobi";
        string expected = "Yep hello there\r\nYep general kenobi";
        Assert.That(inputString.AddCharsToEachLine("Yep ", SpotInLine.Beginning), Is.EqualTo(expected));
    }

    [Test]
    public void TestAddToEachLines_End_TwoLines()
    {
        string inputString = "hello there\r\ngeneral kenobi";
        string expected = "hello there Great\r\ngeneral kenobi Great";
        Assert.That(inputString.AddCharsToEachLine(" Great", SpotInLine.End), Is.EqualTo(expected));
    }

    [Test]
    public void TestAddToEachLines_End_ThreeLines()
    {
        string inputString = "hello there\r\ngeneral kenobi\r\nyou are a bold one!";
        string expected = "hello there Awesome\r\ngeneral kenobi Awesome\r\nyou are a bold one! Awesome";
        Assert.That(inputString.AddCharsToEachLine(" Awesome", SpotInLine.End), Is.EqualTo(expected));
    }

    [TestCase("AWESOME", CurrentCase.Upper)]
    [TestCase("awesome", CurrentCase.Lower)]
    [TestCase("Awesome", CurrentCase.Camel)]
    [TestCase("", CurrentCase.Unknown)]
    [TestCase("   ", CurrentCase.Unknown)]
    [TestCase("the case", CurrentCase.Lower)]
    [TestCase("THE CASE", CurrentCase.Upper)]
    [TestCase("The Case", CurrentCase.Camel)]
    public void TestDetermineToggleCase(string inputString, CurrentCase expectedCase)
    {
        Assert.That(StringMethods.DetermineToggleCase(inputString), Is.EqualTo(expectedCase));
    }

    [TestCase('A', true)]
    [TestCase('a', true)]
    [TestCase('b', true)]
    [TestCase('c', true)]
    [TestCase('C', true)]
    [TestCase('d', true)]
    [TestCase('z', true)]
    [TestCase('Z', true)]
    [TestCase('1', true)]
    [TestCase('4', true)]
    [TestCase('-', true)]
    [TestCase('*', true)]
    [TestCase('+', true)]
    [TestCase('%', true)]
    [TestCase('3', true)]
    [TestCase('|', true)]
    [TestCase('\r', true)]
    [TestCase('\n', true)]
    [TestCase('\t', true)]
    public void TestIsBasicLatin(char inputChar, bool isLatin)
    {
        Assert.That(inputChar.IsBasicLatin(), Is.EqualTo(isLatin));
    }

    [TestCase('\u00C0', false)] // A-grave
    [TestCase('\u00DC', false)] // U-umlaut
    [TestCase('\u00D6', false)] // O-umlaut
    [TestCase('\u00C7', false)] // C-cedilla
    public void TestIsBasicLatin_NonLatin(char inputChar, bool isLatin)
    {
        Assert.That(inputChar.IsBasicLatin(), Is.EqualTo(isLatin));
    }

    [TestCase("string to test", "string to test")]
    [TestCase("ABCDEФGHIJKLMNOПQЯSTUVWXYZ", "ABCDEOGHIJKLMNOnQRSTUVWXYZ")]
    [TestCase("HЭllΘ There! @$2890", "H3llO There! @$2890")]
    [TestCase("", "")]
    public void TestReplaceGreekAndCyrillic(string inputString, string expectedString)
    {
        Assert.That(inputString.ReplaceGreekOrCyrillicWithLatin(), Is.EqualTo(expectedString));
    }

    [Test]
    public void TestLimitEachLine_Beginning_10()
    {
        string inputString = "hello there\r\ngeneral kenobi";
        string expected = "hello ther\r\ngeneral ke";
        Assert.That(inputString.LimitCharactersPerLine(10, SpotInLine.Beginning), Is.EqualTo(expected));
    }

    [Test]
    public void TestLimitEachLine_End_12()
    {
        string inputString = "hello there\r\ngeneral kenobi";
        string expected = "hello there\r\nneral kenobi";
        Assert.That(inputString.LimitCharactersPerLine(12, SpotInLine.End), Is.EqualTo(expected));
    }

    [Test]
    public void TestLimitEachLine_Beginning_100_NoChange()
    {
        string inputString = "hello there\r\ngeneral kenobi";
        string expected = "hello there\r\ngeneral kenobi";
        Assert.That(inputString.LimitCharactersPerLine(100, SpotInLine.Beginning), Is.EqualTo(expected));
    }

    [Test]
    public void TestLimitEachLine_End_100_NoChange()
    {
        string inputString = "hello there\r\ngeneral kenobi";
        string expected = "hello there\r\ngeneral kenobi";
        Assert.That(inputString.LimitCharactersPerLine(100, SpotInLine.End), Is.EqualTo(expected));
    }

    [Test]
    public void TestLimitEachLine_Beginning_5_ThreeLines()
    {
        string inputString = "hello there\r\ngeneral kenobi\r\nyou are a bold one!";
        string expected = "hello\r\ngener\r\nyou a";
        Assert.That(inputString.LimitCharactersPerLine(5, SpotInLine.Beginning), Is.EqualTo(expected));
    }

    [Test]
    public void TestLimitEachLine_Beginning_0_ReturnsEmpty()
    {
        string inputString = "hello there\r\ngeneral kenobi\r\nyou are a bold one!";
        string expected = "";
        Assert.That(inputString.LimitCharactersPerLine(0, SpotInLine.Beginning), Is.EqualTo(expected));
    }

    [Test]
    public void TestLimitEachLine_End_0_ReturnsEmpty()
    {
        string inputString = "hello there\r\ngeneral kenobi\r\nyou are a bold one!";
        string expected = "";
        Assert.That(inputString.LimitCharactersPerLine(0, SpotInLine.End), Is.EqualTo(expected));
    }

    [TestCase("g7a56312-d8e8-4ca5-87fa-18e3S266d3le", "97a56312-d8e8-4ca5-87fa-18e35266d31e")]
    [TestCase("g7a56312-d8e 8-4ca5-87fa-18e3S2 66d3le", "97a56312-d8e8-4ca5-87fa-18e35266d31e")]
    [TestCase("g7a56312-\r\nd8e8\r\n-4ca5-87fa-18e3S266d3le", "97a56312-d8e8-4ca5-87fa-18e35266d31e")]
    public void TestGuidCorrections(string input, string expected)
    {
        Assert.That(input.CorrectCommonGuidErrors(), Is.EqualTo(expected));
    }
}
