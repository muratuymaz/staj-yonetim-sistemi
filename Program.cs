using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StajYonetimSistemi.Data;
using StajYonetimSistemi.Services;

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

// Şifre işlemlerini kullanıma hazırlama
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<DocumentFileValidator>();
builder.Services.AddScoped<DocumentReaderService>();
builder.Services.AddScoped<Ek16FieldExtractor>();
builder.Services.AddScoped<StudentDatasetImportService>();

// Oturum bilgilerini sunucu belleğinde tutma
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".Staj.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// StajDbContext için SQLite bağlantısını ayarlama
builder.Services.AddDbContext<StajDbContext>(options =>
{
    options.UseSqlite(fullConnectionString);
});

WebApplication app = builder.Build();

// Yönlendirme sonrası oturumu kullanıma açma
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Ana sayfada giriş ekranını açma
app.MapControllerRoute(
    name: "login",
    pattern: "",
    defaults: new { controller = "Account", action = "Login" });

// İstekleri controller ve action adına göre yönlendirme
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Index}/{id?}");

app.Run();
