namespace StajYonetimSistemi.Models;

public class Document
{
    private int id;
    private int studentId;
    private string originalFileName = "";
    private string filePath = "";
    private DateTime uploadedAt = DateTime.UtcNow;
    private string status = "Uploaded";

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
