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

        return
        [
            CompareText("FullName", document.FullName, form.FullName),
            CompareDigits("IdentityNumber", document.IdentityNumber, form.IdentityNumber),
            CompareText("Nationality", document.Nationality, form.Nationality),
            CompareDate("BirthDate", document.BirthDate, form.BirthDate),
            CompareText("StudentNumber", document.StudentNumber, form.StudentNumber),
            CompareText("Department", document.Department, form.Department),
            CompareText("Address", document.Address, form.Address),
            CompareText("AcademicYear", document.AcademicYear, form.AcademicYear),
            CompareNumber("PreviousInternshipDays",
                document.PreviousInternshipDays, form.PreviousInternshipDays),
            CompareEmail("Email", document.Email, form.Email),
            ComparePhone("PhoneNumber", document.PhoneNumber, form.PhoneNumber),
            CompareDate("InternshipStartDate",
                document.InternshipStartDate, form.InternshipStartDate),
            CompareDate("InternshipEndDate",
                document.InternshipEndDate, form.InternshipEndDate)
        ];
    }

    public List<FieldComparisonResult> CompareWithReference(
        DocumentReadResult document,
        TestStudent reference)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reference);

        return
        [
            CompareText("FullName", document.FullName, reference.FullName),
            CompareDigits("IdentityNumber", document.IdentityNumber, reference.IdentityNumber),
            CompareNationality("Nationality", document.Nationality, reference.Nationality),
            CompareDate("BirthDate", document.BirthDate, reference.BirthDate),
            CompareText("StudentNumber", document.StudentNumber, reference.StudentNumber),
            CompareText("Address", document.Address, reference.Address),
            CompareText("Department", document.Department, reference.Department),
            CompareText("AcademicYear", document.AcademicYear, reference.AcademicYear),
            CompareNumber(
                "PreviousInternshipDays",
                document.PreviousInternshipDays,
                reference.PreviousInternshipDays),
            CompareEmail("Email", document.Email, reference.Email),
            ComparePhone("PhoneNumber", document.PhoneNumber, reference.PhoneNumber)
        ];
    }

    // Kaydedilen öğrenci profili ile CSV referansındaki alanları karşılaştırma
    public List<FieldComparisonResult> CompareStudentWithReference(
        Student student,
        TestStudent reference)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(reference);

        return
        [
            CompareText("FullName", student.FullName, reference.FullName),
            CompareDigits("IdentityNumber", student.IdentityNumber, reference.IdentityNumber),
            CompareNationality("Nationality", student.Nationality, reference.Nationality),
            CompareDate("BirthDate", student.BirthDate, reference.BirthDate),
            CompareText("StudentNumber", student.StudentNumber, reference.StudentNumber),
            CompareText("Address", student.Address, reference.Address),
            CompareText("Department", student.Department, reference.Department),
            CompareText("AcademicYear", student.AcademicYear, reference.AcademicYear),
            CompareNumber("PreviousInternshipDays",
                student.PreviousInternshipDays, reference.PreviousInternshipDays),
            CompareEmail("Email", student.Email, reference.Email),
            ComparePhone("PhoneNumber", student.PhoneNumber, reference.PhoneNumber),
            CompareNumber("HealthAidStatus", student.HealthAidStatus, reference.HealthAidStatus)
        ];
    }

    private static FieldComparisonResult CompareText(
        string fieldName,
        string? documentValue,
        string? formValue)
    {
        return CompareValues(
            fieldName,
            documentValue,
            formValue,
            NormalizeText);
    }

    private static FieldComparisonResult CompareDigits(
        string fieldName,
        string? documentValue,
        string? formValue)
    {
        return CompareValues(
            fieldName,
            documentValue,
            formValue,
            NormalizeDigits);
    }

    private static FieldComparisonResult CompareNationality(
        string fieldName,
        string? documentValue,
        string? referenceValue)
    {
        return CompareValues(
            fieldName,
            documentValue,
            referenceValue,
            NormalizeNationality);
    }

    private static FieldComparisonResult CompareEmail(
        string fieldName,
        string? documentValue,
        string? formValue)
    {
        return CompareValues(
            fieldName,
            documentValue,
            formValue,
            NormalizeEmail);
    }

    private static FieldComparisonResult ComparePhone(
        string fieldName,
        string? documentValue,
        string? formValue)
    {
        return CompareValues(
            fieldName,
            documentValue,
            formValue,
            NormalizePhone);
    }

    private static FieldComparisonResult CompareDate(
        string fieldName,
        DateTime? documentValue,
        DateTime formValue)
    {
        string? documentText = documentValue?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        string? formText = formValue == DateTime.MinValue
            ? null
            : formValue.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        return CompareValues(
            fieldName,
            documentText,
            formText,
            NormalizeText);
    }

    private static FieldComparisonResult CompareNumber(
        string fieldName,
        int? documentValue,
        int formValue)
    {
        string? documentText = documentValue?.ToString(CultureInfo.InvariantCulture);
        string? formText = formValue.ToString(CultureInfo.InvariantCulture);

        return CompareValues(
            fieldName,
            documentText,
            formText,
            NormalizeNumber);
    }

    private static FieldComparisonResult CompareValues(
        string fieldName,
        string? documentValue,
        string? formValue,
        Func<string?, string> normalize)
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

        string status = normalize(documentValue) == normalize(formValue)
            ? FieldComparisonStatus.Match
            : FieldComparisonStatus.Mismatch;

        return CreateResult(fieldName, documentValue, formValue, status);
    }

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
    // Karşılaştırma öncesi alan değerlerini düzenleme
    // Ad ve adres gibi metinlerde boşluk ve harf büyüklüğü farklarını giderir.
    // Noktalama işaretlerini korur.
    private static string NormalizeText(string? value)
    {
        return Regex.Replace(value ?? "", @"\s+", " ")
            .Trim()
            .ToUpperInvariant();
    }

    // Kimlik numarası biçimindeki boşluk ve ayraçları yok sayar.
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
        string compact = Regex.Replace(
            NormalizeText(value),
            @"[^\p{L}\p{N}]",
            "");

        return compact switch
        {
            "TC" or "TÜRKİYECUMHURİYETİ" => "TC",
            "FOREIGN" or "YABANCI" or "YABANCIUYRUKLU" => "FOREIGN",
            _ => compact
        };
    }

    // +90 532..., 0532... ve 532... biçimlerini aynı yerel numaraya indirger.
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
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
            ? NormalizeNumber(number)
            : "";
    }
}
