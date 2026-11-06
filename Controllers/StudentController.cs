using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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
        private readonly ILogger<StudentController> logger;

        private const long MaxFileSize = 10 * 1024 * 1024;

        public StudentController(
            StajDbContext database,
            IWebHostEnvironment environment,
            DocumentFileValidator fileValidator,
            ILogger<StudentController> logger)
        {
            this.database = database;
            this.environment = environment;
            this.fileValidator = fileValidator;
            this.logger = logger;
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
            profile.Email = student.Email;
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
                string.IsNullOrWhiteSpace(form.Email) ||
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

            form.Email = form.Email.Trim();
            form.FullName = form.FullName.Trim();
            form.StudentNumber = form.StudentNumber.Trim();
            form.IdentityNumber = form.IdentityNumber.Trim();
            form.Nationality = form.Nationality.Trim();
            form.Address = form.Address.Trim();
            form.PhoneNumber = form.PhoneNumber.Trim();
            form.Department = form.Department.Trim();
            form.AcademicYear = form.AcademicYear.Trim();

            EmailAddressAttribute emailValidator = new EmailAddressAttribute();
            if (emailValidator.IsValid(form.Email) == false)
            {
                return BadRequest("Geçerli bir e-posta adresi girin.");
            }

            if (form.BirthDate > DateTime.Today)
            {
                return BadRequest("Doğum tarihi gelecekte olamaz.");
            }

            if (form.PreviousInternshipDays < 0)
            {
                return BadRequest("Önceki staj gün sayısı negatif olamaz.");
            }

            if (form.HealthAidStatus < 0 || form.HealthAidStatus > 2)
            {
                return BadRequest("Sağlık yardımı statüsü geçersiz.");
            }

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

            bool emailExists = database.Users.Any(user =>
                user.Id != userId && user.Email == form.Email);

            if (emailExists)
            {
                return Conflict("Bu e-posta adresi başka bir kullanıcıya ait.");
            }

            var student = database.Students.Find(userId);

            if (student == null)
            {
                return NotFound("Öğrenci hesabı bulunamadı.");
            }

            // Yalnızca formdaki öğrenci bilgilerini güncelleme
            student.Email = form.Email;
            student.FullName = form.FullName;
            student.StudentNumber = form.StudentNumber;
            student.IdentityNumber = form.IdentityNumber;
            student.Nationality = form.Nationality;
            student.BirthDate = form.BirthDate;
            student.Address = form.Address;
            student.PhoneNumber = form.PhoneNumber;
            student.Department = form.Department;
            student.AcademicYear = form.AcademicYear;
            student.PreviousInternshipDays = form.PreviousInternshipDays;
            student.HealthAidStatus = form.HealthAidStatus;

            database.SaveChanges();

            return Ok("Öğrenci bilgileri kaydedildi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                TempData["UploadMessage"] = "Lütfen bir dosya seçin.";
                return RedirectToAction(nameof(Index));
            }

            if (file.Length > MaxFileSize)
            {
                TempData["UploadMessage"] = "Dosya boyutu en fazla 10 MB olabilir.";
                return RedirectToAction(nameof(Index));
            }

            // İçerik doğrulaması başarısızsa dosyayı kaydetme veya Document oluşturmuyoruz.
            string? validationError =
                await fileValidator.ValidateAsync(file, cancellationToken);
            if (validationError is not null)
            {
                TempData["UploadMessage"] = validationError;
                return RedirectToAction(nameof(Index));
            }

            // TPT kalıtımında oturumdaki UserId, Student.Id ile aynıdır.
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            Student? student = await database.Students
                .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

            if (student is null)
            {
                TempData["UploadMessage"] = "Öğrenci kaydı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(student.StudentNumber))
            {
                TempData["UploadMessage"] = "Önce öğrenci bilgilerinizi doldurun.";
                return RedirectToAction(nameof(Index));
            }

            // Kullanıcının dosya adını fiziksel yolda kullanmıyor, GUID ile çakışmayı önlüyoruz.
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string storedFileName = $"{Guid.NewGuid():N}{extension}";
            string documentsDirectory = Path.Combine(
                environment.ContentRootPath, "Uploads", "Documents");
            string temporaryPath = Path.Combine(
                documentsDirectory, $".{Guid.NewGuid():N}.uploading");
            string finalPath = Path.Combine(documentsDirectory, storedFileName);

            // Veritabanında Windows/Mac'e özgü mutlak yol yerine taşınabilir göreli yol tutuyoruz.
            string relativePath = $"Uploads/Documents/{storedFileName}";
            Document? document = null;

            try
            {
                Directory.CreateDirectory(documentsDirectory);

                // Önce geçici dosyaya tam yaz; yarım yükleme nihai dosya adıyla görünmesin.
                await using (FileStream output = new(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await file.CopyToAsync(output, cancellationToken);
                }

                // Dosya tamamlandıktan sonra aynı klasörde nihai GUID adına taşı.
                System.IO.File.Move(temporaryPath, finalPath);

                document = new Document
                {
                    StudentId = student.Id,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    FilePath = relativePath,
                    UploadedAt = DateTime.UtcNow,
                    Status = "Uploaded"
                };

                database.Documents.Add(document);

                // Belge satırı başarısız kaydedilirse aşağıdaki telafi adımı dosyayı temizler.
                await database.SaveChangesAsync(cancellationToken);

                TempData["UploadMessage"] = "Ek-16 dosyası başarıyla yüklendi.";
            }
            catch (OperationCanceledException)
            {
                TryDeleteUploadFile(temporaryPath);
                TryDeleteUploadFile(finalPath);
                throw;
            }
            catch (Exception exception)
            {
                if (document is not null &&
                    database.Entry(document).State != EntityState.Detached)
                {
                    database.Entry(document).State = EntityState.Detached;
                }

                // Disk yazımı veya DB kaydı başarısızsa yarım kalan dosyaları temizle.
                TryDeleteUploadFile(temporaryPath);
                TryDeleteUploadFile(finalPath);
                logger.LogError(exception, "Ek-16 yükleme veya belge kaydı başarısız oldu.");

                TempData["UploadMessage"] =
                    "Dosya yüklenemedi veya belge kaydı oluşturulamadı. Lütfen tekrar deneyin.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void TryDeleteUploadFile(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch (Exception exception)
            {
                // Temizlik hatası da loglanır; dosya yolu/istisna kullanıcıya gösterilmez.
                logger.LogError(exception, "Yükleme artığı silinemedi: {Path}", path);
            }
        }
    }
}
