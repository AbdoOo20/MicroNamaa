using System.Globalization;

using System.Text;

namespace MicroNamaa.Application.Common;

/// <summary>Builds a CSV file for the Export buttons. UTF-8 with BOM so Excel shows Arabic correctly.</summary>
public sealed class CsvFile(params string[] headers)
{
    private readonly StringBuilder builder = new StringBuilder().AppendLine(string.Join(',', headers.Select(Escape)));

    public CsvFile Row(params object?[] values)
    {
        builder.AppendLine(string.Join(',', values.Select(Format)));

        return this;
    }

    public FileDownload ToDownload(string name)
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();

        return new FileDownload(new MemoryStream(bytes), "text/csv; charset=utf-8", $"{name}-{LibyaTime.Now:yyyyMMdd-HHmm}.csv");
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,

        DateTime date => Escape(LibyaTime.ToLocal(date).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),

        decimal number => number.ToString("0.###", CultureInfo.InvariantCulture),

        IFormattable formattable => Escape(formattable.ToString(null, CultureInfo.InvariantCulture)),

        _ => Escape(value.ToString() ?? string.Empty)
    };

    private static string Escape(string value)
    {
        // A leading = + - @ would run as a formula in Excel.
        if (value.Length > 0 && "=+-@".Contains(value[0]) && !decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            value = "'" + value;
        }

        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
