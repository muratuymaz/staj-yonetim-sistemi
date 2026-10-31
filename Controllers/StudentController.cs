using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajYonetimSistemi.Data;
using StajYonetimSistemi.Filters;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Controllers
{
    [SessionAuthorize("Student")]
    public class StudentController : Controller
    {
        private readonly StajDbContext database;
        private readonly IWebHostEnvironment environment;

        private const long MaxFileSize = 10 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
            };

        public StudentController(StajDbContext database, IWebHostEnvironment environment)
        {
            this.database = database;
            this.environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["UploadMessage"] = "Lütfen bir dosya seçin.";
                return RedirectToAction(nameof(Index));
            }

            if (file.Length > MaxFileSize)
            {
                TempData["UploadMessage"] = "Dosya boyutu en fazla 10 MB olabilir.";
                return RedirectToAction(nameof(Index));
            }

            string extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
            {
                TempData["UploadMessage"] = "Bu dosya türüne izin verilmiyor.";
                return RedirectToAction(nameof(Index));
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            Student? student = await database.Students
                .SingleOrDefaultAsync(item => item.UserId == userId);

            if (student == null)
            {
                TempData["UploadMessage"] = "Öğrenci kaydı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            string storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            string uploadDirectory = Path.Combine(environment.ContentRootPath, "UploadedFiles");
            string fullPath = Path.Combine(uploadDirectory, storedFileName);

            try
            {
                Directory.CreateDirectory(uploadDirectory);

                await using (FileStream stream = new(fullPath, FileMode.CreateNew))
                {
                    await file.CopyToAsync(stream);
                }

                Document document = new()
                {
                    StudentId = student.Id,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    FilePath = Path.Combine("UploadedFiles", storedFileName),
                    UploadedAt = DateTime.UtcNow,
                    Status = "Uploaded"
                };

                database.Documents.Add(document);
                await database.SaveChangesAsync();

                TempData["UploadMessage"] = "Evrak başarıyla yüklendi.";
            }
            catch
            {
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                TempData["UploadMessage"] = "Dosya yüklenirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
