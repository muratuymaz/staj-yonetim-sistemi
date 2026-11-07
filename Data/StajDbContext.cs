using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StajYonetimSistemi.Models;

namespace StajYonetimSistemi.Data;

public class StajDbContext : DbContext
{
    // Bağlantı ayarlarını aktarma
    public StajDbContext(DbContextOptions<StajDbContext> options) : base(options)
    {
        Users = Set<User>();
        Students = Set<Student>();
        Documents = Set<Document>();
        LoginAttempts = Set<LoginAttempt>();
        TestStudents = Set<TestStudent>();
        DocumentReadResults = Set<DocumentReadResult>();
    }

    // Tabloları tanımlama
    public DbSet<User> Users { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<TestStudent> TestStudents { get; set; }
    public DbSet<DocumentReadResult> DocumentReadResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tablo ayarlarını hazırlama
        EntityTypeBuilder<User> userTable = modelBuilder.Entity<User>();
        EntityTypeBuilder<Student> studentTable = modelBuilder.Entity<Student>();
        EntityTypeBuilder<Document> documentTable = modelBuilder.Entity<Document>();

        // Ortak hesap ve öğrenci bilgilerini ayrı tablolarda tutma
        userTable.UseTptMappingStrategy();
        userTable.ToTable("Users");
        studentTable.ToTable("Students");

        // Kullanıcı bilgilerinin tekrarını engelleme
        userTable.Property(user => user.UserName).UseCollation("NOCASE");
        userTable.Property(user => user.Email).UseCollation("NOCASE");

        IndexBuilder userNameIndex = userTable.HasIndex(user => user.UserName);
        userNameIndex.IsUnique();

        IndexBuilder emailIndex = userTable.HasIndex(user => user.Email);
        emailIndex.IsUnique();

        // Profil doldurulduğunda öğrenci numaralarının tekrarını engelleme
        IndexBuilder studentNumberIndex = studentTable.HasIndex(student => student.StudentNumber);
        studentNumberIndex.IsUnique();
        studentNumberIndex.HasFilter("\"StudentNumber\" <> ''");

        IndexBuilder identityNumberIndex = studentTable.HasIndex(student => student.IdentityNumber);
        identityNumberIndex.IsUnique();
        identityNumberIndex.HasFilter("\"IdentityNumber\" <> ''");

        // Öğrenci ve evrakları bağlama
        documentTable
            .HasOne<Student>()
            .WithMany()
            .HasForeignKey(document => document.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Her belge için tek okuma sonucu tutma
        modelBuilder.Entity<DocumentReadResult>()
            .HasOne<Document>()
            .WithOne()
            .HasForeignKey<DocumentReadResult>(result => result.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Referans öğrenci numaralarının tekrarını engelleme
        modelBuilder.Entity<TestStudent>()
            .HasIndex(student => student.StudentNumber)
            .IsUnique();

        // Kullanıcı silinse de giriş kayıtlarını koruma
        modelBuilder.Entity<LoginAttempt>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(attempt => attempt.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
