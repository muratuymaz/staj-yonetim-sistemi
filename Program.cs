using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StajYonetimSistemi.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// appsettings.json dosyasındaki veritabanı bağlantı bilgisini okuma
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Bağlantı bilgisinin eksik olup olmadığını kontrol etme
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("appsettings.json dosyasında DefaultConnection bağlantı bilgisi bulunamadı.");
}

SqliteConnectionStringBuilder sqliteConnection = new SqliteConnectionStringBuilder(connectionString);

// Veritabanı yolunu Mac ve Windows'ta kullanılacak tam dosya yoluna çevirme
string databasePath = Path.GetFullPath(
    sqliteConnection.DataSource,
    builder.Environment.ContentRootPath);

sqliteConnection.DataSource = databasePath;

// MVC desteğini ekleme
builder.Services.AddControllersWithViews();

// AppDbContext için SQLite bağlantısını ayarlama
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(sqliteConnection.ToString());
});

WebApplication app = builder.Build();

// İstekleri controller ve action adına göre yönlendirme
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
