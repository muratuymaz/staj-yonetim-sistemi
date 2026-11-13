using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace StajYonetimSistemi.Services;

// Belge okuyucularının ortak işlemini tanımlama
public abstract class DocumentReader
{
    public abstract string ReadText(string filePath);
}

public class PdfDocumentReader : DocumentReader
{
    // PDF sayfalarındaki metinleri okuma
    public override string ReadText(string filePath)
    {
        using PdfDocument document = PdfDocument.Open(filePath);

        return string.Join(
            Environment.NewLine,
            document.GetPages()
                .Select(page => ContentOrderTextExtractor.GetText(page)));
    }
}

public class WordDocumentReader : DocumentReader
{
    // DOCX belgesindeki paragraf metinlerini okuma
    public override string ReadText(string filePath)
    {
        using WordprocessingDocument document =
            WordprocessingDocument.Open(filePath, false);

        var body = document.MainDocumentPart?.Document?.Body;

        if (body == null)
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            body.Descendants<Paragraph>()
                .Select(paragraph => string.Concat(
                    paragraph.Descendants<Text>()
                        .Select(text => text.Text)))
                .Where(line => !string.IsNullOrWhiteSpace(line)));
    }
}
