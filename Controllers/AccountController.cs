using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using StajYonetimSistemi.Data;
using StajYonetimSistemi.Models;
using StajYonetimSistemi.Services;

namespace StajYonetimSistemi.Controllers;

public class AccountController : Controller
{
    private readonly StajDbContext database;
    private readonly PasswordService passwordService;

    public AccountController(StajDbContext database, PasswordService passwordService)
    {
        this.database = database;
        this.passwordService = passwordService;
    }

    // Giriş formu için güvenlik doğrulama değeri oluşturma
    [HttpGet]
    public IActionResult Login([FromServices] IAntiforgery antiforgery)
    {
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Json(new { requestToken = tokens.RequestToken });
    }

    // Kullanıcı adı veya e-posta ile giriş yapma
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login([FromForm] string userName, [FromForm] string password)
    {
        // Boş bırakılan giriş bilgilerini kontrol etme
        if (string.IsNullOrWhiteSpace(userName))
        {
            return BadRequest("Kullanıcı adı veya e-posta gerekli.");
        }

        if (string.IsNullOrEmpty(password))
        {
            return BadRequest("Şifre gerekli.");
        }

        // Kullanıcı adı veya e-postası eşleşen hesabı bulma
        List<User> users = database.Users
            .Where(account => account.UserName == userName || account.Email == userName)
            .ToList();

        // Tek bir hesap bulunmadıysa girişi reddetme
        if (users.Count != 1)
        {
            return Unauthorized("Kullanıcı adı/e-posta veya şifre hatalı.");
        }

        User user = users[0];

        if (user.IsLocked == true)
        {
            return Unauthorized("Hesap kilitli.");
        }

        bool passwordIsCorrect = passwordService.VerifyPassword(user, password);

        if (passwordIsCorrect == false)
        {
            return Unauthorized("Kullanıcı adı/e-posta veya şifre hatalı.");
        }

        // Şifre servisi hash değerini yenilediyse kaydetme
        database.SaveChanges();

        // Kullanıcı kimliğini ve rolünü oturuma kaydetme
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok("Giriş başarılı.");
    }

    // Oturumdaki kullanıcı bilgisini görüntüleme
    [HttpGet]
    public IActionResult Current()
    {
        // Oturumda kullanıcı yoksa kimlik değerini 0 kabul etme
        int userId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));

        if (userId == 0)
        {
            return Unauthorized("Giriş yapılmamış.");
        }

        return Json(new { userId = userId, role = HttpContext.Session.GetString("Role") });
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
}
