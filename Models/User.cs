namespace StajYonetimSistemi.Models;

public class User
{
    public int Id { get; set; }

    // Giriş için kullanılacak hesap bilgilerini tutma
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";

    // Admin veya Student rolünü tutma
    public string Role { get; set; } = "Student";
}
