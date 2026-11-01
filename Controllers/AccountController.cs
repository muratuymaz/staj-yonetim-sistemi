using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StajYonetimSistemi.Data;
using StajYonetimSistemi.Filters;
using StajYonetimSistemi.Models;
using StajYonetimSistemi.Services;

namespace StajYonetimSistemi.Controllers
{
    public class AccountController : Controller
    {
        private StajDbContext database;
        private PasswordService passwordService;

        public AccountController(StajDbContext database, PasswordService passwordService)
        {
            this.database = database;
            this.passwordService = passwordService;
        }

        // Giriş sayfasını görüntüleme
        [HttpGet]
        public IActionResult Login(
            [FromQuery] bool registered = false,
            [FromQuery] bool unauthorized = false)
        {
            if (registered)
            {
                ViewBag.LoginMessage = "Kayıt başarılı. Bilgilerinizle giriş yapabilirsiniz.";
            }

            if (unauthorized)
            {
                ViewBag.UnauthorizedMessage =
                    "Yetkisiz işlem: Bu sayfaya erişmek için giriş yapmanız veya uygun role sahip olmanız gerekiyor.";
            }

            return View();
        }

        // Kayıt sayfasını görüntüleme
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Yeni öğrenciyi ortak hesap bilgileriyle kaydetme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(
            [FromForm] string userName,
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] string passwordConfirmation)
        {
            if (string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(passwordConfirmation))
            {
                return BadRequest("Tüm alanları doldurmanız gerekiyor.");
            }

            // Kullanıcı adı ve e-postanın kenar boşluklarını temizleme
            userName = userName.Trim();
            email = email.Trim();

            // E-posta biçimini kontrol etme
            EmailAddressAttribute emailValidator = new EmailAddressAttribute();
            bool emailIsValid = emailValidator.IsValid(email);

            // E-postanın içinde boşluk olmadığını kontrol etme
            foreach (char character in email)
            {
                if (char.IsWhiteSpace(character))
                {
                    emailIsValid = false;
                    break;
                }
            }

            if (emailIsValid == false)
            {
                return BadRequest("Geçerli bir e-posta adresi girmeniz gerekiyor.");
            }

            if (password != passwordConfirmation)
            {
                return BadRequest("Şifreler eşleşmiyor.");
            }

            bool userNameExists = database.Users.Any(user => user.UserName == userName);
            bool emailExists = database.Users.Any(user => user.Email == email);

            if (userNameExists || emailExists)
            {
                return Conflict("Kullanıcı adı veya e-posta zaten kayıtlı.");
            }

            Student student = new Student();
            student.UserName = userName;
            student.Email = email;
            student.Role = "Student";
            student.PasswordHash = passwordService.HashPassword(student, password);

            database.Students.Add(student);
            database.SaveChanges();

            return Ok("Kayıt başarılı.");
        }

        // Kullanıcı adı veya e-posta ile giriş yapma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login([FromForm] string userName, [FromForm] string password)
        {
            // Boş bırakılan giriş bilgilerini kontrol etme
            if (string.IsNullOrWhiteSpace(userName))
            {
                RecordFailedLogin("", null, "Kullanıcı adı/e-posta boş.");
                return BadRequest("Kullanıcı adı veya e-posta gerekli.");
            }

            if (string.IsNullOrEmpty(password))
            {
                RecordFailedLogin(userName, null, "Şifre boş.");
                return BadRequest("Şifre gerekli.");
            }

            // Kullanıcı adı veya e-postaya göre arama
            var userQuery = from account in database.Users
                            where account.UserName == userName || account.Email == userName
                            select account;

            List<User> matchingUsers = userQuery.ToList();

            // Tek bir hesap bulunmadıysa girişi reddetme
            if (matchingUsers.Count != 1)
            {
                RecordFailedLogin(userName, null, "Tek bir hesap eşleşmedi.");
                return Unauthorized("Kullanıcı adı/e-posta veya şifre hatalı.");
            }

            User user = matchingUsers[0];

            if (user.IsLocked == true)
            {
                RecordFailedLogin(userName, user.Id, "Hesap kilitli.");
                return Unauthorized("Hesap kilitli.");
            }

            bool passwordIsCorrect = passwordService.VerifyPassword(user, password);

            if (passwordIsCorrect == false)
            {
                // Hatalı giriş sayısını artırma
                user.FailedLoginCount = user.FailedLoginCount + 1;

                // Beş hatalı denemede hesabı kilitleme
                if (user.FailedLoginCount >= 5)
                {
                    user.IsLocked = true;
                }

                RecordFailedLogin(userName, user.Id, "Şifre hatalı.");

                if (user.IsLocked == true)
                {
                    return Unauthorized("Hesap kilitli.");
                }

                return Unauthorized("Kullanıcı adı/e-posta veya şifre hatalı.");
            }

            // Sayacı sıfırlama ve değişiklikleri kaydetme
            user.FailedLoginCount = 0;
            database.SaveChanges();

            // Kullanıcı kimliğini ve rolünü oturuma kaydetme
            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Role", user.Role);

            return Ok("Giriş başarılı.");
        }

        // Oturumdaki kullanıcı bilgisini görüntüleme
        [HttpGet]
        [SessionAuthorize]
        public IActionResult Current()
        {
            // Oturum bilgilerini hazırlama
            SessionInfo info = new SessionInfo();
            info.UserId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));

            var role = HttpContext.Session.GetString("Role");

            if (role != null)
            {
                info.Role = role;
            }

            return Json(info);
        }

        // Oturum bilgilerini temizleyerek çıkış yapma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".Staj.Session");

            return Ok("Çıkış yapıldı.");
        }

        // Başarısız giriş denemesini kaydetme
        // Hesap bulunamadığında userId değerini boş bırakma
        private void RecordFailedLogin(string userName, Nullable<int> userId, string reason)
        {
            LoginAttempt attempt = new LoginAttempt();
            attempt.UserNameOrEmail = userName;
            attempt.UserId = userId;
            attempt.FailureReason = reason;

            database.LoginAttempts.Add(attempt);
            database.SaveChanges();
        }
    }
}
