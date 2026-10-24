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
                context.Result = new UnauthorizedResult();
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
                context.Result = new UnauthorizedResult();
                return;
            }

            // Kilitli hesabın oturumunu temizleme
            if (user.IsLocked == true)
            {
                session.Clear();
                context.Result = new UnauthorizedResult();
                return;
            }

            // Rol değişmişse yeniden giriş isteme
            var role = session.GetString("Role");

            if (role != user.Role)
            {
                session.Clear();
                context.Result = new UnauthorizedResult();
                return;
            }

            // Farklı role ait işlemlere erişimi engelleme
            if (requiredRole == "")
            {
                return;
            }

            if (user.Role != requiredRole)
            {
                context.Result = new StatusCodeResult(403);
            }
        }
    }
}
