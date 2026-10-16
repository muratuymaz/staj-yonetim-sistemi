using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StajYonetimSistemi.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// appsettings.json dosyasındaki veritabanı bağlantı bilgisini okuma
string connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection", "");

// Bağlantı bilgisinin eksik olup olmadığını kontrol etme
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("appsettings.json dosyasında DefaultConnection bağlantı bilgisi bulunamadı.");
}

SqliteConnectionStringBuilder sqliteConnection = new SqliteConnectionStringBuilder(connectionString);

// Veritabanı yolunu Mac ve Windows'ta kullanılacak tam dosya yoluna çevirme
string relativeDatabasePath = sqliteConnection.DataSource;
string projectPath = builder.Environment.ContentRootPath;
string databasePath = Path.GetFullPath(relativeDatabasePath, projectPath);

sqliteConnection.DataSource = databasePath;
string fullConnectionString = sqliteConnection.ToString();

// MVC desteğini ekleme
builder.Services.AddControllersWithViews();

// StajDbContext için SQLite bağlantısını ayarlama
builder.Services.AddDbContext<StajDbContext>(options =>
{
    options.UseSqlite(fullConnectionString);
});

WebApplication app = builder.Build();

// İstekleri controller ve action adına göre yönlendirme
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
