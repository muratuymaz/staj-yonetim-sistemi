using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace StajYonetimSistemi.Services;

public class DocumentReaderService
{
    public string ReadText(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".docx" => ReadWordDocument(filePath),
            ".pdf" => ReadPdfDocument(filePath),
            _ => throw new NotSupportedException(
                "Şimdilik yalnızca DOCX ve PDF belgeleri okunabilir.")
        };
    }

    private static string ReadWordDocument(string filePath)
    {
        using WordprocessingDocument document =
            WordprocessingDocument.Open(filePath, false);

        Body? body = document.MainDocumentPart?.Document?.Body;

        if (body == null)
            return string.Empty;

        return string.Join(
            Environment.NewLine,
            body.Descendants<Paragraph>()
                .Select(paragraph => string.Concat(
                    paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                        .Select(text => text.Text)))
                .Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static string ReadPdfDocument(string filePath)
    {
        using PdfDocument document = PdfDocument.Open(filePath);

        return string.Join(
            Environment.NewLine,
            document.GetPages()
                .Select(page => ContentOrderTextExtractor.GetText(page)));
    }
}