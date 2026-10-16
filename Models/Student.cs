namespace StajYonetimSistemi.Models;

public class Student
{
    public int Id { get; set; }

    // Öğrenciyi kullanıcı hesabına bağlama
    public int UserId { get; set; }

    // Öğrenci kimlik bilgilerini tutma
    public string StudentNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string IdentityNumber { get; set; } = "";
    public string Nationality { get; set; } = "";
    public DateTime BirthDate { get; set; }

    // İletişim bilgilerini tutma, e-postayı User kaydından alma
    public string Address { get; set; } = "";
    public string PhoneNumber { get; set; } = "";

    // Bölüm ve staj bilgilerini tutma
    public string Department { get; set; } = "";
    public string AcademicYear { get; set; } = "";
    public int PreviousInternshipDays { get; set; }

    // Sağlık yardımı seçimini tutma
    public int HealthAidStatus { get; set; }
}
