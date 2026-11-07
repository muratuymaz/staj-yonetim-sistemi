namespace StajYonetimSistemi.Models;

// Ek-16 üzerinden okunan bilgileri form bilgilerinden ayrı tutma
// Okunamayan metinleri boş, tarih ve sayıları null bırakma
public class DocumentReadResult
{
    private int id;
    private int documentId;
    private DateTime processedAt = DateTime.UtcNow;
    private string fullName = "";
    private string identityNumber = "";
    private string nationality = "";
    private string studentNumber = "";
    private Nullable<DateTime> birthDate;
    private string address = "";
    private string department = "";
    private string academicYear = "";
    private Nullable<int> previousInternshipDays;
    private string email = "";
    private string phoneNumber = "";
    private Nullable<int> healthAidStatus;
    private Nullable<DateTime> internshipStartDate;
    private Nullable<DateTime> internshipEndDate;
    private Nullable<int> internshipDayCount;

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

    public int DocumentId
    {
        get
        {
            return documentId;
        }

        set
        {
            documentId = value;
        }
    }

    public DateTime ProcessedAt
    {
        get
        {
            return processedAt;
        }

        set
        {
            processedAt = value;
        }
    }

    public string FullName
    {
        get
        {
            return fullName;
        }

        set
        {
            fullName = value;
        }
    }

    public string IdentityNumber
    {
        get
        {
            return identityNumber;
        }

        set
        {
            identityNumber = value;
        }
    }

    public string Nationality
    {
        get
        {
            return nationality;
        }

        set
        {
            nationality = value;
        }
    }

    public string StudentNumber
    {
        get
        {
            return studentNumber;
        }

        set
        {
            studentNumber = value;
        }
    }

    public Nullable<DateTime> BirthDate
    {
        get
        {
            return birthDate;
        }

        set
        {
            birthDate = value;
        }
    }

    public string Address
    {
        get
        {
            return address;
        }

        set
        {
            address = value;
        }
    }

    public string Department
    {
        get
        {
            return department;
        }

        set
        {
            department = value;
        }
    }

    public string AcademicYear
    {
        get
        {
            return academicYear;
        }

        set
        {
            academicYear = value;
        }
    }

    public Nullable<int> PreviousInternshipDays
    {
        get
        {
            return previousInternshipDays;
        }

        set
        {
            previousInternshipDays = value;
        }
    }

    public string Email
    {
        get
        {
            return email;
        }

        set
        {
            email = value;
        }
    }

    public string PhoneNumber
    {
        get
        {
            return phoneNumber;
        }

        set
        {
            phoneNumber = value;
        }
    }

    public Nullable<int> HealthAidStatus
    {
        get
        {
            return healthAidStatus;
        }

        set
        {
            healthAidStatus = value;
        }
    }

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

    public Nullable<int> InternshipDayCount
    {
        get
        {
            return internshipDayCount;
        }

        set
        {
            internshipDayCount = value;
        }
    }
}
