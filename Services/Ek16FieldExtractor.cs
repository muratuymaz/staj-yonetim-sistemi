using System.Globalization;
using System.Text.RegularExpressions;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Services;

/// <summary>Extracts the student form fields represented in Ek-16 text.</summary>
public sealed class Ek16FieldExtractor
{
    private static readonly string[] Labels =
    [
        "T.C. KİMLİK NO / UYRUĞU", "T.C. KİMLİK NO/UYRUĞU", "ADI SOYADI",
        "DOĞUM TARİHİ", "ÖĞRENCİ NUMARASI", "BÖLÜMÜ",
        "DAHA ÖNCE YAPTIĞI STAJ GÜN SAYISI", "ÖĞRETİM YILI",
        "E-POSTA ADRESİ / TELEFON NO", "E-POSTA ADRESİ/TELEFON NO",
        "İKAMETGÂH ADRESİ", "SAĞLIK YARDIM STATÜSÜ",
        "STAJ BAŞLAMA TARİHİ", "STAJ BİTİŞ TARİHİ", "STAJ GÜN SAYISI",
        "STAJ YAPILAN YER", "İŞVEREN VEYA YETKİLİNİN"
    ];

    public DocumentReadResult Extract(string text, int documentId)
    {
        ArgumentNullException.ThrowIfNull(text);

        DocumentReadResult result = new() { DocumentId = documentId };

        // Bu görevde yalnızca öğrenci formunda bulunan alanları dolduruyoruz.
        result.FullName = ReadValue(text, "ADI SOYADI");

        string identityAndNationality = ReadValue(
            text, "T.C. KİMLİK NO / UYRUĞU", "T.C. KİMLİK NO/UYRUĞU");
        Match identityMatch = Regex.Match(identityAndNationality, @"(?<!\d)\d{11}(?!\d)");
        if (identityMatch.Success)
            result.IdentityNumber = identityMatch.Value;

        if (identityAndNationality.Contains("yabancı", StringComparison.OrdinalIgnoreCase))
            result.Nationality = "Foreign";
        else if (identityAndNationality.Contains("T.C.", StringComparison.OrdinalIgnoreCase) ||
                 identityMatch.Success)
            result.Nationality = "TC";

        result.StudentNumber = ReadValue(text, "ÖĞRENCİ NUMARASI");
        result.Department = ReadValue(text, "BÖLÜMÜ");
        result.Address = ReadValue(text, "İKAMETGÂH ADRESİ", "İKAMETGAH ADRESİ");
        result.AcademicYear = ReadValue(text, "ÖĞRETİM YILI");

        string previousDays = ReadValue(text, "DAHA ÖNCE YAPTIĞI STAJ GÜN SAYISI");
        if (int.TryParse(previousDays, NumberStyles.Integer, TurkishCulture,
                out int previousInternshipDays))
            result.PreviousInternshipDays = previousInternshipDays;

        string emailAndPhone = ReadValue(
            text, "E-POSTA ADRESİ / TELEFON NO", "E-POSTA ADRESİ/TELEFON NO");
        Match emailMatch = Regex.Match(emailAndPhone, @"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}");
        if (emailMatch.Success)
            result.Email = emailMatch.Value;

        Match phoneMatch = Regex.Match(emailAndPhone, @"(?:\+?90\s*)?0?\d[\d\s()/-]{8,15}");
        if (phoneMatch.Success)
            result.PhoneNumber = Regex.Replace(phoneMatch.Value, @"\s+", " ").Trim();

        result.BirthDate = ReadDate(text, "DOĞUM TARİHİ");
        result.InternshipStartDate = ReadDate(text, "STAJ BAŞLAMA TARİHİ");
        result.InternshipEndDate = ReadDate(text, "STAJ BİTİŞ TARİHİ");

        string internshipDayCount = ReadValue(text, "STAJ GÜN SAYISI");
        if (int.TryParse(internshipDayCount, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int parsedInternshipDayCount))
            result.InternshipDayCount = parsedInternshipDayCount;

        // Sağlık statüsü formda mevcut; fakat PDF/DOCX metninde seçili kutu
        // güvenilir biçimde ayırt edilemediğinden bu aşamada tahmin etmiyoruz.
        return result;
    }

    private static DateTime? ReadDate(string text, params string[] labels)
    {
        string value = ReadValue(text, labels);
        string[] formats = ["d.M.yyyy", "dd.MM.yyyy", "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd"];

        return DateTime.TryParseExact(
            value,
            formats,
            TurkishCulture,
            DateTimeStyles.None,
            out DateTime date)
            ? date
            : null;
    }

    private static string ReadValue(string text, params string[] labels)
    {
        string normalizedText = Regex.Replace(text, @"\s+", " ").Trim();
        Match? selectedMatch = null;

        foreach (string label in labels)
        {
            Match match = Regex.Match(
                normalizedText,
                $@"(?<![\p{{L}}]){Regex.Escape(label)}(?![\p{{L}}])\s*[:：-]?\s*",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (match.Success && (selectedMatch is null || match.Index < selectedMatch.Index))
                selectedMatch = match;
        }

        if (selectedMatch is null)
            return "";

        int valueStart = selectedMatch.Index + selectedMatch.Length;
        int valueEnd = normalizedText.Length;

        foreach (string nextLabel in Labels)
        {
            Match nextMatch = Regex.Match(
                normalizedText[valueStart..],
                $@"(?<![\p{{L}}]){Regex.Escape(nextLabel)}(?![\p{{L}}])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (nextMatch.Success)
                valueEnd = Math.Min(valueEnd, valueStart + nextMatch.Index);
        }

        return normalizedText[valueStart..valueEnd]
            .Trim(' ', ':', '-', '–', '—', '.', '\t');
    }

    private static CultureInfo TurkishCulture => CultureInfo.GetCultureInfo("tr-TR");
}
