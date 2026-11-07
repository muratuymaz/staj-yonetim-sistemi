using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace StajYonetimSistemi.Services;

// CSV aktarımı sırasında oluşan sonuç ve hata bilgilerini tutma
public class StudentDatasetImportResult
{
    // Dosyanın okunmuş ham metnini tutma
    public string CsvText { get; set; } = "";

    // CSV başlıklarını tutma
    public List<string> Headers { get; set; } = new List<string>();

    // CSV içindeki ham satırları tutma
    public List<List<string>> Rows { get; set; } = new List<List<string>>();

    // Doğrulaması tamamlanmış öğrenci kayıtlarını tutma
    public List<StudentDatasetImportRow> ValidatedRows { get; set; } = new List<StudentDatasetImportRow>();

    // Kullanıcıya gösterilecek hata mesajlarını tutma
    public List<string> Errors { get; set; } = new List<string>();

    // Aktarım için geçerli kayıt bulunup bulunmadığını kontrol etme
    public bool IsSuccess
    {
        get
        {
            return Errors.Count == 0 && ValidatedRows.Count > 0;
        }
    }
}

// CSV içindeki tek bir öğrenci kaydını doğrulanmış alanlarla tutma
public class StudentDatasetImportRow
{
    // CSV satır numarasını tutma
    public int RowNumber { get; set; }

    // Öğrenci kimlik ve iletişim bilgilerini tutma
    public string FullName { get; set; } = "";
    public string IdentityNumber { get; set; } = "";
    public string Nationality { get; set; } = "";
    public string StudentNumber { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public string Address { get; set; } = "";
    public string Department { get; set; } = "";
    public string AcademicYear { get; set; } = "";
    public int PreviousInternshipDays { get; set; }
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";

    // Sağlık yardımı statüsünü sayısal değer olarak tutma
    public int HealthAidStatus { get; set; }
}

public class StudentDatasetImportService
{
    // CSV dosyasının izin verilen en yüksek boyutunu belirleme
    private const long MaxFileSize = 10 * 1024 * 1024;

    // CSV dosyasını aktarım için okuyup temel dosya kontrollerini yapma
    public async Task<StudentDatasetImportResult> ReadFileAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        StudentDatasetImportResult result = new StudentDatasetImportResult();

        if (file is null || file.Length == 0)
        {
            // Dosya seçilmediğinde aktarımı durdurma
            result.Errors.Add("Lutfen bir CSV dosyasi secin.");
            return result;
        }

        if (file.Length > MaxFileSize)
        {
            // Büyük dosyaların sunucuya aktarılmasını engelleme
            result.Errors.Add("CSV dosyasi en fazla 10 MB olabilir.");
            return result;
        }

        string extension = Path.GetExtension(file.FileName);

        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) == false)
        {
            // CSV dışındaki dosya türlerini kabul etmeme
            result.Errors.Add("Yalnizca .csv uzantili dosyalar yuklenebilir.");
            return result;
        }

        try
        {
            // Dosyayı UTF-8 kodlamasıyla metin olarak okuma
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
            // Boş dosyaların ayrıştırılmasını engelleme
            result.Errors.Add("CSV dosyasi bos olamaz.");
            return result;
        }

        // CSV ayırıcısını belirleyip dosyayı satırlara ayırma
        char delimiter = DetectDelimiter(result.CsvText);
        List<List<string>> parsedRows = ParseCsv(result.CsvText, delimiter);

        if (parsedRows.Count < 2)
        {
            // Başlıksız veya kayıtsız CSV dosyasını kabul etmeme
            result.Errors.Add("CSV dosyasinda baslik ve en az bir kayit bulunmalidir.");
            return result;
        }

        // İlk satırı başlık, kalan satırları öğrenci kayıtları olarak ayırma
        result.Headers = parsedRows[0];
        result.Rows = parsedRows.Skip(1)
            .Where(row => row.Any(value => string.IsNullOrWhiteSpace(value) == false))
            .ToList();

        if (result.Rows.Count == 0)
        {
            // Yalnızca boş satırlardan oluşan dosyaları kabul etmeme
            result.Errors.Add("CSV dosyasinda aktarilacak kayit bulunamadi.");
            return result;
        }

        // Projede gerekli olan sütun başlıklarını kontrol etme
        ValidateRequiredHeaders(result.Headers, result.Errors);

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // Başlıkların satır içindeki sütun numaralarını hazırlama
        Dictionary<string, int> headerIndexes = CreateHeaderIndexes(result.Headers);
        HashSet<string> studentNumbers = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> identityNumbers = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> emails = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < result.Rows.Count; index++)
        {
            // Her satırı öğrenci kaydına dönüştürüp doğrulama
            int rowNumber = index + 2;
            StudentDatasetImportRow? validatedRow = ValidateRow(
                result.Rows[index],
                headerIndexes,
                rowNumber,
                result.Errors);

            if (validatedRow is null)
            {
                continue;
            }

            if (studentNumbers.Add(validatedRow.StudentNumber) == false)
            {
                // Aynı öğrenci numarasının dosyada tekrar etmesini engelleme
                result.Errors.Add($"{rowNumber}. satir: Ogrenci numarasi CSV icinde tekrar ediyor.");
            }

            if (identityNumbers.Add(validatedRow.IdentityNumber) == false)
            {
                // Aynı kimlik numarasının dosyada tekrar etmesini engelleme
                result.Errors.Add($"{rowNumber}. satir: Kimlik numarasi CSV icinde tekrar ediyor.");
            }

            if (emails.Add(validatedRow.Email) == false)
            {
                // Aynı e-posta adresinin dosyada tekrar etmesini engelleme
                result.Errors.Add($"{rowNumber}. satir: E-posta adresi CSV icinde tekrar ediyor.");
            }

            result.ValidatedRows.Add(validatedRow);
        }

        return result;
    }

    // CSV başlıklarının farklı yazım biçimlerini öğrenci alanlarına eşleme
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

    // CSV başlıklarının proje için gerekli alanları içerip içermediğini kontrol etme
    private static void ValidateRequiredHeaders(
        List<string> headers,
        List<string> errors)
    {
        // Başlıkları karşılaştırma için ortak bir biçime dönüştürme
        HashSet<string> normalizedHeaders = headers
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string[]> column in requiredColumnAliases)
        {
            // Her alan için tanımlanan başlık seçeneklerinden birini arama
            bool columnExists = column.Value.Any(alias =>
                normalizedHeaders.Contains(Normalize(alias)));

            if (columnExists == false)
            {
                // Eksik başlık varsa aktarımı durdurmak üzere hata ekleme
                errors.Add($"CSV basliginda '{column.Key}' alani bulunamadi.");
            }
        }
    }

    private static Dictionary<string, int> CreateHeaderIndexes(List<string> headers)
    {
        // Her normalize edilmiş başlığı sütun numarasıyla eşleme
        Dictionary<string, int> indexes = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < headers.Count; index++)
        {
            string normalizedHeader = Normalize(headers[index]);

            if (string.IsNullOrWhiteSpace(normalizedHeader) == false)
            {
                indexes[normalizedHeader] = index;
            }
        }

        return indexes;
    }

    private static StudentDatasetImportRow? ValidateRow(
        List<string> row,
        Dictionary<string, int> headers,
        int rowNumber,
        List<string> errors)
    {
        // Sütunlardaki ham değerleri alanlara göre okuma
        string fullName = GetColumnValue(row, headers, "FullName");
        string identityNumber = GetColumnValue(row, headers, "IdentityNumber");
        string nationality = GetColumnValue(row, headers, "Nationality");
        string studentNumber = GetColumnValue(row, headers, "StudentNumber");
        string birthDateText = GetColumnValue(row, headers, "BirthDate");
        string address = GetColumnValue(row, headers, "Address");
        string department = GetColumnValue(row, headers, "Department");
        string academicYear = GetColumnValue(row, headers, "AcademicYear");
        string previousDaysText = GetColumnValue(row, headers, "PreviousInternshipDays");
        string email = GetColumnValue(row, headers, "Email");
        string phoneNumber = GetColumnValue(row, headers, "PhoneNumber");
        string healthAidStatusText = GetColumnValue(row, headers, "HealthAidStatus");
        List<string> rowErrors = new List<string>();

        // Zorunlu alanların boş bırakılmadığını kontrol etme
        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(identityNumber) ||
            string.IsNullOrWhiteSpace(nationality) ||
            string.IsNullOrWhiteSpace(studentNumber) ||
            string.IsNullOrWhiteSpace(birthDateText) ||
            string.IsNullOrWhiteSpace(address) ||
            string.IsNullOrWhiteSpace(department) ||
            string.IsNullOrWhiteSpace(academicYear) ||
            string.IsNullOrWhiteSpace(previousDaysText) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(phoneNumber) ||
            string.IsNullOrWhiteSpace(healthAidStatusText))
        {
            rowErrors.Add("zorunlu alanlardan biri bos");
        }

        // Doğum tarihini Türkçe veya genel tarih biçiminde okuma
        DateTime birthDate = DateTime.MinValue;
        if (DateTime.TryParse(
                birthDateText,
                CultureInfo.GetCultureInfo("tr-TR"),
                DateTimeStyles.AllowWhiteSpaces,
                out DateTime parsedBirthDate) == false &&
            DateTime.TryParse(
                birthDateText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out parsedBirthDate) == false)
        {
            rowErrors.Add("dogum tarihi gecersiz");
        }
        else
        {
            birthDate = parsedBirthDate.Date;

            if (birthDate > DateTime.Today)
            {
                rowErrors.Add("dogum tarihi gelecekte olamaz");
            }
        }

        // Önceki staj gün sayısını sayısal değere dönüştürme
        int previousInternshipDays = 0;
        if (int.TryParse(
                previousDaysText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsedDays) == false &&
            int.TryParse(
                previousDaysText,
                NumberStyles.Integer,
                CultureInfo.GetCultureInfo("tr-TR"),
                out parsedDays) == false)
        {
            rowErrors.Add("onceki staj gun sayisi sayi olmalidir");
        }
        else if (parsedDays < 0)
        {
            rowErrors.Add("onceki staj gun sayisi negatif olamaz");
        }
        else
        {
            previousInternshipDays = parsedDays;
        }

        // E-posta adresinin geçerli biçimde olup olmadığını kontrol etme
        EmailAddressAttribute emailValidator = new EmailAddressAttribute();
        if (emailValidator.IsValid(email) == false)
        {
            rowErrors.Add("e-posta adresi gecersiz");
        }

        // Sağlık yardımı değerini sistemdeki sayısal statüye dönüştürme
        int healthAidStatus = ParseHealthAidStatus(healthAidStatusText);
        if (healthAidStatus < 0)
        {
            rowErrors.Add("saglik yardimi statusu gecersiz");
        }

        if (rowErrors.Count > 0)
        {
            // Satırdaki tüm hataları tek sonuç altında toplama
            errors.Add($"{rowNumber}. satir: {string.Join(", ", rowErrors)}.");
            return null;
        }

        return new StudentDatasetImportRow
        {
            RowNumber = rowNumber,
            FullName = fullName,
            IdentityNumber = identityNumber,
            Nationality = nationality,
            StudentNumber = studentNumber,
            BirthDate = birthDate,
            Address = address,
            Department = department,
            AcademicYear = academicYear,
            PreviousInternshipDays = previousInternshipDays,
            Email = email,
            PhoneNumber = phoneNumber,
            HealthAidStatus = healthAidStatus
        };
    }

    private static string GetColumnValue(
        List<string> row,
        Dictionary<string, int> headers,
        string columnName)
    {
        // Alanın hangi sütunda bulunduğunu başlık eşleşmesinden bulma
        int index = requiredColumnAliases[columnName]
            .Select(Normalize)
            .Where(headers.ContainsKey)
            .Select(alias => headers[alias])
            .First();

        return index < row.Count ? row[index].Trim() : "";
    }

    private static int ParseHealthAidStatus(string value)
    {
        // Sayısal sağlık statülerini doğrudan kabul etme
        string normalizedValue = Normalize(value);

        if (int.TryParse(normalizedValue, out int numericValue) && numericValue >= 0 && numericValue <= 2)
        {
            return numericValue;
        }

        // Metin olarak gelen sağlık statülerini sayısal karşılığına çevirme
        return normalizedValue switch
        {
            "yararlaniyor" or "var" or "evet" => 0,
            "yararlanmiyor" or "yok" or "hayir" => 1,
            "belirtilmedi" or "bilinmiyor" => 2,
            _ => -1
        };
    }

    private static string Normalize(string value)
    {
        // Türkçe karakterleri ve noktalama işaretlerini karşılaştırılabilir hale getirme
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
        // Başlık satırındaki ayraç sayısına göre CSV biçimini belirleme
        string firstLine = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0];
        int commaCount = firstLine.Count(character => character == ',');
        int semicolonCount = firstLine.Count(character => character == ';');

        return semicolonCount > commaCount ? ';' : ',';
    }

    private static List<List<string>> ParseCsv(string csvText, char delimiter)
    {
        // Tırnak içindeki ayraçları ve satır sonlarını dikkate alarak CSV ayrıştırma
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
