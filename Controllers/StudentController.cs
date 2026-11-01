using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajYonetimSistemi.Data;
using StajYonetimSistemi.Filters;
using StajYonetimSistemi.Models;
using StajYonetimSistemi.Services;

namespace StajYonetimSistemi.Controllers
{
    [SessionAuthorize("Student")]
    public class StudentController : Controller
    {
        private readonly StajDbContext database;
        private readonly IWebHostEnvironment environment;
        private readonly DocumentFileValidator fileValidator;

        private const long MaxFileSize = 10 * 1024 * 1024;

        public StudentController(StajDbContext database, IWebHostEnvironment environment, DocumentFileValidator fileValidator)
        {
            this.database = database;
            this.environment = environment;
            this.fileValidator = fileValidator;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Giriş yapan öğrencinin bilgilerini getirme
        [HttpGet]
        public IActionResult Profile()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));
            var student = database.Students.Find(userId);

            if (student == null || string.IsNullOrWhiteSpace(student.StudentNumber))
            {
                return NotFound("Öğrenci bilgileri henüz kaydedilmedi.");
            }

            // Hesap şifresi ve rolü yerine yalnızca profil bilgilerini gönderme
            StudentProfileForm profile = new StudentProfileForm();
            profile.StudentNumber = student.StudentNumber;
            profile.FullName = student.FullName;
            profile.IdentityNumber = student.IdentityNumber;
            profile.Nationality = student.Nationality;
            profile.BirthDate = student.BirthDate;
            profile.Address = student.Address;
            profile.PhoneNumber = student.PhoneNumber;
            profile.Department = student.Department;
            profile.AcademicYear = student.AcademicYear;
            profile.PreviousInternshipDays = student.PreviousInternshipDays;
            profile.HealthAidStatus = student.HealthAidStatus;

            return Json(profile);
        }

        // Öğrenci bilgilerini giriş yapan hesaba kaydetme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile([FromForm] StudentProfileForm form)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest("Öğrenci bilgilerini kontrol edin.");
            }

            // Zorunlu bilgilerin boş olmadığını kontrol etme
            if (string.IsNullOrWhiteSpace(form.FullName) ||
                string.IsNullOrWhiteSpace(form.StudentNumber) ||
                string.IsNullOrWhiteSpace(form.IdentityNumber) ||
                string.IsNullOrWhiteSpace(form.Nationality) ||
                string.IsNullOrWhiteSpace(form.Address) ||
                string.IsNullOrWhiteSpace(form.PhoneNumber) ||
                string.IsNullOrWhiteSpace(form.Department) ||
                string.IsNullOrWhiteSpace(form.AcademicYear) ||
                form.BirthDate == DateTime.MinValue)
            {
                return BadRequest("Öğrenci bilgilerini eksiksiz doldurun.");
            }

            form.StudentNumber = form.StudentNumber.Trim();
            form.IdentityNumber = form.IdentityNumber.Trim();

            // Hesap bilgisini formdan değil oturumdan alma
            int userId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));

            // Başka öğrenciye ait numaraların kullanılmasını engelleme
            bool informationExists = database.Students.Any(item =>
                item.Id != userId &&
                (item.StudentNumber == form.StudentNumber ||
                 item.IdentityNumber == form.IdentityNumber));

            if (informationExists)
            {
                return Conflict("Öğrenci veya kimlik numarası zaten kayıtlı.");
            }

            var student = database.Students.Find(userId);

            if (student == null)
            {
                return NotFound("Öğrenci hesabı bulunamadı.");
            }

            // Yalnızca formdaki öğrenci bilgilerini güncelleme
            student.FullName = form.FullName.Trim();
            student.StudentNumber = form.StudentNumber;
            student.IdentityNumber = form.IdentityNumber;
            student.Nationality = form.Nationality.Trim();
            student.BirthDate = form.BirthDate;
            student.Address = form.Address.Trim();
            student.PhoneNumber = form.PhoneNumber.Trim();
            student.Department = form.Department.Trim();
            student.AcademicYear = form.AcademicYear.Trim();
            student.PreviousInternshipDays = form.PreviousInternshipDays;
            student.HealthAidStatus = form.HealthAidStatus;

            database.SaveChanges();

            return Ok("Öğrenci bilgileri kaydedildi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
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

            string? validationError = await fileValidator.ValidateAsync(file, cancellationToken);
            if (validationError is not null)
            {
                TempData["UploadMessage"] = validationError;
                return RedirectToAction(nameof(Index));
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            Student? student = await database.Students
                .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

            if (student == null)
            {
                TempData["UploadMessage"] = "Öğrenci kaydı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(student.StudentNumber))
            {
                TempData["UploadMessage"] = "Önce öğrenci bilgilerinizi doldurun.";
                return RedirectToAction(nameof(Index));
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string storedFileName = $"{Guid.NewGuid():N}{extension}";
            string uploadDirectory = Path.Combine(environment.ContentRootPath, "UploadedFiles");
            string fullPath = Path.Combine(uploadDirectory, storedFileName);

            try
            {
                Directory.CreateDirectory(uploadDirectory);

                await using (FileStream stream = new(fullPath, FileMode.CreateNew))
                {
                    await file.CopyToAsync(stream, cancellationToken);
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
                await database.SaveChangesAsync(cancellationToken);

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
