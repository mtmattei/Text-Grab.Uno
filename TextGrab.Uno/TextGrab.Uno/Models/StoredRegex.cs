namespace TextGrab.Models;

public class StoredRegex
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;

    public string Pattern { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset? LastUsedDate { get; set; }

    public StoredRegex()
    {
    }

    public StoredRegex(string name, string pattern, bool isDefault = false, string description = "")
    {
        Name = name;
        Pattern = pattern;
        IsDefault = isDefault;
        Description = description;
    }

    public static StoredRegex[] GetDefaultPatterns()
    {
        return
        [
            new StoredRegex("Email Address", @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", true, "Matches email addresses"),
            new StoredRegex("Phone Number (US)", @"\b\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b", true, "Matches US phone numbers like (123) 456-7890"),
            new StoredRegex("URL", @"https?://[^\s/$.?#].[^\s]*", true, "Matches http and https URLs"),
            new StoredRegex("IP Address (IPv4)", @"\b(?:\d{1,3}\.){3}\d{1,3}\b", true, "Matches IPv4 addresses like 192.168.1.1"),
            new StoredRegex("GUID/UUID", @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", true, "Matches GUIDs/UUIDs"),
            new StoredRegex("Date (MM/DD/YYYY)", @"\b(0?[1-9]|1[0-2])/(0?[1-9]|[12][0-9]|3[01])/\d{4}\b", true, "Matches dates in MM/DD/YYYY format"),
            new StoredRegex("Date (YYYY-MM-DD)", @"\b\d{4}-(0?[1-9]|1[0-2])-(0?[1-9]|[12][0-9]|3[01])\b", true, "Matches dates in ISO format YYYY-MM-DD"),
            new StoredRegex("Time (HH:MM)", @"\b([01]?[0-9]|2[0-3]):[0-5][0-9]\b", true, "Matches time in 24-hour format"),
            new StoredRegex("Credit Card", @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", true, "Matches credit card numbers"),
            new StoredRegex("Hex Color", @"#[0-9a-fA-F]{6}\b", true, "Matches hex color codes like #FFFFFF"),
            new StoredRegex("Social Security Number", @"\b\d{3}-\d{2}-\d{4}\b", true, "Matches SSN format XXX-XX-XXXX"),
            new StoredRegex("Zip Code (US)", @"\b\d{5}(-\d{4})?\b", true, "Matches US zip codes (5 or 9 digit)"),
            new StoredRegex("Currency (USD)", @"\$\s?\d+(?:,\d{3})*(?:\.\d{2})?\b", true, "Matches US dollar amounts"),
            new StoredRegex("Integer Number", @"\b-?\d+\b", true, "Matches integer numbers"),
            new StoredRegex("Decimal Number", @"\b-?\d+\.\d+\b", true, "Matches decimal numbers"),
        ];
    }
}
