using System.Globalization;
using System.Text.RegularExpressions;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Services;

public sealed class DocumentFieldComparisonService
{
    // Belgeden okunan bilgileri öğrenci formuyla karşılaştırma
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

    // Belge bilgilerini test veri setindeki kayıtla karşılaştırma
    public List<FieldComparisonResult> CompareWithReference(
        DocumentReadResult document,
        TestStudent reference)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reference);
        return CompareReferenceFields(ToProfile(document), ToProfile(reference));
    }

    // Öğrenci bilgilerini test veri setindeki kayıtla karşılaştırma
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
        ComparisonProfile right)
    {
        return new List<FieldComparisonResult>
        {
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
        };
    }

    private static List<FieldComparisonResult> CompareReferenceFields(
        ComparisonProfile left,
        ComparisonProfile right)
    {
        return new List<FieldComparisonResult>
        {
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
        };
    }

    // Farklı modellerdeki ortak alanları karşılaştırma için hazırlama
    private static ComparisonProfile ToProfile(DocumentReadResult value)
    {
        return new ComparisonProfile
        {
            FullName = value.FullName,
            IdentityNumber = value.IdentityNumber,
            Nationality = value.Nationality,
            BirthDate = value.BirthDate,
            StudentNumber = value.StudentNumber,
            Address = value.Address,
            Department = value.Department,
            AcademicYear = value.AcademicYear,
            PreviousInternshipDays = value.PreviousInternshipDays,
            Email = value.Email,
            PhoneNumber = value.PhoneNumber,
            HealthAidStatus = value.HealthAidStatus
        };
    }

    private static ComparisonProfile ToProfile(StudentProfileForm value)
    {
        return new ComparisonProfile
        {
            FullName = value.FullName,
            IdentityNumber = value.IdentityNumber,
            Nationality = value.Nationality,
            BirthDate = value.BirthDate,
            StudentNumber = value.StudentNumber,
            Address = value.Address,
            Department = value.Department,
            AcademicYear = value.AcademicYear,
            PreviousInternshipDays = value.PreviousInternshipDays,
            Email = value.Email,
            PhoneNumber = value.PhoneNumber,
            HealthAidStatus = value.HealthAidStatus
        };
    }

    private static ComparisonProfile ToProfile(Student value)
    {
        return new ComparisonProfile
        {
            FullName = value.FullName,
            IdentityNumber = value.IdentityNumber,
            Nationality = value.Nationality,
            BirthDate = value.BirthDate,
            StudentNumber = value.StudentNumber,
            Address = value.Address,
            Department = value.Department,
            AcademicYear = value.AcademicYear,
            PreviousInternshipDays = value.PreviousInternshipDays,
            Email = value.Email,
            PhoneNumber = value.PhoneNumber,
            HealthAidStatus = value.HealthAidStatus
        };
    }

    private static ComparisonProfile ToProfile(TestStudent value)
    {
        return new ComparisonProfile
        {
            FullName = value.FullName,
            IdentityNumber = value.IdentityNumber,
            Nationality = value.Nationality,
            BirthDate = value.BirthDate,
            StudentNumber = value.StudentNumber,
            Address = value.Address,
            Department = value.Department,
            AcademicYear = value.AcademicYear,
            PreviousInternshipDays = value.PreviousInternshipDays,
            Email = value.Email,
            PhoneNumber = value.PhoneNumber,
            HealthAidStatus = value.HealthAidStatus
        };
    }

    private static FieldComparisonResult CompareText(string field, string? left, string? right)
    {
        return CompareString(field, left, right, NormalizeText);
    }

    private static FieldComparisonResult CompareDigits(string field, string? left, string? right)
    {
        return CompareString(field, left, right, NormalizeDigits);
    }

    private static FieldComparisonResult CompareNationality(string field, string? left, string? right)
    {
        return CompareString(field, left, right, NormalizeNationality);
    }

    private static FieldComparisonResult CompareEmail(string field, string? left, string? right)
    {
        return CompareString(field, left, right, NormalizeEmail);
    }

    private static FieldComparisonResult ComparePhone(string field, string? left, string? right)
    {
        return CompareString(field, left, right, NormalizePhone);
    }

    private static FieldComparisonResult CompareString(
        string field,
        string? left,
        string? right,
        Func<string?, string> normalize)
    {
        return CompareValues(field, left, right, normalize(left), normalize(right));
    }

    private static FieldComparisonResult CompareDate(
        string field,
        DateTime? left,
        DateTime? right)
    {
        string? leftText = left?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        string? rightText = null;
        if (right.HasValue && right.Value != DateTime.MinValue)
        {
            rightText = right.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

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

    // Eksik veya eşleşmeyen alanın sonucunu belirleme
    private static FieldComparisonResult CompareValues(
        string field,
        string? left,
        string? right,
        string normalizedLeft,
        string normalizedRight)
    {
        string status;
        if (string.IsNullOrWhiteSpace(left))
        {
            status = FieldComparisonStatus.Unreadable;
        }
        else if (string.IsNullOrWhiteSpace(right))
        {
            status = FieldComparisonStatus.FormValueMissing;
        }
        else if (normalizedLeft == normalizedRight)
        {
            status = FieldComparisonStatus.Match;
        }
        else
        {
            status = FieldComparisonStatus.Mismatch;
        }

        return new FieldComparisonResult
        {
            FieldName = field,
            DocumentValue = left,
            FormValue = right,
            Status = status
        };
    }

    private static string NormalizeText(string? value)
    {
        return Regex.Replace(value ?? "", @"\s+", " ")
            .Trim()
            .ToUpper(CultureInfo.GetCultureInfo("tr-TR"));
    }

    private static string NormalizeDigits(string? value)
    {
        return Regex.Replace(value ?? "", @"\D", "");
    }

    private static string NormalizeEmail(string? value)
    {
        return (value ?? "").Trim().ToUpperInvariant();
    }

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

    // Telefon numaralarını karşılaştırma için aynı biçime getirme
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

    private static string NormalizeNumber(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        return "";
    }

    // Karşılaştırılacak ortak öğrenci bilgilerini tutma
    private class ComparisonProfile
    {
        public string? FullName { get; set; }
        public string? IdentityNumber { get; set; }
        public string? Nationality { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? StudentNumber { get; set; }
        public string? Address { get; set; }
        public string? Department { get; set; }
        public string? AcademicYear { get; set; }
        public int? PreviousInternshipDays { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int? HealthAidStatus { get; set; }
    }
}
