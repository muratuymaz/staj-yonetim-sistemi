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

    // Belge ve formdaki ortak alanları karşılaştırma
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

    // Ortak alanları referans öğrenci bilgileriyle karşılaştırma
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

    // Metin değerlerini düzenleyip karşılaştırma
    private static FieldComparisonResult CompareText(string field, string? left, string? right)
    {
        string firstValue = NormalizeText(left);
        string secondValue = NormalizeText(right);
        return CompareValues(field, left, right, firstValue, secondValue);
    }

    // Kimlik numaralarındaki rakamları karşılaştırma
    private static FieldComparisonResult CompareDigits(string field, string? left, string? right)
    {
        string firstValue = NormalizeDigits(left);
        string secondValue = NormalizeDigits(right);
        return CompareValues(field, left, right, firstValue, secondValue);
    }

    // Uyruk bilgilerini ortak biçime getirip karşılaştırma
    private static FieldComparisonResult CompareNationality(string field, string? left, string? right)
    {
        string firstValue = NormalizeNationality(left);
        string secondValue = NormalizeNationality(right);
        return CompareValues(field, left, right, firstValue, secondValue);
    }

    // E-posta adreslerini karşılaştırma
    private static FieldComparisonResult CompareEmail(string field, string? left, string? right)
    {
        string firstValue = NormalizeEmail(left);
        string secondValue = NormalizeEmail(right);
        return CompareValues(field, left, right, firstValue, secondValue);
    }

    // Telefon numaralarını karşılaştırma
    private static FieldComparisonResult ComparePhone(string field, string? left, string? right)
    {
        string firstValue = NormalizePhone(left);
        string secondValue = NormalizePhone(right);
        return CompareValues(field, left, right, firstValue, secondValue);
    }

    // Tarihleri aynı biçime getirip karşılaştırma
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

    // Sayısal değerleri karşılaştırma
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

    // Metindeki boşluk ve büyük küçük harf farklarını düzenleme
    private static string NormalizeText(string? value)
    {
        return Regex.Replace(value ?? "", @"\s+", " ")
            .Trim()
            .ToUpper(CultureInfo.GetCultureInfo("tr-TR"));
    }

    // Rakam dışındaki karakterleri kaldırma
    private static string NormalizeDigits(string? value)
    {
        return Regex.Replace(value ?? "", @"\D", "");
    }

    // E-posta adresini karşılaştırmaya hazırlama
    private static string NormalizeEmail(string? value)
    {
        return (value ?? "").Trim().ToUpperInvariant();
    }

    // Aynı uyruk ifadelerini ortak bir değere çevirme
    private static string NormalizeNationality(string? value)
    {
        string compact = Regex.Replace(NormalizeText(value), @"[^\p{L}\p{N}]", "");

        if (compact == "TC" || compact == "TÜRKİYECUMHURİYETİ")
        {
            return "TC";
        }

        if (compact == "FOREIGN" || compact == "FOREİGN" ||
            compact == "YABANCI" || compact == "YABANCIUYRUKLU")
        {
            return "FOREIGN";
        }

        return compact;
    }

    // Telefon numaralarını karşılaştırma için aynı biçime getirme
    private static string NormalizePhone(string? value)
    {
        string digits = NormalizeDigits(value);

        if (digits.Length == 12 && digits.StartsWith("90"))
        {
            return digits[2..];
        }

        if (digits.Length == 11 && digits.StartsWith("0"))
        {
            return digits[1..];
        }

        return digits;
    }

    // Sayıyı karşılaştırmaya hazırlama
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
