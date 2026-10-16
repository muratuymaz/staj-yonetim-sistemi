namespace StajYonetimSistemi.Models;

public class Document
{
    public int Id { get; set; }
    public int StudentId { get; set; }

    // Yüklenen Ek-16 dosyasının bilgilerini tutma
    public string OriginalFileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Evrak durumunu tutma
    public string Status { get; set; } = "Uploaded";
}
