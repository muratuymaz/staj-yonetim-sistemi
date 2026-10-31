namespace StajYonetimSistemi.Models;

// Ortak hesap bilgilerini User sınıfından alma
public class Student : User
{
    private string studentNumber = "";
    private string fullName = "";
    private string identityNumber = "";
    private string nationality = "";
    private DateTime birthDate;
    private string address = "";
    private string phoneNumber = "";
    private string department = "";
    private string academicYear = "";
    private int previousInternshipDays;
    private int healthAidStatus;

    // Öğrenci kimlik bilgilerini tutma
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

    // İletişim bilgilerini tutma, e-postayı User kaydından alma
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

    // Bölüm ve staj bilgilerini tutma
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
