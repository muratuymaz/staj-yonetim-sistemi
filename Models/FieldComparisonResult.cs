namespace StajYonetimSistemi.Models;

public static class FieldComparisonStatus
{
    public const string Match = "match";
    public const string Mismatch = "mismatch";
    public const string Unreadable = "unreadable";
    public const string FormValueMissing = "formValueMissing";
}

public sealed class FieldComparisonResult
{
    public string FieldName { get; init; } = "";

    public string? DocumentValue { get; init; }

    public string? FormValue { get; init; }

    public string Status { get; init; } = FieldComparisonStatus.Unreadable;
}
