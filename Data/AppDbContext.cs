using Microsoft.EntityFrameworkCore;

namespace StajYonetimSistemi.Data;

public class AppDbContext : DbContext
{
    // Program.cs içinde belirlenen bağlantı ayarlarını EF Core'a aktarma
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

}
