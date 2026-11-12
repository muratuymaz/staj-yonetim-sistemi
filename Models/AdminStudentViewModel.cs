namespace StajYonetimSistemi.Models;

public class AdminStudentViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string StudentNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Nationality { get; set; } = "";
    public string Department { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string DocumentStatus { get; set; } = "Yüklenmedi";
    public string DocumentStatusKey { get; set; } = "not-uploaded";
    public int? DocumentId { get; set; }
}
