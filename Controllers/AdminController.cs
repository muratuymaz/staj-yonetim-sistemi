using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using StajYonetimSistemi.Data;
using StajYonetimSistemi.Filters;
using StajYonetimSistemi.Models;
using StajYonetimSistemi.Services;

namespace StajYonetimSistemi.Controllers
{
    [SessionAuthorize("Admin")]
    public class AdminController : Controller
    {
        private readonly StajDbContext database;
        private readonly StudentDatasetImportService datasetImportService;
        private readonly DocumentFieldComparisonService comparisonService;
        private readonly IWebHostEnvironment environment;
        private readonly ILogger<AdminController> logger;

        public AdminController(
            StajDbContext database,
            StudentDatasetImportService datasetImportService,
            DocumentFieldComparisonService comparisonService,
            IWebHostEnvironment environment,
            ILogger<AdminController> logger)
        {
            this.database = database;
            this.datasetImportService = datasetImportService;
            this.comparisonService = comparisonService;
            this.environment = environment;
            this.logger = logger;
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

            // CSV referansları ile kendi hesabını oluşturan öğrencileri ayrı gösterme
            AdminDashboardViewModel model = new AdminDashboardViewModel();
            model.TestStudents = await database.TestStudents
                .AsNoTracking()
                .OrderBy(student => student.FullName)
                .ThenBy(student => student.StudentNumber)
                .ToListAsync(cancellationToken);
            model.Students = students
                .Select(student => CreateStudentRow(student, latestDocuments))
                .ToList();

            return View(model);
        }

        // Admin tarafından test veri setini referans tablosuna aktarma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportDataset(
            IFormFile? datasetFile,
            CancellationToken cancellationToken)
        {
            StudentDatasetImportResult result = await datasetImportService.ImportAsync(
                datasetFile,
                cancellationToken);

            if (result.IsSuccess)
            {
                TempData["DatasetMessage"] =
                    $"CSV referans verisi kaydedildi. Yeni: {result.AddedCount}, güncellenen referans: {result.UpdatedCount}.";
                TempData["DatasetMessageType"] = "success";
            }
            else
            {
                string fileName = Path.GetFileName(datasetFile?.FileName ?? "CSV");
                TempData["DatasetMessage"] = $"{fileName}: {string.Join(" ", result.Errors.Take(5))}";
                TempData["DatasetMessageType"] = "error";
            }

            return RedirectToAction(nameof(Index));
        }

        // Seçilen öğrencinin kayıt, referans ve belge karşılaştırmasını getirme
        [HttpGet]
        public async Task<IActionResult> StudentDetails(int studentId, CancellationToken cancellationToken)
        {
            Student? student = await database.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == studentId, cancellationToken);
            if (student == null)
                return NotFound();

            Document? document = await LatestDocument(studentId, cancellationToken);
            DocumentReadResult? readResult = document == null ? null : await database.DocumentReadResults
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.DocumentId == document.Id, cancellationToken);
            TestStudent? reference = await database.TestStudents
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.StudentNumber == student.StudentNumber, cancellationToken);

            StudentProfileForm profile = CreateSavedForm(student, document);
            List<FieldComparisonResult>? referenceComparisons = reference == null
                ? null
                : comparisonService.CompareStudentWithReference(student, reference);
            List<FieldComparisonResult>? documentComparisons = readResult == null || document == null
                ? null
                : comparisonService.Compare(readResult, profile);
            bool canComplete = document?.Status == "Approved" &&
                AllFieldsMatch(referenceComparisons) && AllFieldsMatch(documentComparisons);

            (string statusKey, string statusText) = GetDocumentStatus(document);
            return Json(new
            {
                studentId,
                student.UserName,
                profile,
                documentId = document?.Id,
                documentName = document?.OriginalFileName,
                documentStatus = statusText,
                documentStatusKey = statusKey,
                referenceFound = reference != null,
                referenceComparisons,
                documentComparisons,
                canComplete
            });
        }

        // Adminin öğrenci profilini ve son belgedeki staj bilgilerini güncellemesi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStudent(
            int studentId,
            [FromForm] StudentProfileForm form,
            CancellationToken cancellationToken)
        {
            Student? student = await database.Students.FindAsync(new object[] { studentId }, cancellationToken);
            if (student == null)
                return NotFound("Öğrenci bulunamadı.");

            if (!ModelState.IsValid || form.BirthDate == DateTime.MinValue ||
                form.BirthDate > DateTime.Today || form.HealthAidStatus is < 1 or > 6)
                return BadRequest("Öğrenci bilgilerini kontrol edin.");

            form.FullName = form.FullName.Trim();
            form.StudentNumber = form.StudentNumber.Trim();
            form.IdentityNumber = form.IdentityNumber.Trim();
            form.Nationality = form.Nationality.Trim();
            form.Address = form.Address.Trim();
            form.Department = form.Department.Trim();
            form.AcademicYear = form.AcademicYear.Trim();
            form.Email = form.Email.Trim();
            form.PhoneNumber = form.PhoneNumber.Trim();

            if (new EmailAddressAttribute().IsValid(form.Email) == false ||
                new[] { form.FullName, form.StudentNumber, form.IdentityNumber, form.Nationality,
                    form.Address, form.Department, form.AcademicYear, form.Email, form.PhoneNumber }
                    .Any(string.IsNullOrWhiteSpace))
                return BadRequest("Zorunlu alanları eksiksiz doldurun.");

            bool duplicateStudent = await database.Students.AnyAsync(item =>
                item.Id != studentId &&
                (item.StudentNumber == form.StudentNumber || item.IdentityNumber == form.IdentityNumber),
                cancellationToken);
            bool duplicateEmail = await database.Users.AnyAsync(item =>
                item.Id != studentId && item.Email == form.Email, cancellationToken);
            if (duplicateStudent || duplicateEmail)
                return Conflict("Öğrenci no, kimlik no veya e-posta başka bir hesaba ait.");

            Document? document = await LatestDocument(studentId, cancellationToken);
            if (document != null)
            {
                if (form.InternshipStartDate == DateTime.MinValue ||
                    form.InternshipEndDate < form.InternshipStartDate)
                    return BadRequest("Staj tarihlerini kontrol edin.");

                string[] allowedDays = ["monday", "tuesday", "wednesday", "thursday", "friday"];
                if (form.InternshipDays == null || form.InternshipDays.Count == 0 ||
                    form.InternshipDays.Any(day => !allowedDays.Contains(day)))
                    return BadRequest("En az bir geçerli staj günü seçin.");

                document.InternshipStartDate = form.InternshipStartDate;
                document.InternshipEndDate = form.InternshipEndDate;
                document.WeeklyWorkDays = string.Join(",", allowedDays.Where(form.InternshipDays.Contains));
            }

            student.FullName = form.FullName;
            student.StudentNumber = form.StudentNumber;
            student.IdentityNumber = form.IdentityNumber;
            student.Nationality = form.Nationality;
            student.BirthDate = form.BirthDate;
            student.Address = form.Address;
            student.Department = form.Department;
            student.AcademicYear = form.AcademicYear;
            student.PreviousInternshipDays = form.PreviousInternshipDays;
            student.Email = form.Email;
            student.PhoneNumber = form.PhoneNumber;
            student.HealthAidStatus = form.HealthAidStatus;

            // Güncellemeden sonra belgenin eski onayına güvenmeme
            if (document != null)
            {
                DocumentReadResult? readResult = await database.DocumentReadResults
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.DocumentId == document.Id, cancellationToken);
                TestStudent? reference = await database.TestStudents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.StudentNumber == student.StudentNumber, cancellationToken);
                bool documentMatches = readResult != null &&
                    AllFieldsMatch(comparisonService.Compare(readResult, form));
                bool referenceMatches = reference != null &&
                    AllFieldsMatch(comparisonService.CompareStudentWithReference(student, reference));

                document.Status = !documentMatches ? (readResult == null ? "Reviewing" : "Incomplete")
                    : document.Status == "Completed" && referenceMatches ? "Completed" : "Approved";
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Conflict("Öğrenci bilgileri kaydedilemedi. Tekrarlanan alanları kontrol edin.");
            }

            return Ok(new { message = "Öğrenci bilgileri kaydedildi." });
        }

        // Tamamlananlar dahil öğrenci hesabını ve bağlı belgelerini silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(int studentId, CancellationToken cancellationToken)
        {
            Student? student = await database.Students.FindAsync(new object[] { studentId }, cancellationToken);
            if (student == null)
                return NotFound("Öğrenci bulunamadı.");

            List<Document> documents = await database.Documents
                .Where(item => item.StudentId == studentId)
                .ToListAsync(cancellationToken);

            database.Documents.RemoveRange(documents);
            database.Students.Remove(student);
            await database.SaveChangesAsync(cancellationToken);

            // Veritabanı kaydı silindikten sonra yüklenen dosyaları da temizleme
            string documentsDirectory = Path.GetFullPath(Path.Combine(
                environment.ContentRootPath, "Uploads", "Documents"));
            foreach (Document document in documents)
            {
                string filePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath,
                    document.FilePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!string.Equals(Path.GetDirectoryName(filePath), documentsDirectory,
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning(exception, "Silinen öğrenciye ait dosya temizlenemedi: {Path}", filePath);
                }
            }

            return Ok(new { message = "Öğrenci kaydı ve belgeleri silindi." });
        }

        // Onaylanan son belgeyi admin tarafından tamamlandı durumuna geçirme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteDocument(int documentId, CancellationToken cancellationToken)
        {
            Document? document = await database.Documents.FindAsync(
                new object[] { documentId }, cancellationToken);
            if (document == null)
                return NotFound();

            bool hasNewerDocument = await database.Documents.AnyAsync(item =>
                item.StudentId == document.StudentId && item.Id != document.Id &&
                (item.UploadedAt > document.UploadedAt ||
                 (item.UploadedAt == document.UploadedAt && item.Id > document.Id)), cancellationToken);

            if (hasNewerDocument || document.Status != "Approved")
                return BadRequest("Yalnızca öğrencinin son onaylanan belgesi tamamlanabilir.");

            Student? student = await database.Students.FindAsync(
                new object[] { document.StudentId }, cancellationToken);
            TestStudent? reference = student == null ? null : await database.TestStudents
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.StudentNumber == student.StudentNumber, cancellationToken);
            DocumentReadResult? readResult = await database.DocumentReadResults
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.DocumentId == document.Id, cancellationToken);

            if (student == null || reference == null || readResult == null ||
                !AllFieldsMatch(comparisonService.CompareStudentWithReference(student, reference)) ||
                !AllFieldsMatch(comparisonService.Compare(readResult, CreateSavedForm(student, document))))
                return BadRequest("Belge, öğrenci formu ve CSV referansı eşleşmeden tamamlanamaz.");

            document.Status = "Completed";
            await database.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Belge tamamlandı olarak işaretlendi." });
        }

        private Task<Document?> LatestDocument(int studentId, CancellationToken cancellationToken)
        {
            return database.Documents
                .Where(item => item.StudentId == studentId)
                .OrderByDescending(item => item.UploadedAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static bool AllFieldsMatch(List<FieldComparisonResult>? comparisons)
        {
            return comparisons != null && comparisons.All(item => item.Status == FieldComparisonStatus.Match);
        }

        private static StudentProfileForm CreateSavedForm(Student student, Document? document)
        {
            return new StudentProfileForm
            {
                FullName = student.FullName,
                StudentNumber = student.StudentNumber,
                IdentityNumber = student.IdentityNumber,
                Nationality = student.Nationality,
                BirthDate = student.BirthDate,
                Address = student.Address,
                Department = student.Department,
                AcademicYear = student.AcademicYear,
                PreviousInternshipDays = student.PreviousInternshipDays,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                HealthAidStatus = student.HealthAidStatus,
                InternshipStartDate = document?.InternshipStartDate ?? DateTime.MinValue,
                InternshipEndDate = document?.InternshipEndDate ?? DateTime.MinValue,
                InternshipDays = document?.WeeklyWorkDays
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? []
            };
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
                DocumentStatusKey = statusKey,
                DocumentId = document?.Id
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
