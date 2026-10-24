using Microsoft.AspNetCore.Identity;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Services;

public class PasswordService
{
    private PasswordHasher<User> passwordHasher;

    // Şifre işlemleri için nesne oluşturma
    public PasswordService()
    {
        passwordHasher = new PasswordHasher<User>();
    }

    // Şifreyi hash haline getirme
    public string HashPassword(User user, string password)
    {
        string passwordHash = passwordHasher.HashPassword(user, password);
        return passwordHash;
    }

    // Girilen şifreyi kayıtlı hash ile karşılaştırma
    public bool VerifyPassword(User user, string password)
    {
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return false;
        }

        PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            return false;
        }

        // Gerekirse eski hash değerini güncelleme
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = HashPassword(user, password);
        }

        return true;
    }
}
