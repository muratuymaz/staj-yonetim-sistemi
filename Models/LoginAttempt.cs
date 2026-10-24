namespace StajYonetimSistemi.Models;

public class LoginAttempt
{
    private int id;
    private Nullable<int> userId;
    private string userNameOrEmail = "";
    private DateTime attemptedAt = DateTime.UtcNow;
    private string failureReason = "";

    public int Id
    {
        get
        {
            return id;
        }

        set
        {
            id = value;
        }
    }

    // Hesap bulunamadığında kullanıcı bağlantısını boş bırakma
    public Nullable<int> UserId
    {
        get
        {
            return userId;
        }

        set
        {
            userId = value;
        }
    }

    // Başarısız giriş bilgilerini tutma
    public string UserNameOrEmail
    {
        get
        {
            return userNameOrEmail;
        }

        set
        {
            userNameOrEmail = value;
        }
    }

    public DateTime AttemptedAt
    {
        get
        {
            return attemptedAt;
        }

        set
        {
            attemptedAt = value;
        }
    }

    public string FailureReason
    {
        get
        {
            return failureReason;
        }

        set
        {
            failureReason = value;
        }
    }
}
