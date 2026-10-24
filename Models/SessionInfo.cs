namespace StajYonetimSistemi.Models
{
    // Oturumda gösterilecek kullanıcı bilgilerini tutma
    public class SessionInfo
    {
        private int userId;
        private string role = "";

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
