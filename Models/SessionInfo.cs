namespace StajYonetimSistemi.Models
{
    // Oturumda gösterilecek kullanıcı bilgilerini tutma
    public class SessionInfo
    {
        private int userId;
        private string role = "";

        // Kullanıcı kimliğini tutma
        public int UserId
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

        // Kullanıcı rolünü tutma
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
    }
}
