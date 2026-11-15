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
