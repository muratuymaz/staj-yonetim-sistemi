using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace StajYonetimSistemi.Services;

public class StudentDatasetImportResult
{
    public string CsvText { get; set; } = "";
    public List<string> Headers { get; set; } = new List<string>();
    public List<List<string>> Rows { get; set; } = new List<List<string>>();
    public List<string> Errors { get; set; } = new List<string>();

    public bool IsSuccess
    {
        get
        {
            return Errors.Count == 0 && Rows.Count > 0;
        }
    }
}

public class StudentDatasetImportService
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    // CSV dosyasini aktarim icin okuyup temel dosya kontrollerini yapma
    public async Task<StudentDatasetImportResult> ReadFileAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        StudentDatasetImportResult result = new StudentDatasetImportResult();

        if (file is null || file.Length == 0)
        {
            result.Errors.Add("Lutfen bir CSV dosyasi secin.");
            return result;
        }

        if (file.Length > MaxFileSize)
        {
            result.Errors.Add("CSV dosyasi en fazla 10 MB olabilir.");
            return result;
        }

        string extension = Path.GetExtension(file.FileName);

        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) == false)
        {
            result.Errors.Add("Yalnizca .csv uzantili dosyalar yuklenebilir.");
            return result;
        }

        try
        {
            await using Stream fileStream = file.OpenReadStream();
            using StreamReader reader = new(
                fileStream,
                new UTF8Encoding(false, true));

            result.CsvText = await reader.ReadToEndAsync(cancellationToken);
        }
        catch (DecoderFallbackException)
        {
            result.Errors.Add("CSV dosyasi UTF-8 kodlamasinda olmalidir.");
        }

        if (string.IsNullOrWhiteSpace(result.CsvText))
        {
            result.Errors.Add("CSV dosyasi bos olamaz.");
            return result;
        }

        char delimiter = DetectDelimiter(result.CsvText);
        List<List<string>> parsedRows = ParseCsv(result.CsvText, delimiter);

        if (parsedRows.Count < 2)
        {
            result.Errors.Add("CSV dosyasinda baslik ve en az bir kayit bulunmalidir.");
            return result;
        }

        result.Headers = parsedRows[0];
        result.Rows = parsedRows.Skip(1)
            .Where(row => row.Any(value => string.IsNullOrWhiteSpace(value) == false))
            .ToList();

        if (result.Rows.Count == 0)
        {
            result.Errors.Add("CSV dosyasinda aktarilacak kayit bulunamadi.");
            return result;
        }

        ValidateRequiredHeaders(result.Headers, result.Errors);

        return result;
    }

    private static readonly Dictionary<string, string[]> requiredColumnAliases = new()
    {
        { "FullName", new[] { "ogrenci ad soyad", "ad soyad", "full name" } },
        { "IdentityNumber", new[] { "tc kimlik no", "t c kimlik no", "kimlik no", "tc no", "yabanci kimlik no" } },
        { "Nationality", new[] { "uyruk", "nationality" } },
        { "StudentNumber", new[] { "ogrenci no", "ogrenci numarasi", "student number" } },
        { "BirthDate", new[] { "dogum tarihi", "birth date" } },
        { "Address", new[] { "ikametgah adresi", "acik adres", "adres", "address" } },
        { "Department", new[] { "bolum", "department" } },
        { "AcademicYear", new[] { "ek 16 teslim yili", "ek16 teslim yili", "teslim yili", "academic year" } },
        { "PreviousInternshipDays", new[] { "daha once yapilan staj gun sayisi", "onceki staj gun sayisi", "previous internship days" } },
        { "Email", new[] { "e posta", "eposta", "email", "e mail" } },
        { "PhoneNumber", new[] { "telefon no", "telefon numarasi", "telefon", "phone" } },
        { "HealthAidStatus", new[] { "saglik yardimi statusu", "saglik yardimi", "sigorta durumu", "health aid status" } }
    };

    // CSV basliklarinin proje icin gerekli alanlari icerip icermedigini kontrol etme
    private static void ValidateRequiredHeaders(
        List<string> headers,
        List<string> errors)
    {
        HashSet<string> normalizedHeaders = headers
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string[]> column in requiredColumnAliases)
        {
            bool columnExists = column.Value.Any(alias =>
                normalizedHeaders.Contains(Normalize(alias)));

            if (columnExists == false)
            {
                errors.Add($"CSV basliginda '{column.Key}' alani bulunamadi.");
            }
        }
    }

    private static string Normalize(string value)
    {
        string decomposedValue = value
            .Trim()
            .Trim('\uFEFF')
            .ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ş', 's')
            .Replace('ç', 'c')
            .Replace('ö', 'o')
            .Replace('ü', 'u')
            .Normalize(NormalizationForm.FormD);
        StringBuilder normalizedValue = new StringBuilder();

        foreach (char character in decomposedValue)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            normalizedValue.Append(char.IsLetterOrDigit(character) ? character : ' ');
        }

        return string.Join(" ", normalizedValue.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static char DetectDelimiter(string csvText)
    {
        string firstLine = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0];
        int commaCount = firstLine.Count(character => character == ',');
        int semicolonCount = firstLine.Count(character => character == ';');

        return semicolonCount > commaCount ? ';' : ',';
    }

    private static List<List<string>> ParseCsv(string csvText, char delimiter)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> currentRow = new List<string>();
        StringBuilder currentValue = new StringBuilder();
        bool insideQuotes = false;

        for (int index = 0; index < csvText.Length; index++)
        {
            char character = csvText[index];

            if (insideQuotes)
            {
                if (character == '"' && index + 1 < csvText.Length && csvText[index + 1] == '"')
                {
                    currentValue.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    insideQuotes = false;
                }
                else
                {
                    currentValue.Append(character);
                }

                continue;
            }

            if (character == '"')
            {
                insideQuotes = true;
            }
            else if (character == delimiter)
            {
                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else if (character == '\r' || character == '\n')
            {
                if (character == '\r' && index + 1 < csvText.Length && csvText[index + 1] == '\n')
                {
                    index++;
                }

                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
                rows.Add(currentRow);
                currentRow = new List<string>();
            }
            else
            {
                currentValue.Append(character);
            }
        }

        if (currentValue.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentValue.ToString());
            rows.Add(currentRow);
        }

        return rows;
    }
}
