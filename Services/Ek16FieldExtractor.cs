using System.Globalization;
using System.Text.RegularExpressions;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Services;

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
        "STAJ YAPILAN YER", "İŞVEREN VEYA YETKİLİNİN", "TELEFON NO",
        "FAX NO", "E-POSTA ADRESİ", "WEB ADRESİ", "HAFTA SONU",
        "ÇALIŞAN SAYISI", "MÜHENDİS SAYISI", "ÜCRET ALIYOR"
    ];

    private static readonly string[] DateFormats =
        ["d.M.yyyy", "dd.MM.yyyy", "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd"];
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    // Ek-16 metnindeki bilgileri okuma
    public DocumentReadResult Extract(string text, int documentId)
    {
        ArgumentNullException.ThrowIfNull(text);
        string normalizedText = NormalizeDocumentText(text);

        var result = new DocumentReadResult
        {
            DocumentId = documentId,
            FullName = ReadValue(normalizedText, "ADI SOYADI"),
            StudentNumber = ReadValue(normalizedText, "ÖĞRENCİ NUMARASI"),
            Department = ReadValue(normalizedText, "BÖLÜMÜ"),
            Address = ReadValue(normalizedText, "İKAMETGÂH ADRESİ", "İKAMETGAH ADRESİ"),
            AcademicYear = ReadValue(normalizedText, "ÖĞRETİM YILI"),
            BirthDate = ReadDate(normalizedText, "DOĞUM TARİHİ"),
            InternshipStartDate = ReadDate(normalizedText, "STAJ BAŞLAMA TARİHİ"),
            InternshipEndDate = ReadDate(normalizedText, "STAJ BİTİŞ TARİHİ")
        };

        ReadIdentityAndNationality(normalizedText, result);
        ReadInternshipCounts(normalizedText, result);
        ReadContactDetails(normalizedText, result);
        ReadHealthAidStatus(normalizedText, result);
        return result;
    }

    // Kimlik numarasını ve uyruk bilgisini okuma
    private static void ReadIdentityAndNationality(string text, DocumentReadResult result)
    {
        string value = ReadValue(text, "T.C. KİMLİK NO / UYRUĞU", "T.C. KİMLİK NO/UYRUĞU");
        Match identity = Regex.Match(value, @"(?<!\d)\d{11}(?!\d)");

        if (identity.Success)
        {
            result.IdentityNumber = identity.Value;
        }

        if (value.Contains("yabancı", StringComparison.OrdinalIgnoreCase) ||
            identity.Success && identity.Value.StartsWith("99"))
        {
            result.Nationality = "Foreign";
        }
        else if (value.Contains("T.C.", StringComparison.OrdinalIgnoreCase) || identity.Success)
        {
            result.Nationality = "TC";
        }
    }

    // Önceki ve bu staja ait gün sayılarını ayrı okuma
    private static void ReadInternshipCounts(string text, DocumentReadResult result)
    {
        string previousDays = ReadValue(text, "DAHA ÖNCE YAPTIĞI STAJ GÜN SAYISI");
        if (int.TryParse(previousDays, NumberStyles.Integer, TurkishCulture, out int parsedPreviousDays))
        {
            result.PreviousInternshipDays = parsedPreviousDays;
        }

        string totalDays = ReadValue(text, "STAJ GÜN SAYISI");
        if (int.TryParse(totalDays, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedTotalDays))
        {
            result.InternshipDayCount = parsedTotalDays;
        }
    }

    // E-posta ve telefon bilgilerini okuma
    private static void ReadContactDetails(string text, DocumentReadResult result)
    {
        string value = ReadValue(text, "E-POSTA ADRESİ / TELEFON NO", "E-POSTA ADRESİ/TELEFON NO");
        Match email = Regex.Match(value, @"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}");
        Match phone = Regex.Match(value, @"(?:\+?90\s*)?0?\d[\d\s()/-]{8,15}");

        if (email.Success)
        {
            result.Email = email.Value;
        }
        if (phone.Success)
        {
            result.PhoneNumber = Regex.Replace(phone.Value, @"\s+", " ").Trim();
        }
    }

    // İşaretlenen sağlık yardımı seçeneğini okuma
    private static void ReadHealthAidStatus(string text, DocumentReadResult result)
    {
        MatchCollection selectedOptions = Regex.Matches(
            ReadValue(text, "SAĞLIK YARDIM STATÜSÜ"),
            @"(?<!\d)([1-6])\s*[-.)]?\s*[☒☑✓✔]");

        if (selectedOptions.Count == 1 &&
            int.TryParse(selectedOptions[0].Groups[1].Value, out int status))
        {
            result.HealthAidStatus = status;
        }
    }

    // Tarih bilgisini bilinen biçimlerden biriyle okuma
    private static DateTime? ReadDate(string text, params string[] labels)
    {
        if (DateTime.TryParseExact(
            ReadValue(text, labels), DateFormats, TurkishCulture, DateTimeStyles.None, out DateTime date))
        {
            return date;
        }

        return null;
    }

    private static string NormalizeDocumentText(string text)
    {
        string normalized = Regex.Replace(text, @"\s*/\s*", "/");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return Regex.Replace(normalized, @"TARİH\s+İ\b", "TARİHİ", RegexOptions.IgnoreCase);
    }

    // Başlıklar arasındaki değeri bulma
    private static string ReadValue(string text, params string[] labels)
    {
        Match? selected = null;
        foreach (string label in labels)
        {
            string prefix = "";
            if (label == "STAJ GÜN SAYISI")
            {
                prefix = @"(?<!DAHA ÖNCE YAPTIĞI )";
            }
            Match match = Regex.Match(
                text,
                $@"{prefix}(?<![\p{{L}}]){LabelPattern(label)}(?![\p{{L}}])\s*[:：-]?\s*",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (match.Success && (selected is null || match.Index < selected.Index))
            {
                selected = match;
            }
        }

        if (selected is null)
        {
            return "";
        }

        int valueStart = selected.Index + selected.Length;
        int valueEnd = text.Length;
        foreach (string label in Labels)
        {
            Match next = Regex.Match(
                text[valueStart..],
                $@"(?<![\p{{L}}]){LabelPattern(label)}(?![\p{{L}}])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (next.Success)
            {
                valueEnd = Math.Min(valueEnd, valueStart + next.Index);
            }
        }

        return text[valueStart..valueEnd].Trim(' ', ':', '-', '–', '—', '.', '\t');
    }

    private static string LabelPattern(string label) =>
        Regex.Escape(label.Replace(" / ", "/"))
            .Replace("İ", "[İi]")
            .Replace("I", "[Iı]");
}
