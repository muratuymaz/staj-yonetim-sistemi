namespace StajYonetimSistemi.Models;

// Admin sayfasına gönderilen listeleri tutma
public class AdminDashboardViewModel
{
    private List<TestStudent> testStudents = new List<TestStudent>();
    private List<AdminStudentViewModel> students = new List<AdminStudentViewModel>();

    // Admin ekranındaki referans öğrenci listesini tutma
    public List<TestStudent> TestStudents
    {
        get
        {
            return testStudents;
        }

        set
        {
            testStudents = value;
        }
    }

    // Admin ekranındaki kayıtlı öğrenci listesini tutma
    public List<AdminStudentViewModel> Students
    {
        get
        {
            return students;
        }

        set
        {
            students = value;
        }
    }
}

// Admin listesindeki bir öğrencinin bilgilerini tutma
public class AdminStudentViewModel
{
    private int id;
    private string userName = "";
    private string email = "";
    private string studentNumber = "";
    private string fullName = "";
    private string nationality = "";
    private string department = "";
    private string phoneNumber = "";
    private string documentStatus = "Yüklenmedi";
    private string documentStatusKey = "not-uploaded";
    private Nullable<int> documentId;

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

    // Kullanıcı adını tutma
    public string UserName
    {
        get
        {
            return userName;
        }

        set
        {
            userName = value;
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

    // Ekranda gösterilecek belge durumunu tutma
    public string DocumentStatus
    {
        get
        {
            return documentStatus;
        }

        set
        {
            documentStatus = value;
        }
    }

    // Belge durumunun kodunu tutma
    public string DocumentStatusKey
    {
        get
        {
            return documentStatusKey;
        }

        set
        {
            documentStatusKey = value;
        }
    }

    // İlgili belgenin kimliğini tutma
    public Nullable<int> DocumentId
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
}
