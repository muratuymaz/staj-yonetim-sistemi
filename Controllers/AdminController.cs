using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajYonetimSistemi.Data;
using StajYonetimSistemi.Filters;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Controllers
{
    [SessionAuthorize("Admin")]
    public class AdminController : Controller
    {
        private readonly StajDbContext database;

        public AdminController(StajDbContext database)
        {
            this.database = database;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            List<Student> students = await database.Students
                .AsNoTracking()
                .OrderBy(student => student.FullName)
                .ThenBy(student => student.UserName)
                .ToListAsync(cancellationToken);

            Dictionary<int, Document> latestDocuments = (await database.Documents
                    .AsNoTracking()
                    .OrderByDescending(document => document.UploadedAt)
                    .ToListAsync(cancellationToken))
                .GroupBy(document => document.StudentId)
                .ToDictionary(group => group.Key, group => group.First());

            List<AdminStudentViewModel> model = students
                .Select(student => CreateStudentRow(student, latestDocuments))
                .ToList();

            return View(model);
        }

        private static AdminStudentViewModel CreateStudentRow(
            Student student,
            Dictionary<int, Document> latestDocuments)
        {
            latestDocuments.TryGetValue(student.Id, out Document? document);
            (string statusKey, string statusText) = GetDocumentStatus(document);

            return new AdminStudentViewModel
            {
                Id = student.Id,
                UserName = student.UserName,
                Email = student.Email,
                StudentNumber = student.StudentNumber,
                FullName = string.IsNullOrWhiteSpace(student.FullName)
                    ? student.UserName
                    : student.FullName,
                Nationality = student.Nationality,
                Department = student.Department,
                PhoneNumber = student.PhoneNumber,
                DocumentStatus = statusText,
                DocumentStatusKey = statusKey
            };
        }

        private static (string Key, string Text) GetDocumentStatus(Document? document)
        {
            if (document == null)
            {
                return ("not-uploaded", "Yüklenmedi");
            }

            string status = document.Status.Trim().ToLowerInvariant();

            return status switch
            {
                "uploaded" or "reviewing" => ("reviewing", "İnceleniyor"),
                "missing" or "incomplete" => ("missing", "Hatalı / Eksik"),
                "approved" => ("approved", "Onaylandı"),
                "completed" => ("completed", "Tamamlandı"),
                _ => ("reviewing", document.Status)
            };
        }
    }
}
