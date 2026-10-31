using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StajYonetimSistemi.Data.Migrations
{
    public partial class UseStudentInheritance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Öğrenci kimliğini bağlı hesabın kimliğiyle birleştirme
            migrationBuilder.Sql("""
                CREATE TABLE "Students_New" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Students" PRIMARY KEY,
                    "StudentNumber" TEXT NOT NULL,
                    "FullName" TEXT NOT NULL,
                    "IdentityNumber" TEXT NOT NULL,
                    "Nationality" TEXT NOT NULL,
                    "BirthDate" TEXT NOT NULL,
                    "Address" TEXT NOT NULL,
                    "PhoneNumber" TEXT NOT NULL,
                    "Department" TEXT NOT NULL,
                    "AcademicYear" TEXT NOT NULL,
                    "PreviousInternshipDays" INTEGER NOT NULL,
                    "HealthAidStatus" INTEGER NOT NULL,
                    CONSTRAINT "FK_Students_Users_Id" FOREIGN KEY ("Id")
                        REFERENCES "Users" ("Id") ON DELETE CASCADE
                );

                INSERT INTO "Students_New"
                    ("Id", "StudentNumber", "FullName", "IdentityNumber", "Nationality",
                     "BirthDate", "Address", "PhoneNumber", "Department", "AcademicYear",
                     "PreviousInternshipDays", "HealthAidStatus")
                SELECT "UserId", "StudentNumber", "FullName", "IdentityNumber", "Nationality",
                       "BirthDate", "Address", "PhoneNumber", "Department", "AcademicYear",
                       "PreviousInternshipDays", "HealthAidStatus"
                FROM "Students";

                INSERT INTO "Students_New"
                    ("Id", "StudentNumber", "FullName", "IdentityNumber", "Nationality",
                     "BirthDate", "Address", "PhoneNumber", "Department", "AcademicYear",
                     "PreviousInternshipDays", "HealthAidStatus")
                SELECT "Id", '', '', '', '', '0001-01-01 00:00:00', '', '', '', '', 0, 0
                FROM "Users" AS account
                WHERE account."Role" = 'Student'
                  AND NOT EXISTS (SELECT 1 FROM "Students_New" AS student
                                  WHERE student."Id" = account."Id");
                """);

            // Belge sahiplerini yeni öğrenci kimliklerine taşıma
            migrationBuilder.Sql("""
                CREATE TABLE "Documents_New" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Documents" PRIMARY KEY AUTOINCREMENT,
                    "StudentId" INTEGER NOT NULL,
                    "OriginalFileName" TEXT NOT NULL,
                    "FilePath" TEXT NOT NULL,
                    "UploadedAt" TEXT NOT NULL,
                    "Status" TEXT NOT NULL,
                    CONSTRAINT "FK_Documents_Students_StudentId" FOREIGN KEY ("StudentId")
                        REFERENCES "Students_New" ("Id") ON DELETE RESTRICT
                );

                INSERT INTO "Documents_New"
                    ("Id", "StudentId", "OriginalFileName", "FilePath", "UploadedAt", "Status")
                SELECT document."Id", student."UserId", document."OriginalFileName",
                       document."FilePath", document."UploadedAt", document."Status"
                FROM "Documents" AS document
                INNER JOIN "Students" AS student ON student."Id" = document."StudentId";

                DROP TABLE "Documents";
                DROP TABLE "Students";
                ALTER TABLE "Students_New" RENAME TO "Students";
                ALTER TABLE "Documents_New" RENAME TO "Documents";

                CREATE UNIQUE INDEX "IX_Students_StudentNumber" ON "Students" ("StudentNumber")
                    WHERE "StudentNumber" <> '';
                CREATE UNIQUE INDEX "IX_Students_IdentityNumber" ON "Students" ("IdentityNumber")
                    WHERE "IdentityNumber" <> '';
                CREATE INDEX "IX_Documents_StudentId" ON "Documents" ("StudentId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hesap ve öğrenci bağlantısını eski UserId alanına döndürme
            migrationBuilder.Sql("""
                CREATE TABLE "Students_Old" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Students" PRIMARY KEY AUTOINCREMENT,
                    "UserId" INTEGER NOT NULL,
                    "StudentNumber" TEXT NOT NULL,
                    "FullName" TEXT NOT NULL,
                    "IdentityNumber" TEXT NOT NULL,
                    "Nationality" TEXT NOT NULL,
                    "BirthDate" TEXT NOT NULL,
                    "Address" TEXT NOT NULL,
                    "PhoneNumber" TEXT NOT NULL,
                    "Department" TEXT NOT NULL,
                    "AcademicYear" TEXT NOT NULL,
                    "PreviousInternshipDays" INTEGER NOT NULL,
                    "HealthAidStatus" INTEGER NOT NULL,
                    CONSTRAINT "FK_Students_Users_UserId" FOREIGN KEY ("UserId")
                        REFERENCES "Users" ("Id") ON DELETE RESTRICT
                );

                INSERT INTO "Students_Old"
                    ("Id", "UserId", "StudentNumber", "FullName", "IdentityNumber", "Nationality",
                     "BirthDate", "Address", "PhoneNumber", "Department", "AcademicYear",
                     "PreviousInternshipDays", "HealthAidStatus")
                SELECT student."Id", student."Id", student."StudentNumber", student."FullName",
                       student."IdentityNumber", student."Nationality", student."BirthDate",
                       student."Address", student."PhoneNumber", student."Department",
                       student."AcademicYear", student."PreviousInternshipDays", student."HealthAidStatus"
                FROM "Students" AS student
                WHERE student."StudentNumber" <> '' OR student."IdentityNumber" <> ''
                   OR EXISTS (SELECT 1 FROM "Documents" AS document
                              WHERE document."StudentId" = student."Id");

                CREATE TABLE "Documents_Old" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Documents" PRIMARY KEY AUTOINCREMENT,
                    "StudentId" INTEGER NOT NULL,
                    "OriginalFileName" TEXT NOT NULL,
                    "FilePath" TEXT NOT NULL,
                    "UploadedAt" TEXT NOT NULL,
                    "Status" TEXT NOT NULL,
                    CONSTRAINT "FK_Documents_Students_StudentId" FOREIGN KEY ("StudentId")
                        REFERENCES "Students_Old" ("Id") ON DELETE RESTRICT
                );

                INSERT INTO "Documents_Old"
                    ("Id", "StudentId", "OriginalFileName", "FilePath", "UploadedAt", "Status")
                SELECT "Id", "StudentId", "OriginalFileName", "FilePath", "UploadedAt", "Status"
                FROM "Documents";

                DROP TABLE "Documents";
                DROP TABLE "Students";
                ALTER TABLE "Students_Old" RENAME TO "Students";
                ALTER TABLE "Documents_Old" RENAME TO "Documents";

                CREATE UNIQUE INDEX "IX_Students_UserId" ON "Students" ("UserId");
                CREATE UNIQUE INDEX "IX_Students_StudentNumber" ON "Students" ("StudentNumber");
                CREATE UNIQUE INDEX "IX_Students_IdentityNumber" ON "Students" ("IdentityNumber");
                CREATE INDEX "IX_Documents_StudentId" ON "Documents" ("StudentId");
                """);
        }
    }
}
