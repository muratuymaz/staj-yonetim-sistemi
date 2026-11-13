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

        List<FieldComparisonResult> results = new List<FieldComparisonResult>();

        results.Add(CompareText("FullName", document.FullName, form.FullName));
        results.Add(CompareDigits("IdentityNumber", document.IdentityNumber, form.IdentityNumber));
        results.Add(CompareNationality("Nationality", document.Nationality, form.Nationality));
        results.Add(CompareDate("BirthDate", document.BirthDate, form.BirthDate));
        results.Add(CompareText("StudentNumber", document.StudentNumber, form.StudentNumber));
        results.Add(CompareText("Department", document.Department, form.Department));
        results.Add(CompareText("Address", document.Address, form.Address));
        results.Add(CompareText("AcademicYear", document.AcademicYear, form.AcademicYear));
        results.Add(CompareNumber("PreviousInternshipDays", document.PreviousInternshipDays, form.PreviousInternshipDays));
        results.Add(CompareEmail("Email", document.Email, form.Email));
        results.Add(ComparePhone("PhoneNumber", document.PhoneNumber, form.PhoneNumber));
        results.Add(CompareNumber("HealthAidStatus", document.HealthAidStatus, form.HealthAidStatus));
        results.Add(CompareDate("InternshipStartDate", document.InternshipStartDate, form.InternshipStartDate));
        results.Add(CompareDate("InternshipEndDate", document.InternshipEndDate, form.InternshipEndDate));

        return results;
    }

    // Belgeden okunan bilgileri CSV referansıyla karşılaştırma
    public List<FieldComparisonResult> CompareWithReference(
        DocumentReadResult document,
        TestStudent reference)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reference);

        List<FieldComparisonResult> results = new List<FieldComparisonResult>();

        results.Add(CompareText("FullName", document.FullName, reference.FullName));
        results.Add(CompareDigits("IdentityNumber", document.IdentityNumber, reference.IdentityNumber));
        results.Add(CompareNationality("Nationality", document.Nationality, reference.Nationality));
        results.Add(CompareDate("BirthDate", document.BirthDate, reference.BirthDate));
        results.Add(CompareText("StudentNumber", document.StudentNumber, reference.StudentNumber));
        results.Add(CompareText("Address", document.Address, reference.Address));
        results.Add(CompareText("Department", document.Department, reference.Department));
        results.Add(CompareText("AcademicYear", document.AcademicYear, reference.AcademicYear));
        results.Add(CompareNumber("PreviousInternshipDays", document.PreviousInternshipDays, reference.PreviousInternshipDays));
        results.Add(CompareEmail("Email", document.Email, reference.Email));
        results.Add(ComparePhone("PhoneNumber", document.PhoneNumber, reference.PhoneNumber));
        results.Add(CompareNumber("HealthAidStatus", document.HealthAidStatus, reference.HealthAidStatus));

        return results;
    }

    // Kaydedilen öğrenci profili ile CSV referansındaki alanları karşılaştırma
    public List<FieldComparisonResult> CompareStudentWithReference(
        Student student,
        TestStudent reference)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(reference);

        List<FieldComparisonResult> results = new List<FieldComparisonResult>();

        results.Add(CompareText("FullName", student.FullName, reference.FullName));
        results.Add(CompareDigits("IdentityNumber", student.IdentityNumber, reference.IdentityNumber));
        results.Add(CompareNationality("Nationality", student.Nationality, reference.Nationality));
        results.Add(CompareDate("BirthDate", student.BirthDate, reference.BirthDate));
        results.Add(CompareText("StudentNumber", student.StudentNumber, reference.StudentNumber));
        results.Add(CompareText("Address", student.Address, reference.Address));
        results.Add(CompareText("Department", student.Department, reference.Department));
        results.Add(CompareText("AcademicYear", student.AcademicYear, reference.AcademicYear));
        results.Add(CompareNumber("PreviousInternshipDays", student.PreviousInternshipDays, reference.PreviousInternshipDays));
        results.Add(CompareEmail("Email", student.Email, reference.Email));
        results.Add(ComparePhone("PhoneNumber", student.PhoneNumber, reference.PhoneNumber));
        results.Add(CompareNumber("HealthAidStatus", student.HealthAidStatus, reference.HealthAidStatus));

        return results;
    }

    private static FieldComparisonResult CompareText(
        string fieldName,
        string? documentValue,
        string? formValue)
    {
        string firstValue = NormalizeText(documentValue);
        string secondValue = NormalizeText(formValue);
        return CompareValues(fieldName, documentValue, formValue, firstValue, secondValue);
    }

    private static FieldComparisonResult CompareDigits(
        string fieldName,
        string? documentValue,
        string? formValue)
    {
        string firstValue = NormalizeDigits(documentValue);
        string secondValue = NormalizeDigits(formValue);
        return CompareValues(fieldName, documentValue, formValue, firstValue, secondValue);
    }

    private static FieldComparisonResult CompareNationality(
        string fieldName,
        string? documentValue,
        string? referenceValue)
    {
        string firstValue = NormalizeNationality(documentValue);
        string secondValue = NormalizeNationality(referenceValue);
        return CompareValues(fieldName, documentValue, referenceValue, firstValue, secondValue);
    }

    private static FieldComparisonResult CompareEmail(
        string fieldName,
        string? documentValue,
        string? formValue)
    {
        string firstValue = NormalizeEmail(documentValue);
        string secondValue = NormalizeEmail(formValue);
        return CompareValues(fieldName, documentValue, formValue, firstValue, secondValue);
    }

    private static FieldComparisonResult ComparePhone(
        string fieldName,
        string? documentValue,
        string? formValue)
    {
        string firstValue = NormalizePhone(documentValue);
        string secondValue = NormalizePhone(formValue);
        return CompareValues(fieldName, documentValue, formValue, firstValue, secondValue);
    }

    private static FieldComparisonResult CompareDate(
        string fieldName,
        DateTime? documentValue,
        DateTime formValue)
    {
        string? documentText = null;
        string? formText = null;

        if (documentValue.HasValue)
        {
            documentText = documentValue.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        if (formValue != DateTime.MinValue)
        {
            formText = formValue.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        return CompareValues(fieldName, documentText, formText,
            NormalizeText(documentText), NormalizeText(formText));
    }

    private static FieldComparisonResult CompareNumber(
        string fieldName,
        int? documentValue,
        int formValue)
    {
        string? documentText = null;
        if (documentValue.HasValue)
        {
            documentText = documentValue.Value.ToString(CultureInfo.InvariantCulture);
        }

        string formText = formValue.ToString(CultureInfo.InvariantCulture);
        return CompareValues(fieldName, documentText, formText,
            NormalizeNumber(documentText), NormalizeNumber(formText));
    }

    // Boş alanları ve düzenlenen değerlerin eşleşmesini kontrol etme
    private static FieldComparisonResult CompareValues(
        string fieldName,
        string? documentValue,
        string? formValue,
        string firstValue,
        string secondValue)
    {
        if (string.IsNullOrWhiteSpace(documentValue))
        {
            return CreateResult(
                fieldName,
                documentValue,
                formValue,
                FieldComparisonStatus.Unreadable);
        }

        if (string.IsNullOrWhiteSpace(formValue))
        {
            return CreateResult(
                fieldName,
                documentValue,
                formValue,
                FieldComparisonStatus.FormValueMissing);
        }

        string status;
        if (firstValue == secondValue)
        {
            status = FieldComparisonStatus.Match;
        }
        else
        {
            status = FieldComparisonStatus.Mismatch;
        }

        return CreateResult(fieldName, documentValue, formValue, status);
    }

    // Alanın karşılaştırma sonucunu oluşturma
    private static FieldComparisonResult CreateResult(
        string fieldName,
        string? documentValue,
        string? formValue,
        string status)
    {
        return new FieldComparisonResult
        {
            FieldName = fieldName,
            DocumentValue = documentValue,
            FormValue = formValue,
            Status = status
        };
    }

    // Metinlerde boşluk ve Türkçe harf büyüklüğü farklarını giderme
    private static string NormalizeText(string? value)
    {
        return Regex.Replace(value ?? "", @"\s+", " ")
            .Trim()
            .ToUpper(CultureInfo.GetCultureInfo("tr-TR"));
    }

    // Kimlik numarası biçimindeki boşluk ve ayraçları yok sayar.
    private static string NormalizeDigits(string? value)
    {
        return Regex.Replace(value ?? "", @"\D", "");
    }

    // E-posta adresini karşılaştırmaya hazırlama
    private static string NormalizeEmail(string? value)
    {
        return (value ?? "").Trim().ToUpperInvariant();
    }

    // Aynı uyruk ifadelerini ortak değere çevirme
    private static string NormalizeNationality(string? value)
    {
        string compact = Regex.Replace(
            NormalizeText(value),
            @"[^\p{L}\p{N}]",
            "");

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

        if (digits.Length == 12 && digits.StartsWith("90"))
            return digits[2..];

        if (digits.Length == 11 && digits.StartsWith("0"))
            return digits[1..];

        return digits;
    }

    private static string NormalizeNumber(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? "";
    }

    private static string NormalizeNumber(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
        {
            return NormalizeNumber(number);
        }

        return "";
    }
}
