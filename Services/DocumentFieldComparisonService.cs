using System.Globalization;
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

    private static FieldComparisonResult CompareText(
        string fieldName,
        string? documentValue,
        string? formValue)
    {
        return CompareValues(
            fieldName,
            documentValue,
            formValue,
            FieldValueNormalizer.NormalizeText);
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
            FieldValueNormalizer.NormalizeDigits);
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
            FieldValueNormalizer.NormalizeEmail);
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
            FieldValueNormalizer.NormalizePhone);
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
            FieldValueNormalizer.NormalizeText);
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
            FieldValueNormalizer.NormalizeNumber);
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
}
