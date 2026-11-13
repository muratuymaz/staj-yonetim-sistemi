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

    // İlgili belgenin kimliğini tutma
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

    // Belgenin okunma zamanını tutma
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

    // Ad soyad bilgisini tutma
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

    // Kimlik numarasını tutma
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

    // Uyruk bilgisini tutma
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

    // Öğrenci numarasını tutma
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

    // Doğum tarihini tutma
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

    // Açık adresi tutma
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

    // Bölüm bilgisini tutma
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

    // Öğretim yılı ve yarıyıl bilgisini tutma
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

    // Önceden yapılan staj gün sayısını tutma
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

    // E-posta adresini tutma
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

    // Telefon numarasını tutma
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

    // Sağlık yardımı seçimini tutma
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

    // Belgedeki toplam staj gün sayısını tutma
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
