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

    // Sağlık yardımı seçimini tutma
    [Range(0, 2)]
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
