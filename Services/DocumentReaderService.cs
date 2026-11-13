namespace StajYonetimSistemi.Services;

public class DocumentReaderService
{
    public string ReadText(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        DocumentReader reader;

        // Dosya türüne uygun okuyucuyu seçme
        if (extension == ".pdf")
        {
            reader = new PdfDocumentReader();
        }
        else if (extension == ".docx")
        {
            reader = new WordDocumentReader();
        }
        else
        {
            throw new NotSupportedException(
                "Şimdilik yalnızca DOCX ve PDF belgeleri okunabilir.");
        }

        // Seçilen okuyucunun metin okuma işlemini çalıştırma
        return reader.ReadText(filePath);
    }
}
