using System.Globalization;
using System.Text.RegularExpressions;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Services;

public sealed class DocumentFieldComparisonService
{
    public List<FieldComparisonResult> Compare(
        DocumentReadResult document,
        StudentProfileForm form)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(form);

        List<FieldComparisonResult> results = CompareDocumentFields(
            ToProfile(document), ToProfile(form));
        results.Add(CompareDate("InternshipStartDate", document.InternshipStartDate, form.InternshipStartDate));
        results.Add(CompareDate("InternshipEndDate", document.InternshipEndDate, form.InternshipEndDate));
        return results;
    }

    public List<FieldComparisonResult> CompareWithReference(
        DocumentReadResult document,
        TestStudent reference)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reference);
        return CompareReferenceFields(ToProfile(document), ToProfile(reference));
    }

    public List<FieldComparisonResult> CompareStudentWithReference(
        Student student,
        TestStudent reference)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(reference);
        return CompareReferenceFields(ToProfile(student), ToProfile(reference));
    }

    private static List<FieldComparisonResult> CompareDocumentFields(
        ComparisonProfile left,
        ComparisonProfile right) =>
    [
        CompareText("FullName", left.FullName, right.FullName),
        CompareDigits("IdentityNumber", left.IdentityNumber, right.IdentityNumber),
        CompareNationality("Nationality", left.Nationality, right.Nationality),
        CompareDate("BirthDate", left.BirthDate, right.BirthDate),
        CompareText("StudentNumber", left.StudentNumber, right.StudentNumber),
        CompareText("Department", left.Department, right.Department),
        CompareText("Address", left.Address, right.Address),
        CompareText("AcademicYear", left.AcademicYear, right.AcademicYear),
        CompareNumber("PreviousInternshipDays", left.PreviousInternshipDays, right.PreviousInternshipDays),
        CompareEmail("Email", left.Email, right.Email),
        ComparePhone("PhoneNumber", left.PhoneNumber, right.PhoneNumber),
        CompareNumber("HealthAidStatus", left.HealthAidStatus, right.HealthAidStatus)
    ];

    private static List<FieldComparisonResult> CompareReferenceFields(
        ComparisonProfile left,
        ComparisonProfile right) =>
    [
        CompareText("FullName", left.FullName, right.FullName),
        CompareDigits("IdentityNumber", left.IdentityNumber, right.IdentityNumber),
        CompareNationality("Nationality", left.Nationality, right.Nationality),
        CompareDate("BirthDate", left.BirthDate, right.BirthDate),
        CompareText("StudentNumber", left.StudentNumber, right.StudentNumber),
        CompareText("Address", left.Address, right.Address),
        CompareText("Department", left.Department, right.Department),
        CompareText("AcademicYear", left.AcademicYear, right.AcademicYear),
        CompareNumber("PreviousInternshipDays", left.PreviousInternshipDays, right.PreviousInternshipDays),
        CompareEmail("Email", left.Email, right.Email),
        ComparePhone("PhoneNumber", left.PhoneNumber, right.PhoneNumber),
        CompareNumber("HealthAidStatus", left.HealthAidStatus, right.HealthAidStatus)
    ];

    private static ComparisonProfile ToProfile(DocumentReadResult value) => new(
        value.FullName, value.IdentityNumber, value.Nationality, value.BirthDate,
        value.StudentNumber, value.Address, value.Department, value.AcademicYear,
        value.PreviousInternshipDays, value.Email, value.PhoneNumber, value.HealthAidStatus);

    private static ComparisonProfile ToProfile(StudentProfileForm value) => new(
        value.FullName, value.IdentityNumber, value.Nationality, value.BirthDate,
        value.StudentNumber, value.Address, value.Department, value.AcademicYear,
        value.PreviousInternshipDays, value.Email, value.PhoneNumber, value.HealthAidStatus);

    private static ComparisonProfile ToProfile(Student value) => new(
        value.FullName, value.IdentityNumber, value.Nationality, value.BirthDate,
        value.StudentNumber, value.Address, value.Department, value.AcademicYear,
        value.PreviousInternshipDays, value.Email, value.PhoneNumber, value.HealthAidStatus);

    private static ComparisonProfile ToProfile(TestStudent value) => new(
        value.FullName, value.IdentityNumber, value.Nationality, value.BirthDate,
        value.StudentNumber, value.Address, value.Department, value.AcademicYear,
        value.PreviousInternshipDays, value.Email, value.PhoneNumber, value.HealthAidStatus);

    private static FieldComparisonResult CompareText(string field, string? left, string? right) =>
        CompareString(field, left, right, NormalizeText);

    private static FieldComparisonResult CompareDigits(string field, string? left, string? right) =>
        CompareString(field, left, right, NormalizeDigits);

    private static FieldComparisonResult CompareNationality(string field, string? left, string? right) =>
        CompareString(field, left, right, NormalizeNationality);

    private static FieldComparisonResult CompareEmail(string field, string? left, string? right) =>
        CompareString(field, left, right, NormalizeEmail);

    private static FieldComparisonResult ComparePhone(string field, string? left, string? right) =>
        CompareString(field, left, right, NormalizePhone);

    private static FieldComparisonResult CompareString(
        string field,
        string? left,
        string? right,
        Func<string?, string> normalize) =>
        CompareValues(field, left, right, normalize(left), normalize(right));

    private static FieldComparisonResult CompareDate(
        string field,
        DateTime? left,
        DateTime? right)
    {
        string? leftText = left?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        string? rightText = right.HasValue && right.Value != DateTime.MinValue
            ? right.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : null;

        return CompareValues(field, leftText, rightText,
            NormalizeText(leftText), NormalizeText(rightText));
    }

    private static FieldComparisonResult CompareNumber(
        string field,
        int? left,
        int? right)
    {
        string? leftText = left?.ToString(CultureInfo.InvariantCulture);
        string? rightText = right?.ToString(CultureInfo.InvariantCulture);

        return CompareValues(field, leftText, rightText,
            NormalizeNumber(leftText), NormalizeNumber(rightText));
    }

    private static FieldComparisonResult CompareValues(
        string field,
        string? left,
        string? right,
        string normalizedLeft,
        string normalizedRight)
    {
        string status = string.IsNullOrWhiteSpace(left)
            ? FieldComparisonStatus.Unreadable
            : string.IsNullOrWhiteSpace(right)
                ? FieldComparisonStatus.FormValueMissing
                : normalizedLeft == normalizedRight
                    ? FieldComparisonStatus.Match
                    : FieldComparisonStatus.Mismatch;

        return new FieldComparisonResult
        {
            FieldName = field,
            DocumentValue = left,
            FormValue = right,
            Status = status
        };
    }

    private static string NormalizeText(string? value) =>
        Regex.Replace(value ?? "", @"\s+", " ")
            .Trim()
            .ToUpper(CultureInfo.GetCultureInfo("tr-TR"));

    private static string NormalizeDigits(string? value) =>
        Regex.Replace(value ?? "", @"\D", "");

    private static string NormalizeEmail(string? value) =>
        (value ?? "").Trim().ToUpperInvariant();

    private static string NormalizeNationality(string? value)
    {
        string compact = Regex.Replace(NormalizeText(value), @"[^\p{L}\p{N}]", "");

        return compact switch
        {
            "TC" or "TÜRKİYECUMHURİYETİ" => "TC",
            "FOREIGN" or "FOREİGN" or "YABANCI" or "YABANCIUYRUKLU" => "FOREIGN",
            _ => compact
        };
    }

    private static string NormalizePhone(string? value)
    {
        string digits = NormalizeDigits(value);

        return digits.Length switch
        {
            12 when digits.StartsWith("90") => digits[2..],
            11 when digits.StartsWith("0") => digits[1..],
            _ => digits
        };
    }

    private static string NormalizeNumber(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
            ? number.ToString(CultureInfo.InvariantCulture)
            : "";

    private sealed record ComparisonProfile(
        string? FullName,
        string? IdentityNumber,
        string? Nationality,
        DateTime? BirthDate,
        string? StudentNumber,
        string? Address,
        string? Department,
        string? AcademicYear,
        int? PreviousInternshipDays,
        string? Email,
        string? PhoneNumber,
        int? HealthAidStatus);
}
