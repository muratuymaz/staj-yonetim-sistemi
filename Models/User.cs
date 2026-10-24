namespace StajYonetimSistemi.Models;

public class User
{
    private int id;
    private string userName = "";
    private string email = "";
    private string passwordHash = "";
    private string role = "Student";
    private int failedLoginCount;
    private bool isLocked;

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

    // Giriş için kullanılacak hesap bilgilerini tutma
    public string UserName
    {
        get
        {
            return userName;
        }

        set
        {
            userName = value;
        }
    }

    public string Email
    {
        get
        {
            return email;
        }

        set
        {
            email = value;
        }
    }

    public string PasswordHash
    {
        get
        {
            return passwordHash;
        }

        set
        {
            passwordHash = value;
        }
    }

    // Admin veya Student rolünü tutma
    public string Role
    {
        get
        {
            return role;
        }

        set
        {
            role = value;
        }
    }

    // Hatalı giriş sayısını tutma
    public int FailedLoginCount
    {
        get
        {
            return failedLoginCount;
        }

        set
        {
            failedLoginCount = value;
        }
    }

    // Hesabın kilit durumunu tutma
    public bool IsLocked
    {
        get
        {
            return isLocked;
        }

        set
        {
            isLocked = value;
        }
    }

    // Yeni kullanıcının başlangıç değerlerini belirleme
    public User()
    {
        UserName = "";
        Email = "";
        PasswordHash = "";
        Role = "Student";
        FailedLoginCount = 0;
        IsLocked = false;
    }
}
