using System.Buffers.Binary;
using DocumentFormat.OpenXml.Packaging;
using OpenMcdf;
using SkiaSharp;
using UglyToad.PdfPig;

namespace StajYonetimSistemi.Services;

public sealed class DocumentFileValidator(ILogger<DocumentFileValidator> logger)
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private const string InvalidFileMessage = "Dosya geçersiz veya bozuk. Lütfen geçerli bir belge seçin.";
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png" };

    // Dosyanın boyutunu, türünü ve açılabilirliğini kontrol etme
    public async Task<string?> ValidateAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0 || file.Length > MaxFileSize)
        {
            return "Dosya boş olamaz ve en fazla 10 MB olabilir.";
        }

        string extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return "Yalnızca DOC, DOCX, PDF, JPEG ve PNG dosyaları yüklenebilir.";
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await HasMatchingSignatureAsync(file, extension, cancellationToken))
            {
                return InvalidFileMessage;
            }

            bool isReadable = extension.ToLowerInvariant() switch
            {
                ".doc" => IsReadableLegacyWord(file),
                ".docx" => IsReadableDocx(file),
                ".pdf" => IsReadablePdf(file),
                ".jpg" or ".jpeg" or ".png" => IsReadableImage(file, cancellationToken),
                _ => false
            };

            if (isReadable)
            {
                return null;
            }

            return InvalidFileMessage;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Yüklenen belge doğrulanamadı. Uzantı: {Extension}", extension);
            return InvalidFileMessage;
        }
    }

    // Dosya başlığının uzantıyla uyuşup uyuşmadığını kontrol etme
    private static async Task<bool> HasMatchingSignatureAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        byte[] header = new byte[8];
        int totalRead = 0;

        await using (Stream stream = file.OpenReadStream())
        {
            while (totalRead < header.Length)
            {
                int read = await stream.ReadAsync(header.AsMemory(totalRead), cancellationToken);
                if (read == 0)
                {
                    break;
                }
                totalRead += read;
            }
        }

        ReadOnlySpan<byte> expected = extension.ToLowerInvariant() switch
        {
            ".doc" => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1],
            ".docx" => [0x50, 0x4B, 0x03, 0x04],
            ".pdf" => "%PDF-"u8,
            ".png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            ".jpg" or ".jpeg" => [0xFF, 0xD8, 0xFF],
            _ => ReadOnlySpan<byte>.Empty
        };

        return expected.Length > 0 && totalRead >= expected.Length &&
               header.AsSpan(0, expected.Length).SequenceEqual(expected);
    }

    // DOCX belgesinin açılıp açılmadığını kontrol etme
    private static bool IsReadableDocx(IFormFile file)
    {
        using Stream stream = file.OpenReadStream();
        using WordprocessingDocument document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return false;
        }

        _ = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().ToList();
        return true;
    }

    // Eski Word belgesinin iç yapısını kontrol etme
    private static bool IsReadableLegacyWord(IFormFile file)
    {
        using Stream stream = file.OpenReadStream();
        using RootStorage root = RootStorage.Open(
            stream, StorageModeFlags.StrictValidation | StorageModeFlags.LeaveOpen);
        using CfbStream wordStream = root.OpenStream("WordDocument");

        byte[] fibHeader = new byte[2];
        wordStream.ReadExactly(fibHeader);
        return BinaryPrimitives.ReadUInt16LittleEndian(fibHeader) == 0xA5EC &&
               HasCompoundStream(root, "0Table", "1Table");
    }

    private static bool HasCompoundStream(RootStorage root, string first, string second)
    {
        if (root.TryOpenStream(first, out CfbStream? firstTable) && firstTable is not null)
        {
            using (firstTable) return firstTable.Length > 0;
        }

        if (root.TryOpenStream(second, out CfbStream? secondTable) && secondTable is not null)
        {
            using (secondTable) return secondTable.Length > 0;
        }

        return false;
    }

    // PDF dosyasının sayfalarını okuyabildiğini kontrol etme
    private static bool IsReadablePdf(IFormFile file)
    {
        using Stream stream = file.OpenReadStream();
        using PdfDocument document = PdfDocument.Open(stream);
        foreach (var page in document.GetPages())
        {
            _ = page.Text;
        }
        return document.NumberOfPages > 0;
    }

    // Görsel dosyasının açılıp açılmadığını kontrol etme
    private static bool IsReadableImage(IFormFile file, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using Stream stream = file.OpenReadStream();
        using SKBitmap? bitmap = SKBitmap.Decode(stream);

        if (bitmap == null)
        {
            return false;
        }

        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return false;
        }

        return true;
    }
}
