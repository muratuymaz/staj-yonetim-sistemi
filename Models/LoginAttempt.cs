namespace StajYonetimSistemi.Models;

public class LoginAttempt
{
    public int Id { get; set; }

    // Hesap bulunamadığında kullanıcı bağlantısını boş bırakma
    public Nullable<int> UserId { get; set; }

    // Başarısız giriş bilgilerini tutma
    public string UserNameOrEmail { get; set; } = "";
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public string FailureReason { get; set; } = "";
}
