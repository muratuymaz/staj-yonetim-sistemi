namespace StajYonetimSistemi.Models;

public class Document
{
    private int id;
    private int studentId;
    private string originalFileName = "";
    private string filePath = "";
    private DateTime uploadedAt = DateTime.UtcNow;
    private string status = "Uploaded";

    // Kayıt kimliğini tutma
    public int Id
    {
        get
        {
            return id;
        }

        set
        {
            id = value;
        }
    }

    // Belgenin ait olduğu öğrenci kimliğini tutma
    public int StudentId
    {
        get
        {
            return studentId;
        }

        set
        {
            studentId = value;
        }
    }

    // Yüklenen Ek-16 dosyasının bilgilerini tutma
    public string OriginalFileName
    {
        get
        {
            return originalFileName;
        }

        set
        {
            originalFileName = value;
        }
    }

    // Dosyanın saklandığı yolu tutma
    public string FilePath
    {
        get
        {
            return filePath;
        }

        set
        {
            filePath = value;
        }
    }

    // Dosyanın yüklenme zamanını tutma
    public DateTime UploadedAt
    {
        get
        {
            return uploadedAt;
        }

        set
        {
            uploadedAt = value;
        }
    }

    // Evrak durumunu tutma
    public string Status
    {
        get
        {
            return status;
        }

        set
        {
            status = value;
        }
    }

    // Formdan girilen staj bilgilerini tutma
    private Nullable<DateTime> internshipStartDate;
    private Nullable<DateTime> internshipEndDate;
    private string weeklyWorkDays = "";

    // Staj başlangıç tarihini tutma
    public Nullable<DateTime> InternshipStartDate
    {
        get
        {
            return internshipStartDate;
        }

        set
        {
            internshipStartDate = value;
        }
    }

    // Staj bitiş tarihini tutma
    public Nullable<DateTime> InternshipEndDate
    {
        get
        {
            return internshipEndDate;
        }

        set
        {
            internshipEndDate = value;
        }
    }

    // Seçilen haftalık staj günlerini tutma
    public string WeeklyWorkDays
    {
        get
        {
            return weeklyWorkDays;
        }

        set
        {
            weeklyWorkDays = value;
        }
    }
}
