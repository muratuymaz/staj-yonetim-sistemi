using System.ComponentModel.DataAnnotations;

namespace StajYonetimSistemi.Models;

public class StudentProfileForm
{
    private string email = "";
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
    private DateTime internshipStartDate;
    private DateTime internshipEndDate;
    private List<string> internshipDays = new List<string>();

    // Öğrenci kimlik bilgilerini tutma
    [Required]
    [EmailAddress]
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

    // Öğrenci numarasını tutma
    [Required]
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

    // Ad soyad bilgisini tutma
    [Required]
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
    [Required]
    [StringLength(11, MinimumLength = 11)]
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
    [Required]
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

    // İletişim bilgilerini tutma
    [Required]
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

    // Telefon numarasını tutma
    [Required]
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
    [Required]
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
    [Required]
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
    [Range(0, int.MaxValue)]
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

    // Ek-16 üzerindeki altı sağlık yardımı maddesini tutma
    [Range(1, 6)]
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

    // Staj başlangıç tarihini tutma
    public DateTime InternshipStartDate
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
    public DateTime InternshipEndDate
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

    // Formda seçilen haftalık staj günlerini tutma
    public List<string> InternshipDays
    {
        get
        {
            return internshipDays;
        }

        set
        {
            internshipDays = value;
        }
    }

}
