using System.Buffers.Binary;
using DocumentFormat.OpenXml.Packaging;
using OpenMcdf;
using SkiaSharp;
using UglyToad.PdfPig;

namespace StajYonetimSistemi.Services;

// Yüklenen belgelerin boyut, tür ve geçerlilik kontrollerini yapan servis
public sealed class DocumentFileValidator(ILogger<DocumentFileValidator> logger)
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private const string InvalidFileMessage =
        "Dosya geçersiz veya bozuk. Lütfen geçerli bir belge seçin.";

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png"
        };

    // Yüklenen dosyanın boyutunu, türünü ve okunabilirliğini kontrol etme
    public async Task<string?> ValidateAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0 || file.Length > MaxFileSize)
            return "Dosya boş olamaz ve en fazla 10 MB olabilir.";

        string extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return "Yalnızca DOC, DOCX, PDF, JPEG ve PNG dosyaları yüklenebilir.";

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Uzantı ile temel dosya imzası eşleşmeden belge ayrıştırıcısını çalıştırmıyoruz.
            if (!await HasMatchingSignatureAsync(file, extension, cancellationToken))
                return InvalidFileMessage;

            // İmza sahte dosyalar için yeterli değildir; türüne uygun ayrıştırıcıyla açıyoruz.
            bool isReadable = extension.ToLowerInvariant() switch
            {
                ".doc" => IsReadableLegacyWord(file),
                ".docx" => IsReadableDocx(file),
                ".pdf" => IsReadablePdf(file),
                ".jpg" or ".jpeg" or ".png" =>
                    await IsReadableImageAsync(file, cancellationToken),
                _ => false
            };

            return isReadable ? null : InvalidFileMessage;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // İstek iptalini bozuk dosya hatası gibi göstermiyoruz.
            throw;
        }
        catch (Exception exception)
        {
            // Bozuk dosya ayrıştırıcılarının hatası 500'e dönüşmesin; ayrıntıyı logla.
            logger.LogWarning(
                exception,
                "Yüklenen belge doğrulanamadı. Uzantı: {Extension}",
                extension);

            return InvalidFileMessage;
        }
    }

    // Dosyanın başlangıç baytlarını uzantısına ait imzayla karşılaştırma
    private static async Task<bool> HasMatchingSignatureAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        byte[] header = new byte[8];
        int totalRead = 0;

        // Stream tek okumada tüm başlığı vermeyebilir; istenen baytları döngüyle oku.
        await using (Stream stream = file.OpenReadStream())
        {
            while (totalRead < header.Length)
            {
                int read = await stream.ReadAsync(
                    header.AsMemory(totalRead), cancellationToken);

                if (read == 0)
                    break;

                totalRead += read;
            }
        }

        ReadOnlySpan<byte> expected = extension.ToLowerInvariant() switch
        {
            ".doc" => new byte[]
                { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 },
            ".docx" => new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            ".pdf" => "%PDF-"u8,
            ".png" => new byte[]
                { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            ".jpg" or ".jpeg" => new byte[] { 0xFF, 0xD8, 0xFF },
            _ => ReadOnlySpan<byte>.Empty
        };

        return expected.Length > 0 &&
               totalRead >= expected.Length &&
               header.AsSpan(0, expected.Length).SequenceEqual(expected);
    }

    // DOCX dosyasını açıp ana belge gövdesinin bulunup bulunmadığını kontrol etme
    private static bool IsReadableDocx(IFormFile file)
    {
        // Open XML paketi açıp ana belge gövdesini ayrıştırıyoruz.
        using Stream stream = file.OpenReadStream();
        using WordprocessingDocument document =
            WordprocessingDocument.Open(stream, false);

        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
            return false;

        // XML içeriğine erişerek yalnızca ZIP girdilerinin adlarını kontrol etmemiş oluyoruz.
        _ = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().ToList();
        return true;
    }

    // Eski DOC dosyasının Word başlığını ve gerekli iç akışlarını kontrol etme
    private static bool IsReadableLegacyWord(IFormFile file)
    {
        // OLE imzası Excel/PowerPoint gibi başka biçimlerde de bulunduğundan,
        // eski Word'e özgü akışları ve Word FIB tanımlayıcısını doğrula.
        using Stream stream = file.OpenReadStream();
        using RootStorage root = RootStorage.Open(
            stream,
            StorageModeFlags.StrictValidation | StorageModeFlags.LeaveOpen);
        using CfbStream wordStream = root.OpenStream("WordDocument");

        byte[] fibHeader = new byte[2];
        wordStream.ReadExactly(fibHeader);

        bool hasWordIdentifier =
            BinaryPrimitives.ReadUInt16LittleEndian(fibHeader) == 0xA5EC;

        return hasWordIdentifier && HasCompoundStream(root, "0Table", "1Table");
    }

    // Eski Word dosyasında belirtilen akışlardan en az birinin boş olmadığını kontrol etme
    private static bool HasCompoundStream(RootStorage root, string first, string second)
    {
        if (root.TryOpenStream(first, out CfbStream? firstTable) && firstTable is not null)
        {
            using (firstTable)
                return firstTable.Length > 0;
        }

        if (root.TryOpenStream(second, out CfbStream? secondTable) && secondTable is not null)
        {
            using (secondTable)
                return secondTable.Length > 0;
        }

        return false;
    }

    // PDF dosyasının açılıp sayfa yapısının okunabildiğini kontrol etme
    private static bool IsReadablePdf(IFormFile file)
    {
        // PDF okuyucuyla sayfa yapısını ayrıştır; metin olmaması geçerli PDF'yi reddetmez.
        using Stream stream = file.OpenReadStream();
        using PdfDocument document = PdfDocument.Open(stream);

        foreach (var page in document.GetPages())
            _ = page.Text;

        return document.NumberOfPages > 0;
    }

    // Görsel dosyasını asenkron olarak açıp okunabilirliğini kontrol etme
    private static Task<bool> IsReadableImageAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        // Görseli tam çözümlemek bozuk JPEG/PNG içeriklerini başlık kontrolünden ayırır.
        cancellationToken.ThrowIfCancellationRequested();
        using Stream stream = file.OpenReadStream();

        // SkiaSharp geçersiz veya çözümlenemeyen görselde null döndürür.
        using SKBitmap? bitmap = SKBitmap.Decode(stream);
        return Task.FromResult(bitmap is { Width: > 0, Height: > 0 });
    }
}
