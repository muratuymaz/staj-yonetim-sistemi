using System.IO.Compression;

namespace StajYonetimSistemi.Services;

//Checks that an uploaded document has an allowed extension and matching signature.</summary>
public sealed class DocumentFileValidator
{
    private const int HeaderLength = 8;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png"
        };

    public async Task<string?> ValidateAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        string extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return "Yalnızca DOC, DOCX, PDF, JPEG ve PNG dosyaları yüklenebilir.";

        byte[] header = new byte[HeaderLength];
        await using (Stream stream = file.OpenReadStream())
        {
            int bytesRead = await stream.ReadAsync(header.AsMemory(), cancellationToken);
            bool matches = extension.ToLowerInvariant() switch
            {
                ".doc" => StartsWith(header, bytesRead, new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
                ".pdf" => StartsWith(header, bytesRead, "%PDF-"u8),
                ".png" => StartsWith(header, bytesRead, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".jpg" or ".jpeg" => StartsWith(header, bytesRead, new byte[] { 0xFF, 0xD8, 0xFF }),
                ".docx" => await IsDocxAsync(file, cancellationToken),
                _ => false
            };

            return matches ? null : "Dosyanın içeriği uzantısıyla uyuşmuyor veya dosya geçersiz.";
        }
    }

    private static bool StartsWith(byte[] actual, int actualLength, ReadOnlySpan<byte> expected) =>
        actualLength >= expected.Length && actual.AsSpan(0, expected.Length).SequenceEqual(expected);

    private static async Task<bool> IsDocxAsync(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            await using Stream stream = file.OpenReadStream();
            using ZipArchive archive = new(stream, ZipArchiveMode.Read);
            cancellationToken.ThrowIfCancellationRequested();
            return archive.GetEntry("[Content_Types].xml") is not null &&
                   archive.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
