using System.Globalization;
using System.Text.RegularExpressions;

namespace StajYonetimSistemi.Services;

public static class FieldValueNormalizer
{
    // Ad ve adres gibi metinlerde boşluk ve harf büyüklüğü farklarını giderir.
    // Noktalama işaretlerini korur.
    public static string NormalizeText(string? value)
    {
        return Regex.Replace(value ?? "", @"\s+", " ")
            .Trim()
            .ToUpperInvariant();
    }

    // Kimlik numarası biçimindeki boşluk ve ayraçları yok sayar.
    public static string NormalizeDigits(string? value)
    {
        return Regex.Replace(value ?? "", @"\D", "");
    }

    public static string NormalizeEmail(string? value)
    {
        return (value ?? "").Trim().ToUpperInvariant();
    }

    // +90 532..., 0532... ve 532... biçimlerini aynı yerel numaraya indirger.
    public static string NormalizePhone(string? value)
    {
        string digits = NormalizeDigits(value);

        if (digits.Length == 12 && digits.StartsWith("90"))
            return digits[2..];

        if (digits.Length == 11 && digits.StartsWith("0"))
            return digits[1..];

        return digits;
    }

    public static string NormalizeDate(DateTime? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
    }

    public static string NormalizeNumber(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? "";
    }
}
