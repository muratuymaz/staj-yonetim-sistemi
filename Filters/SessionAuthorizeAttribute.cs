using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StajYonetimSistemi.Data;

namespace StajYonetimSistemi.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private string requiredRole;

        public SessionAuthorizeAttribute(string role = "")
        {
            requiredRole = role;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            ISession session = context.HttpContext.Session;
            int userId = Convert.ToInt32(session.GetInt32("UserId"));

            // Giriş yapılmamışsa erişimi engelleme
            if (userId == 0)
            {
                RedirectToLogin(context);
                return;
            }

            // Veritabanı bağlantısını alma
            IServiceProvider services = context.HttpContext.RequestServices;
            StajDbContext database = services.GetRequiredService<StajDbContext>();
            var user = database.Users.Find(userId);

            // Hesap bulunamadığında oturumu temizleme
            if (user == null)
            {
                session.Clear();
                RedirectToLogin(context);
                return;
            }

            // Kilitli hesabın oturumunu temizleme
            if (user.IsLocked == true)
            {
                session.Clear();
                RedirectToLogin(context);
                return;
            }

            // Rol değişmişse yeniden giriş isteme
            var role = session.GetString("Role");

            if (role != user.Role)
            {
                session.Clear();
                RedirectToLogin(context);
                return;
            }

            // Farklı role ait işlemlere erişimi engelleme
            if (requiredRole == "")
            {
                return;
            }

            if (user.Role != requiredRole)
            {
                RedirectToLogin(context);
            }
        }

        private static void RedirectToLogin(AuthorizationFilterContext context)
        {
            context.Result = new RedirectToActionResult(
                "Login",
                "Account",
                routeValues: new { unauthorized = true });
        }
    }
}
