namespace StajYonetimSistemi.Models
{
    // Oturumda gösterilecek kullanıcı bilgilerini tutma
    public class SessionInfo
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "";
    }
}
