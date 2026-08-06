using System.Globalization;
using System.Text;

namespace Currency.Services;

/// <summary>Formattazione/parsing manuale in stile italiano (punto migliaia, virgola decimali), senza richiedere i dati ICU completi.</summary>
public static class NumberFormatter
{
    public static string FormatFixed(decimal value, int decimals = 2)
    {
        value = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        var negative = value < 0;
        value = Math.Abs(value);

        var raw = value.ToString("F" + decimals, CultureInfo.InvariantCulture);
        var dotIndex = raw.IndexOf('.');
        var intPart = dotIndex >= 0 ? raw[..dotIndex] : raw;
        var decPart = dotIndex >= 0 ? raw[(dotIndex + 1)..] : "";

        var sb = new StringBuilder();
        for (var i = 0; i < intPart.Length; i++)
        {
            if (i > 0 && (intPart.Length - i) % 3 == 0)
                sb.Append('.');
            sb.Append(intPart[i]);
        }

        if (decimals > 0)
        {
            sb.Append(',');
            sb.Append(decPart);
        }

        return negative ? "-" + sb : sb.ToString();
    }

    /// <summary>Rappresentazione "editabile" di un importo: intero se non ha decimali, altrimenti con virgola, niente separatore delle migliaia.</summary>
    public static string ToEditable(decimal value)
    {
        if (value == 0m)
            return "";

        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return rounded == Math.Truncate(rounded)
            ? rounded.ToString("0", CultureInfo.InvariantCulture)
            : rounded.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
    }

    /// <summary>Interpreta un buffer digitato dall'utente (es. "150", "35,3"). Tollera una virgola finale mentre l'utente sta ancora scrivendo.</summary>
    public static decimal ParseBuffer(string buffer)
    {
        var trimmed = buffer.TrimEnd(',');
        if (string.IsNullOrEmpty(trimmed))
            return 0m;

        return decimal.TryParse(trimmed.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
    }

    /// <summary>Ripulisce l'input libero (tastiera fisica/incolla) lasciando solo cifre e una virgola.</summary>
    public static string SanitizeBuffer(string raw)
    {
        var sb = new StringBuilder();
        var commaSeen = false;
        foreach (var ch in raw)
        {
            if (char.IsDigit(ch))
            {
                sb.Append(ch);
            }
            else if ((ch == ',' || ch == '.') && !commaSeen)
            {
                sb.Append(',');
                commaSeen = true;
            }
        }

        return sb.ToString();
    }
}
