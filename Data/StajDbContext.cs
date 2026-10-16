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
    }

    // Tabloları tanımlama
    public DbSet<User> Users { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tablo ayarlarını hazırlama
        EntityTypeBuilder<User> userTable = modelBuilder.Entity<User>();
        EntityTypeBuilder<Student> studentTable = modelBuilder.Entity<Student>();
        EntityTypeBuilder<Document> documentTable = modelBuilder.Entity<Document>();

        // Kullanıcı bilgilerinin tekrarını engelleme
        userTable.Property(user => user.UserName).UseCollation("NOCASE");
        userTable.Property(user => user.Email).UseCollation("NOCASE");

        IndexBuilder userNameIndex = userTable.HasIndex(user => user.UserName);
        userNameIndex.IsUnique();

        IndexBuilder emailIndex = userTable.HasIndex(user => user.Email);
        emailIndex.IsUnique();

        // Öğrenci bilgilerinin tekrarını engelleme
        IndexBuilder studentNumberIndex = studentTable.HasIndex(student => student.StudentNumber);
        studentNumberIndex.IsUnique();

        IndexBuilder identityNumberIndex = studentTable.HasIndex(student => student.IdentityNumber);
        identityNumberIndex.IsUnique();

        // Kullanıcı ve öğrenciyi bağlama
        studentTable
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Student>(student => student.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Öğrenci ve evrakları bağlama
        documentTable
            .HasOne<Student>()
            .WithMany()
            .HasForeignKey(document => document.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
