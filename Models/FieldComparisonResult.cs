namespace StajYonetimSistemi.Models;

// Karşılaştırma sonuçlarında kullanılan durumları tanımlama
public static class FieldComparisonStatus
{
    public const string Match = "match";
    public const string Mismatch = "mismatch";
    public const string Unreadable = "unreadable";
    public const string FormValueMissing = "formValueMissing";
}

// Tek bir alanın karşılaştırma sonucunu tutma
public sealed class FieldComparisonResult
{
    private string fieldName = "";
    private string? documentValue;
    private string? formValue;
    private string status = FieldComparisonStatus.Unreadable;

    // Karşılaştırılan alanın adını tutma
    public string FieldName
    {
        get
        {
            return fieldName;
        }

        init
        {
            fieldName = value;
        }
    }

    // Belgeden okunan değeri tutma
    public string? DocumentValue
    {
        get
        {
            return documentValue;
        }

        init
        {
            documentValue = value;
        }
    }

    // Karşılaştırılacak form veya referans değerini tutma
    public string? FormValue
    {
        get
        {
            return formValue;
        }

        init
        {
            formValue = value;
        }
    }

    // Durum bilgisini tutma
    public string Status
    {
        get
        {
            return status;
        }

        init
        {
            status = value;
        }
    }
}
