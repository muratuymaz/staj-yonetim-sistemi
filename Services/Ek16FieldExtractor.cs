using System.Globalization;
using System.Text.RegularExpressions;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Services;

// Ek-16 metnindeki bilgileri okuma
public sealed class Ek16FieldExtractor
{
    // Alanların başlangıç ve bitiş başlıklarını tutma
    private static readonly string[] Labels = new string[]
    {
        "T.C. KİMLİK NO / UYRUĞU", "T.C. KİMLİK NO/UYRUĞU", "ADI SOYADI",
        "DOĞUM TARİHİ", "ÖĞRENCİ NUMARASI", "BÖLÜMÜ",
        "DAHA ÖNCE YAPTIĞI STAJ GÜN SAYISI", "ÖĞRETİM YILI",
        "E-POSTA ADRESİ / TELEFON NO", "E-POSTA ADRESİ/TELEFON NO",
        "İKAMETGÂH ADRESİ", "SAĞLIK YARDIM STATÜSÜ",
        "STAJ BAŞLAMA TARİHİ", "STAJ BİTİŞ TARİHİ", "STAJ GÜN SAYISI",
        "STAJ YAPILAN YER", "İŞVEREN VEYA YETKİLİNİN",
        "TELEFON NO", "FAX NO", "E-POSTA ADRESİ", "WEB ADRESİ",
        "HAFTA SONU", "ÇALIŞAN SAYISI", "MÜHENDİS SAYISI", "ÜCRET ALIYOR"
    };

    public DocumentReadResult Extract(string text, int documentId)
    {
        ArgumentNullException.ThrowIfNull(text);

        DocumentReadResult result = new DocumentReadResult();
        result.DocumentId = documentId;

        // Öğrencinin adını belgeden okuma
        result.FullName = ReadValue(text, "ADI SOYADI");

        // Kimlik numarasını ve uyruk bilgisini belirleme
        string identityAndNationality = ReadValue(
            text, "T.C. KİMLİK NO / UYRUĞU", "T.C. KİMLİK NO/UYRUĞU");
        Match identityMatch = Regex.Match(identityAndNationality, @"(?<!\d)\d{11}(?!\d)");
        if (identityMatch.Success)
        {
            result.IdentityNumber = identityMatch.Value;
        }

        if (identityAndNationality.Contains("yabancı", StringComparison.OrdinalIgnoreCase))
        {
            result.Nationality = "Foreign";
        }
        else if (identityMatch.Success && identityMatch.Value.StartsWith("99"))
        {
            // 99 ile başlayan kimlik numarasından yabancı uyruk çıkarsama
            result.Nationality = "Foreign";
        }
        else if (identityAndNationality.Contains("T.C.", StringComparison.OrdinalIgnoreCase) ||
                 identityMatch.Success)
        {
            result.Nationality = "TC";
        }

        // Diğer öğrenci bilgilerini okuma
        result.StudentNumber = ReadValue(text, "ÖĞRENCİ NUMARASI");
        result.Department = ReadValue(text, "BÖLÜMÜ");
        result.Address = ReadValue(text, "İKAMETGÂH ADRESİ", "İKAMETGAH ADRESİ");
        result.AcademicYear = ReadValue(text, "ÖĞRETİM YILI");

        // Daha önce yapılan staj gün sayısını okuma
        string previousDays = ReadValue(text, "DAHA ÖNCE YAPTIĞI STAJ GÜN SAYISI");
        if (int.TryParse(previousDays, NumberStyles.Integer, TurkishCulture,
                out int previousInternshipDays))
        {
            result.PreviousInternshipDays = previousInternshipDays;
        }

        // Aynı alandaki e-posta ve telefon bilgisini ayırma
        string emailAndPhone = ReadValue(
            text, "E-POSTA ADRESİ / TELEFON NO", "E-POSTA ADRESİ/TELEFON NO");
        Match emailMatch = Regex.Match(emailAndPhone, @"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}");
        if (emailMatch.Success)
        {
            result.Email = emailMatch.Value;
        }

        Match phoneMatch = Regex.Match(emailAndPhone, @"(?:\+?90\s*)?0?\d[\d\s()/-]{8,15}");
        if (phoneMatch.Success)
        {
            result.PhoneNumber = Regex.Replace(phoneMatch.Value, @"\s+", " ").Trim();
        }

        // Doğum ve staj tarihlerini okuma
        result.BirthDate = ReadDate(text, "DOĞUM TARİHİ");
        result.InternshipStartDate = ReadDate(text, "STAJ BAŞLAMA TARİHİ");
        result.InternshipEndDate = ReadDate(text, "STAJ BİTİŞ TARİHİ");

        // Bu staj için belirtilen toplam gün sayısını okuma
        string internshipDayCount = ReadValue(text, "STAJ GÜN SAYISI");
        if (int.TryParse(internshipDayCount, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int parsedInternshipDayCount))
        {
            result.InternshipDayCount = parsedInternshipDayCount;
        }

        // Yalnızca tek bir işaretli seçenek okunabiliyorsa sağlık durumunu kaydetme
        string healthSection = ReadValue(text, "SAĞLIK YARDIM STATÜSÜ");
        MatchCollection selectedOptions = Regex.Matches(healthSection, @"(?<!\d)([1-6])\s*[-.)]?\s*[☒☑✓✔]");
        if (selectedOptions.Count == 1)
        {
            result.HealthAidStatus = int.Parse(selectedOptions[0].Groups[1].Value);
        }

        return result;
    }

    // Tarihi bilinen biçimlerden biriyle okuma
    private static DateTime? ReadDate(string text, params string[] labels)
    {
        string value = ReadValue(text, labels);
        string[] formats = new string[] { "d.M.yyyy", "dd.MM.yyyy", "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };

        bool dateFound = DateTime.TryParseExact(
            value,
            formats,
            TurkishCulture,
            DateTimeStyles.None,
            out DateTime date);

        if (dateFound)
        {
            return date;
        }

        return null;
    }

    // Başlık ile sonraki başlık arasındaki değeri bulma
    private static string ReadValue(string text, params string[] labels)
    {
        // Başlıklardaki boşluk farklarını düzenleme
        string normalizedText = Regex.Replace(text, @"\s*/\s*", "/");
        normalizedText = Regex.Replace(normalizedText, @"\s+", " ").Trim();
        normalizedText = Regex.Replace(normalizedText, @"TARİH\s+İ\b", "TARİHİ", RegexOptions.IgnoreCase);
        Match? selectedMatch = null;

        // Aranan başlığın metindeki yerini bulma
        foreach (string label in labels)
        {
            string labelPattern = LabelPattern(label);
            string prefix = "";
            if (label == "STAJ GÜN SAYISI")
            {
                // Toplam gün başlığını önceki staj günü başlığıyla karıştırmama
                prefix = @"(?<!DAHA ÖNCE YAPTIĞI )";
            }
            Match match = Regex.Match(
                normalizedText,
                $@"{prefix}(?<![\p{{L}}]){labelPattern}(?![\p{{L}}])\s*[:：-]?\s*",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (match.Success && (selectedMatch is null || match.Index < selectedMatch.Index))
            {
                selectedMatch = match;
            }
        }

        if (selectedMatch is null)
        {
            return "";
        }

        int valueStart = selectedMatch.Index + selectedMatch.Length;
        int valueEnd = normalizedText.Length;

        // Sonraki başlığı bularak değerin bittiği yeri belirleme
        foreach (string nextLabel in Labels)
        {
            Match nextMatch = Regex.Match(
                normalizedText.Substring(valueStart),
                $@"(?<![\p{{L}}]){LabelPattern(nextLabel)}(?![\p{{L}}])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (nextMatch.Success)
            {
                valueEnd = Math.Min(valueEnd, valueStart + nextMatch.Index);
            }
        }

        string value = normalizedText.Substring(valueStart, valueEnd - valueStart);
        return value.Trim(' ', ':', '-', '–', '—', '.', '\t');
    }

    // Başlıklardaki Türkçe harf farklılıklarını aramaya hazırlama
    private static string LabelPattern(string label)
    {
        return Regex.Escape(label.Replace(" / ", "/"))
            .Replace("İ", "[İi]")
            .Replace("I", "[Iı]");
    }

    private static CultureInfo TurkishCulture => CultureInfo.GetCultureInfo("tr-TR");
}
