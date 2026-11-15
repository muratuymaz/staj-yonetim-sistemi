using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic.FileIO;
using Microsoft.EntityFrameworkCore;
using StajYonetimSistemi.Data;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Services;

// CSV aktarımı sırasında oluşan sonuç ve hata bilgilerini tutma
public class StudentDatasetImportResult
{
    public List<StudentDatasetImportRow> ValidatedRows { get; set; } = new();
    public int AddedCount { get; set; }
    public int UpdatedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsSuccess => Errors.Count == 0 && ValidatedRows.Count > 0;
}

// CSV içindeki doğrulanmış öğrenci kaydını tutma
public class StudentDatasetImportRow
{
    public int RowNumber { get; set; }
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
    public int HealthAidStatus { get; set; }
}

public class StudentDatasetImportService
{
    private readonly StajDbContext database;

    public StudentDatasetImportService(StajDbContext database)
    {
        this.database = database;
    }

    // Doğrulanmış CSV kayıtlarını yalnızca referans tablosuna aktarma
    public async Task<StudentDatasetImportResult> ImportAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        StudentDatasetImportResult result = await ReadFileAsync(file, cancellationToken);

        if (result.IsSuccess == false)
        {
            return result;
        }

        List<TestStudent> referenceStudents = await database.TestStudents
            .ToListAsync(cancellationToken);

        foreach (StudentDatasetImportRow record in result.ValidatedRows)
        {
            // Aynı kimlik veya e-posta farklı referans numarasına aitse aktarımı durdurma
            bool belongsToAnotherReference = referenceStudents.Any(student =>
                string.Equals(student.StudentNumber, record.StudentNumber, StringComparison.OrdinalIgnoreCase) == false &&
                (string.Equals(student.IdentityNumber, record.IdentityNumber, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(student.Email, record.Email, StringComparison.OrdinalIgnoreCase)));

            if (belongsToAnotherReference)
            {
                result.Errors.Add($"{record.RowNumber}. satir: Kimlik numarasi veya e-posta baska bir referans kaydina ait.");
            }
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        foreach (StudentDatasetImportRow record in result.ValidatedRows)
        {
            // Öğrenci numarasına göre yalnızca test referansını ekleme veya güncelleme
            TestStudent? referenceStudent = referenceStudents.FirstOrDefault(student =>
                string.Equals(student.StudentNumber, record.StudentNumber, StringComparison.OrdinalIgnoreCase));

            if (referenceStudent is null)
            {
                referenceStudent = new TestStudent();
                database.TestStudents.Add(referenceStudent);
                result.AddedCount++;
            }
            else
            {
                result.UpdatedCount++;
            }

            ApplyRecord(referenceStudent, record);
        }

        try
        {
            // Tüm satırları tek veritabanı işlemi içinde kaydetme
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
                await database.Database.BeginTransactionAsync(cancellationToken);

            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            result.Errors.Add("CSV kayitlari veritabanina aktarilirken bir hata olustu.");
            result.AddedCount = 0;
            result.UpdatedCount = 0;
        }

        return result;
    }


    // CSV dosyasının izin verilen en yüksek boyutunu belirleme
    private const long MaxFileSize = 10 * 1024 * 1024;
    // CSV dosyasını okuyup kayıtları doğrulama
    private async Task<StudentDatasetImportResult> ReadFileAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        StudentDatasetImportResult result = new();

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

        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("Yalnizca .csv uzantili dosyalar yuklenebilir.");
            return result;
        }

        string csvText;
        try
        {
            await using Stream stream = file.OpenReadStream();
            using StreamReader reader = new(stream, new UTF8Encoding(false, true));
            csvText = await reader.ReadToEndAsync(cancellationToken);
        }
        catch (DecoderFallbackException)
        {
            result.Errors.Add("CSV dosyasi UTF-8 kodlamasinda olmalidir.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(csvText))
        {
            result.Errors.Add("CSV dosyasi bos olamaz.");
            return result;
        }

        using StringReader textReader = new(csvText);
        using TextFieldParser parser = new(textReader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(DetectDelimiter(csvText).ToString());

        try
        {
            string[]? headers = parser.ReadFields();
            if (headers is null || parser.EndOfData)
            {
                result.Errors.Add("CSV dosyasinda baslik ve en az bir kayit bulunmalidir.");
                return result;
            }

            Dictionary<string, int> columnIndexes = CreateColumnIndexes(headers, result.Errors);
            if (result.Errors.Count > 0)
                return result;

            HashSet<string> studentNumbers = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> identityNumbers = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> emails = new(StringComparer.OrdinalIgnoreCase);
            bool hasDataRow = false;
            int rowNumber = 2;

            while (!parser.EndOfData)
            {
                string[]? fields = parser.ReadFields();
                if (fields is null)
                    continue;

                hasDataRow = true;
                if (fields.All(string.IsNullOrWhiteSpace))
                    continue;

                StudentDatasetImportRow? record = ValidateRow(
                    fields, columnIndexes, rowNumber, result.Errors);

                if (record != null)
                {
                    if (!studentNumbers.Add(record.StudentNumber))
                        result.Errors.Add($"{rowNumber}. satir: Ogrenci numarasi CSV icinde tekrar ediyor.");

                    if (!identityNumbers.Add(record.IdentityNumber))
                        result.Errors.Add($"{rowNumber}. satir: Kimlik numarasi CSV icinde tekrar ediyor.");

                    if (!emails.Add(record.Email))
                        result.Errors.Add($"{rowNumber}. satir: E-posta adresi CSV icinde tekrar ediyor.");

                    result.ValidatedRows.Add(record);
                }

                rowNumber++;
            }

            if (!hasDataRow)
                result.Errors.Add("CSV dosyasinda baslik ve en az bir kayit bulunmalidir.");
            else if (result.ValidatedRows.Count == 0 && result.Errors.Count == 0)
                result.Errors.Add("CSV dosyasinda aktarilacak kayit bulunamadi.");
        }
        catch (MalformedLineException exception)
        {
            result.Errors.Add($"CSV dosyasinin {exception.LineNumber}. satiri okunamadi.");
        }

        return result;
    }
    // CSV başlıklarını alanlara eşleyip zorunlu sütunları kontrol etme
    private static readonly Dictionary<string, string[]> requiredColumnAliases = new()
    {
        { "FullName", ["ogrenci ad soyad", "ad soyad", "full name"] },
        { "IdentityNumber", ["tc kimlik no", "t c kimlik no", "kimlik no", "tc no", "yabanci kimlik no"] },
        { "Nationality", ["uyruk", "nationality"] },
        { "StudentNumber", ["ogrenci no", "ogrenci numarasi", "student number"] },
        { "BirthDate", ["dogum tarihi", "birth date"] },
        { "Address", ["ikametgah adresi", "acik adres", "adres", "address"] },
        { "Department", ["bolum", "department"] },
        { "AcademicYear", ["ek 16 teslim yili", "ek16 teslim yili", "teslim yili", "academic year"] },
        { "PreviousInternshipDays", ["daha once yapilan staj gun sayisi", "onceki staj gun sayisi", "previous internship days"] },
        { "Email", ["e posta", "eposta", "email", "e mail"] },
        { "PhoneNumber", ["telefon no", "telefon numarasi", "telefon", "phone"] },
        { "HealthAidStatus", ["saglik yardimi statusu", "saglik yardimi", "sigorta durumu", "health aid status"] }
    };

    private static Dictionary<string, int> CreateColumnIndexes(
        string[] headers,
        List<string> errors)
    {
        Dictionary<string, int> headerIndexes = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < headers.Length; index++)
            headerIndexes[Normalize(headers[index])] = index;

        Dictionary<string, int> columnIndexes = new();
        foreach (KeyValuePair<string, string[]> column in requiredColumnAliases)
        {
            string? alias = column.Value.FirstOrDefault(value =>
                headerIndexes.ContainsKey(Normalize(value)));

            if (alias is null)
                errors.Add($"CSV basliginda '{column.Key}' alani bulunamadi.");
            else
                columnIndexes[column.Key] = headerIndexes[Normalize(alias)];
        }

        return columnIndexes;
    }

    private static StudentDatasetImportRow? ValidateRow(
        string[] row,
        Dictionary<string, int> columns,
        int rowNumber,
        List<string> errors)
    {
        string Value(string name) => columns.TryGetValue(name, out int index) && index < row.Length
            ? row[index].Trim()
            : "";

        string fullName = Value("FullName");
        string identityNumber = Value("IdentityNumber");
        string nationality = Value("Nationality");
        string studentNumber = Value("StudentNumber");
        string birthDateText = Value("BirthDate");
        string address = Value("Address");
        string department = Value("Department");
        string academicYear = Value("AcademicYear");
        string previousDaysText = Value("PreviousInternshipDays");
        string email = Value("Email");
        string phoneNumber = Value("PhoneNumber");
        string healthAidText = Value("HealthAidStatus");
        List<string> rowErrors = new();

        if (new[] { fullName, identityNumber, nationality, studentNumber, birthDateText,
                address, department, academicYear, previousDaysText, email, phoneNumber, healthAidText }
            .Any(string.IsNullOrWhiteSpace))
            rowErrors.Add("zorunlu alanlardan biri bos");

        bool validBirthDate = DateTime.TryParse(
            birthDateText, CultureInfo.GetCultureInfo("tr-TR"),
            DateTimeStyles.AllowWhiteSpaces, out DateTime birthDate) ||
            DateTime.TryParse(
                birthDateText, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out birthDate);

        if (!validBirthDate)
            rowErrors.Add("dogum tarihi gecersiz");
        else if (birthDate.Date > DateTime.Today)
            rowErrors.Add("dogum tarihi gelecekte olamaz");

        bool validDays = int.TryParse(
            previousDaysText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int previousDays) ||
            int.TryParse(
                previousDaysText, NumberStyles.Integer,
                CultureInfo.GetCultureInfo("tr-TR"), out previousDays);

        if (!validDays)
            rowErrors.Add("onceki staj gun sayisi sayi olmalidir");
        else if (previousDays < 0)
            rowErrors.Add("onceki staj gun sayisi negatif olamaz");

        if (!new EmailAddressAttribute().IsValid(email))
            rowErrors.Add("e-posta adresi gecersiz");

        bool validHealthStatus = int.TryParse(Normalize(healthAidText), out int healthAidStatus) &&
            healthAidStatus is >= 1 and <= 6;
        if (!validHealthStatus)
        {
            string shownValue = healthAidText.Length > 40
                ? healthAidText[..40] + "..."
                : healthAidText;
            rowErrors.Add($"saglik yardimi statusu '{shownValue}' gecersiz; 1-6 arasinda olmalidir");
        }

        if (rowErrors.Count > 0)
        {
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
            BirthDate = birthDate.Date,
            Address = address,
            Department = department,
            AcademicYear = academicYear,
            PreviousInternshipDays = previousDays,
            Email = email,
            PhoneNumber = phoneNumber,
            HealthAidStatus = healthAidStatus
        };
    }

    private static string Normalize(string value)
    {
        string text = value.Trim().Trim('\uFEFF').ToLowerInvariant()
            .Replace('ı', 'i').Replace('ğ', 'g').Replace('ş', 's')
            .Replace('ç', 'c').Replace('ö', 'o').Replace('ü', 'u')
            .Normalize(NormalizationForm.FormD);

        StringBuilder normalized = new();
        foreach (char character in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            normalized.Append(char.IsLetterOrDigit(character) ? character : ' ');
        }

        return string.Join(" ", normalized.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static char DetectDelimiter(string csvText)
    {
        string firstLine = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0];
        return firstLine.Count(character => character == ';') >
               firstLine.Count(character => character == ',') ? ';' : ',';
    }
    private static void ApplyRecord(TestStudent student, StudentDatasetImportRow record)
    {
        // CSV alanlarını test referansı modeline aktarma
        student.FullName = record.FullName;
        student.IdentityNumber = record.IdentityNumber;
        student.Nationality = record.Nationality;
        student.StudentNumber = record.StudentNumber;
        student.BirthDate = record.BirthDate;
        student.Address = record.Address;
        student.Department = record.Department;
        student.AcademicYear = record.AcademicYear;
        student.PreviousInternshipDays = record.PreviousInternshipDays;
        student.Email = record.Email;
        student.PhoneNumber = record.PhoneNumber;
        student.HealthAidStatus = record.HealthAidStatus;
    }

}