# Staj Evrak Yönetim Sistemi

Öğrencilerin Ek-16 belgelerini ve bilgilerini gönderebildiği, yöneticilerin ise kayıtları ve karşılaştırma sonuçlarını inceleyebildiği bir web uygulamasıdır. Kocaeli Üniversitesi Programlama Laboratuvarı III dersi için yapılmıştır.

## Ekip

- Murat Uymaz
- Ahmet Melih Çalış
- Hatice Can

## Kullanılan teknolojiler

- C# ve .NET 10
- ASP.NET Core MVC
- Entity Framework Core ve SQLite
- DOCX okumak için Open XML, PDF okumak için PdfPig
- HTML, CSS ve JavaScript

## Proje yapısı

```text
Controllers/       Sayfa ve istek işlemleri
Data/              Veritabanı bağlantısı ve migration dosyaları
Filters/           Oturum ve rol kontrolleri
Models/            Kullanıcı, öğrenci ve belge modelleri
Services/          Belge okuma, karşılaştırma ve diğer işlemler
Views/             HTML içeren Razor sayfaları
wwwroot/           CSS ve JavaScript dosyaları
Database/          Yerel SQLite veritabanı
Uploads/Documents/ Yüklenen belgeler
```

`Database` klasörü kurulumda oluşturulur, `Uploads/Documents` klasörü belge yüklenirken oluşturulur.

## Nesne yönelimli programlama

Projede nesne yönelimli programlamanın dört temel prensibinden yararlanılmıştır:

### Kalıtım

`Models/Student.cs` içindeki `Student` sınıfı, `User` sınıfından türetilmiştir. Böylece öğrenci, `User` içinde tanımlanan kullanıcı adı, e-posta, şifre hash'i ve rol gibi hesap bilgilerini kullanır. Öğrenci numarası, ad soyad ve bölüm gibi ek bilgiler `Student` sınıfında tutulur. `Data/StajDbContext.cs` içinde ortak kullanıcı bilgileri ve öğrenciye özel bilgiler ayrı tablolara eşlenmiştir.

`Services/DocumentReaders.cs` dosyasında `PdfDocumentReader` ve `WordDocumentReader` sınıfları da ortak `DocumentReader` sınıfından türetilmiştir.

### Kapsülleme

`Models/User.cs` içinde hatalı giriş sayısı ve hesap kilidi özel alanlarda tutulur. `FailedLoginCount` ve `IsLocked` özellikleri dışarıdan okunabilir, ancak `private set` kullanıldığı için dışarıdan doğrudan değiştirilemez.

`RegisterFailedLogin()` metodu hatalı giriş sayısını artırır ve sayı beşe ulaştığında hesabı kilitler. `ResetFailedLoginCount()` metodu başarılı girişte sayacı sıfırlar. Bu metotlar `Controllers/AccountController.cs` içindeki giriş işleminde kullanılır.

### Soyutlama

`Services/DocumentReaders.cs` içindeki soyut `DocumentReader` sınıfı, belge okuyucuları için `ReadText` adlı ortak işlemi tanımlar. İşlemin içeriği bu sınıfta yazılmaz, PDF ve Word dosyalarının nasıl okunacağı ilgili okuyucu sınıflarında belirlenir.

### Çok biçimlilik

`PdfDocumentReader` ve `WordDocumentReader`, ortak `ReadText` işlemini kendi dosya türlerine göre uygular. PDF okuyucu metni PdfPig ile, Word okuyucu ise DOCX içeriğini Open XML ile okur.

`Services/DocumentReaderService.cs`, dosya uzantısına göre uygun okuyucuyu seçer ve `ReadText` işlemini ortak `DocumentReader` türü üzerinden çağırır. Bu servis, `Controllers/StudentController.cs` içinde yüklenen belgenin metnini okumak için kullanılır.

## Kurulum ve çalıştırma

Bilgisayarınızda **.NET 10 SDK** kurulu olmalıdır. Komutları projenin `.csproj` dosyasının bulunduğu klasörde çalıştırın.

1. Projeyi klonlayın ve proje klasörüne girin:

   ```bash
   git clone https://github.com/muratuymaz/staj-yonetim-sistemi.git
   cd staj-yonetim-sistemi
   ```

2. Paketleri indirin:

   ```bash
   dotnet restore
   ```

3. EF Core komutları için `dotnet-ef` kurulu değilse yükleyin:

   ```bash
   dotnet tool install --global dotnet-ef --version 10.0.13
   ```

4. Proje klasöründe `Database` klasörü yoksa oluşturun. Ardından veritabanını oluşturun ve uygulamayı başlatın:

   ```bash
   dotnet ef database update
   dotnet run -- --environment Development
   ```

5. Terminalde gösterilen yerel adresi tarayıcıda açın.

SQLite veritabanı her geliştiricinin bilgisayarında yerel olarak oluşur.

## Test kullanıcıları

Veritabanı oluşturulduktan sonra öğrenci hesabı kayıt ekranından oluşturulabilir.

Uygulama geliştirme ortamında ilk kez başlatıldığında test için bir admin hesabı oluşturulur:

- Kullanıcı adı: `admin`
- Şifre: `AdminTest!2026`

Bu hesap yerel geliştirme ve test içindir.
