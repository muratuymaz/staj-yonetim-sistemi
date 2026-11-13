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
        private readonly DocumentReaderService documentReaderService;
        private readonly Ek16FieldExtractor ek16FieldExtractor;
        private readonly DocumentFieldComparisonService documentFieldComparisonService;
        private readonly ILogger<StudentController> logger;

        private const long MaxFileSize = 10 * 1024 * 1024;

        public StudentController(
            StajDbContext database,
            IWebHostEnvironment environment,
            DocumentFileValidator fileValidator,
            DocumentReaderService documentReaderService,
            Ek16FieldExtractor ek16FieldExtractor,
            DocumentFieldComparisonService documentFieldComparisonService,
            ILogger<StudentController> logger)
        {
            this.database = database;
            this.environment = environment;
            this.fileValidator = fileValidator;
            this.documentReaderService = documentReaderService;
            this.ek16FieldExtractor = ek16FieldExtractor;
            this.documentFieldComparisonService = documentFieldComparisonService;
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

            // Son yüklemede girilen staj bilgilerini getirme
            Document? lastDocument = database.Documents
                .Where(document => document.StudentId == userId)
                .OrderByDescending(document => document.UploadedAt)
                .ThenByDescending(document => document.Id)
                .FirstOrDefault();

            if (lastDocument != null)
            {
                profile.InternshipStartDate = lastDocument.InternshipStartDate.GetValueOrDefault();
                profile.InternshipEndDate = lastDocument.InternshipEndDate.GetValueOrDefault();
                profile.InternshipDays = lastDocument.WeeklyWorkDays
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            return Json(profile);
        }

        // Son Ek-16 belgesinin durumunu ve alan karşılaştırmasını öğrenciye gösterme
        [HttpGet]
        public async Task<IActionResult> Report(CancellationToken cancellationToken)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));
            Student? student = await database.Students.FindAsync(new object[] { userId }, cancellationToken);
            Document? document = await database.Documents
                .Where(item => item.StudentId == userId)
                .OrderByDescending(item => item.UploadedAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (student == null || document == null)
                return Json(new { hasDocument = false });

            DocumentReadResult? readResult = await database.DocumentReadResults
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.DocumentId == document.Id, cancellationToken);

            string? extractedStudentNumber = string.IsNullOrWhiteSpace(readResult?.StudentNumber)
                ? null
                : readResult.StudentNumber.Trim();

            TestStudent? referenceStudent = null;
            string referenceLookupStatus;

            if (extractedStudentNumber is null)
            {
                referenceLookupStatus = "studentNumberUnreadable";
            }
            else
            {
                referenceStudent = await database.TestStudents
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item => item.StudentNumber == extractedStudentNumber,
                        cancellationToken);

                referenceLookupStatus = referenceStudent is null
                    ? "notFound"
                    : "found";
            }

            List<FieldComparisonResult>? comparisons = null;
            if (readResult != null)
            {
                StudentProfileForm savedForm = CreateSavedForm(student, document);
                comparisons = documentFieldComparisonService.Compare(readResult, savedForm);
            }

            List<FieldComparisonResult>? referenceComparisons = null;
            if (readResult != null && referenceStudent != null)
            {
                referenceComparisons = documentFieldComparisonService.CompareWithReference(
                    readResult,
                    referenceStudent);
            }

            return Json(new
            {
                hasDocument = true,
                document.OriginalFileName,
                document.UploadedAt,
                document.Status,
                fieldComparisons = comparisons,
                referenceLookupStatus,
                referenceStudentNumber = referenceStudent?.StudentNumber,
                referenceFieldComparisons = referenceComparisons
            });
        }

        // Öğrenci formunu ve Ek-16 dosyasını birlikte kaydetme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            [FromForm] StudentProfileForm form,
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            // Ek-16 üzerindeki altı sağlık yardımı maddesinden birini seçtirme
            if (form.HealthAidStatus < 1 || form.HealthAidStatus > 6)
            {
                return BadRequest("Ek-16'daki sağlık yardımı maddelerinden birini seçin.");
            }

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

            if (form.InternshipStartDate == DateTime.MinValue ||
                form.InternshipEndDate == DateTime.MinValue)
            {
                return BadRequest("Staj başlangıç ve bitiş tarihlerini girin.");
            }

            if (form.InternshipEndDate < form.InternshipStartDate)
            {
                return BadRequest("Staj bitiş tarihi başlangıç tarihinden önce olamaz.");
            }

            // Seçilen günleri kontrol edip sıraya koyma
            List<string> allowedDays = new List<string>
            {
                "monday", "tuesday", "wednesday", "thursday", "friday"
            };
            List<string> selectedDays = new List<string>();

            if (form.InternshipDays == null || form.InternshipDays.Count == 0)
            {
                return BadRequest("En az bir staj günü seçin.");
            }

            foreach (string day in form.InternshipDays)
            {
                if (string.IsNullOrWhiteSpace(day))
                {
                    return BadRequest("Seçilen staj günü geçersiz.");
                }

                string selectedDay = day.Trim().ToLowerInvariant();
                if (allowedDays.Contains(selectedDay) == false)
                {
                    return BadRequest("Seçilen staj günü geçersiz.");
                }

                selectedDays.Add(selectedDay);
            }

            List<string> orderedDays = allowedDays
                .Where(day => selectedDays.Contains(day))
                .ToList();
            string weeklyWorkDays = string.Join(",", orderedDays);

            // Dosyayı bilgiler kaydedilmeden önce kontrol etme
            if (file == null || file.Length == 0)
            {
                return BadRequest("Bilgi formuyla birlikte Ek-16 dosyasını seçin.");
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest("Dosya boyutu en fazla 10 MB olabilir.");
            }

            string? validationError = await fileValidator.ValidateAsync(file, cancellationToken);
            if (validationError != null)
            {
                return BadRequest(validationError);
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

            // Tamamlanan başvuruyu öğrenci tarafından değiştirmeme
            bool hasCompletedDocument = await database.Documents.AnyAsync(item =>
                item.StudentId == userId && item.Status == "Completed", cancellationToken);
            if (hasCompletedDocument)
                return Conflict("Tamamlanan Ek-16 belgesi öğrenci tarafından değiştirilemez.");

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

                document = new Document();
                document.StudentId = student.Id;
                document.OriginalFileName = Path.GetFileName(file.FileName);
                document.FilePath = relativePath;
                document.UploadedAt = DateTime.UtcNow;
                document.Status = "Uploaded";
                document.InternshipStartDate = form.InternshipStartDate;
                document.InternshipEndDate = form.InternshipEndDate;
                document.WeeklyWorkDays = weeklyWorkDays;

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

                database.Documents.Add(document);

                // Profil ve belgeyi tek veritabanı işlemiyle kaydetme
                await database.SaveChangesAsync(cancellationToken);
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

                return StatusCode(500,
                    "Bilgiler ve dosya kaydedilemedi. Lütfen tekrar deneyin.");
            }

            string extractedText = "";
            string? extractionWarning = null;
            DocumentReadResult? extractedFields = null;
            List<FieldComparisonResult>? fieldComparisons = null;
            if (extension is ".docx" or ".pdf")
            {
                try
                {
                    extractedText = documentReaderService.ReadText(finalPath);
                    extractedFields = ek16FieldExtractor.Extract(extractedText, document.Id);
                    database.DocumentReadResults.Add(extractedFields);
                    await database.SaveChangesAsync(cancellationToken);
                    fieldComparisons = documentFieldComparisonService.Compare(extractedFields, form);

                    // Tüm okunabilen alanlar eşleşmeden belgeyi onaylamama
                    document.Status = fieldComparisons.All(item =>
                        item.Status == FieldComparisonStatus.Match)
                        ? "Approved"
                        : "Incomplete";
                    await database.SaveChangesAsync(cancellationToken);

                    if (string.IsNullOrWhiteSpace(extractedText))
                        extractionWarning = "Belge kaydedildi ancak okunabilir metin bulunamadı.";
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // Okuma hatası, başarıyla kaydedilmiş yüklemeyi başarısız göstermesin.
                    logger.LogWarning(
                        exception,
                        "Yüklenen belgeden alanlar çıkarılamadı. Belge kimliği: {DocumentId}",
                        document.Id);
                    extractionWarning = "Belge kaydedildi ancak metin alanları çıkarılamadı.";
                }
            }

            // Otomatik okuma yapılamayan belgeleri admin incelemesine bırakma
            if (document.Status == "Uploaded")
            {
                document.Status = "Reviewing";
                await database.SaveChangesAsync(cancellationToken);
            }

            return Ok(new
            {
                message = "Öğrenci bilgileri ve Ek-16 dosyası birlikte kaydedildi.",
                extractedFields,
                fieldComparisons,
                documentStatus = document.Status,
                extractionWarning
            });
        }

        private static StudentProfileForm CreateSavedForm(Student student, Document document)
        {
            return new StudentProfileForm
            {
                Email = student.Email,
                StudentNumber = student.StudentNumber,
                FullName = student.FullName,
                IdentityNumber = student.IdentityNumber,
                Nationality = student.Nationality,
                BirthDate = student.BirthDate,
                Address = student.Address,
                PhoneNumber = student.PhoneNumber,
                Department = student.Department,
                AcademicYear = student.AcademicYear,
                PreviousInternshipDays = student.PreviousInternshipDays,
                HealthAidStatus = student.HealthAidStatus,
                InternshipStartDate = document.InternshipStartDate.GetValueOrDefault(),
                InternshipEndDate = document.InternshipEndDate.GetValueOrDefault(),
                InternshipDays = document.WeeklyWorkDays
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            };
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
