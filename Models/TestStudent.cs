namespace StajYonetimSistemi.Models;

// CSV referans bilgilerini hesaplardan ayrı tutma
public class TestStudent
{
    private int id;
    private string fullName = "";
    private string identityNumber = "";
    private string nationality = "";
    private string studentNumber = "";
    private DateTime birthDate;
    private string address = "";
    private string department = "";
    private string academicYear = "";
    private int previousInternshipDays;
    private string email = "";
    private string phoneNumber = "";
    private int healthAidStatus;

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
    public DateTime BirthDate
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
    public int PreviousInternshipDays
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
    public int HealthAidStatus
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
}
