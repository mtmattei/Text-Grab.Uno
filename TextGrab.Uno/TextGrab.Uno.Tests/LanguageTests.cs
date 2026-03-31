using System.Globalization;

namespace TextGrab.Uno.Tests;

public class LanguageTests
{
    [TestCase("zh-Hant")]
    [TestCase("zh-Hans")]
    public void CanParseEveryLanguageTag(string langTag)
    {
        CultureInfo culture = new(langTag);
        Assert.That(culture, Is.Not.Null);
    }

    [TestCase("chi_sim", "Chinese (Simplified)")]
    [TestCase("chi_tra", "Chinese (Traditional)")]
    [TestCase("chi_sim_vert", "Chinese (Simplified) Vertical")]
    [TestCase("chi_tra_vert", "Chinese (Traditional) Vertical")]
    public void CanParseChineseLanguageTag(string langTag, string expectedDisplayName)
    {
        TessLang tessLang = new(langTag);
        Assert.That(tessLang.CultureDisplayName, Is.EqualTo(expectedDisplayName));
    }

    [TestCase("en-US")]
    [TestCase("es-ES")]
    [TestCase("fr-FR")]
    [TestCase("it-IT")]
    [TestCase("ro-RO")]
    [TestCase("pt-BR")]
    public void IsLatinBased_WithLatinLanguages_ReturnsTrue(string languageTag)
    {
        // Arrange
        GlobalLang language = new(languageTag);
        TessLang tessLang = new(languageTag);

        // Act
        bool result = language.IsLatinBased();
        bool tessResult = tessLang.IsLatinBased();

        // Assert
        Assert.That(result, Is.True);
        Assert.That(tessResult, Is.True);
    }

    [TestCase("zh-CN")]
    [TestCase("ja-JP")]
    [TestCase("ar-SA")]
    [TestCase("ru-RU")]
    [TestCase("hi-IN")]
    public void IsLatinBased_WithNonLatinLanguages_ReturnsFalse(string languageTag)
    {
        // Arrange
        GlobalLang language = new(languageTag);

        // Act
        bool result = language.IsLatinBased();

        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase("en-GB")]
    [TestCase("en-CA")]
    [TestCase("es-MX")]
    [TestCase("fr-CA")]
    [TestCase("pt-PT")]
    public void IsLatinBased_WithLatinLanguageVariants_ReturnsTrue(string languageTag)
    {
        // Arrange
        GlobalLang language = new(languageTag);

        // Act
        bool result = language.IsLatinBased();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLatinBased_WithMixedCaseLanguageTag_WorksCorrectly()
    {
        // Arrange
        GlobalLang language = new("En-us");

        // Act
        bool result = language.IsLatinBased();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLatinBased_WithWindowsAiLang_ReturnsFalse()
    {
        // Arrange
        WindowsAiLang windowsAiLang = new();
        // Act
        bool result = windowsAiLang.IsLatinBased();
        // Assert
        Assert.That(result, Is.False);
    }
}
