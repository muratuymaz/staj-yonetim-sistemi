namespace StajYonetimSistemi.Models;

public class AdminDashboardViewModel
{
    public List<TestStudent> TestStudents { get; set; } = new List<TestStudent>();
    public List<AdminStudentViewModel> Students { get; set; } = new List<AdminStudentViewModel>();
}
