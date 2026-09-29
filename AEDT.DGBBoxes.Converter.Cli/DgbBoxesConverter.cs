using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace AEDT.DGBBoxes.Converter.Cli;

/// <summary>
/// Converts old-format AEDT DGBBoxes files (<c>id, minLon, maxLon, minLat, maxLat</c> per row) to the new format
/// (the four corners per row: bottom-left, top-left, top-right, bottom-right, as <c>lon, lat</c> pairs).
/// </summary>
public static class DgbBoxesConverter
{
    private const int FieldCount = 5;

    /// <summary>
    /// Reads and validates an old-format file. Blank lines and the <c>END</c> trailer are skipped; any other invalid
    /// row stops the read.
    /// </summary>
    /// <param name="input">The old-format file.</param>
    /// <returns>The boxes, in file order.</returns>
    /// <exception cref="DgbBoxesFormatException">
    /// A row does not have exactly five fields, a coordinate is not a finite number or is out of range, a minimum
    /// exceeds its maximum, or the file contains no boxes.
    /// </exception>
    public static List<InputRecord> Read(TextReader input)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            IgnoreBlankLines = true,
            // Fields are separated by ", "; trimming also lets quoted fields after the space parse.
            TrimOptions = TrimOptions.Trim,
        };

        using var csv = new CsvReader(input, config);
        var records = new List<InputRecord>();

        try
        {
            while (csv.Read())
            {
                int row = csv.Parser.RawRow;

                if (csv.GetField(0)?.StartsWith("END", StringComparison.Ordinal) == true) continue;

                if (csv.Parser.Count != FieldCount)
                {
                    throw new DgbBoxesFormatException(row,
                        $"expected {FieldCount} fields (id, minLon, maxLon, minLat, maxLat) but found {csv.Parser.Count}.");
                }

                var record = new InputRecord
                {
                    Id = csv.GetField(0) ?? string.Empty,
                    MinLon = ParseCoordinate(csv, 1, "minLon", 180, row),
                    MaxLon = ParseCoordinate(csv, 2, "maxLon", 180, row),
                    MinLat = ParseCoordinate(csv, 3, "minLat", 90, row),
                    MaxLat = ParseCoordinate(csv, 4, "maxLat", 90, row),
                };

                if (record.MinLon > record.MaxLon)
                {
                    // Also rejects boxes crossing the antimeridian, which the new format can't express as min/max.
                    throw new DgbBoxesFormatException(row, $"minLon {Format(record.MinLon)} is greater than maxLon {Format(record.MaxLon)}.");
                }

                if (record.MinLat > record.MaxLat)
                {
                    throw new DgbBoxesFormatException(row, $"minLat {Format(record.MinLat)} is greater than maxLat {Format(record.MaxLat)}.");
                }

                records.Add(record);
            }
        }
        catch (CsvHelperException ex)
        {
            // e.g. a quote inside an unquoted field
            throw new DgbBoxesFormatException(csv.Parser.RawRow, "the row is not valid CSV.", ex);
        }

        if (records.Count == 0)
        {
            throw new DgbBoxesFormatException(csv.Parser.RawRow, "the file contains no boxes.");
        }

        return records;
    }

    /// <summary>
    /// Writes boxes in the new format: one row per box with its four corners, separated by <c>", "</c>.
    /// </summary>
    /// <param name="records">The boxes.</param>
    /// <param name="output">Writer for the new-format file.</param>
    public static void Write(IEnumerable<InputRecord> records, TextWriter output)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ", ",
        };

        using var csv = new CsvWriter(output, config, leaveOpen: true);

        foreach (var record in records)
        {
            var outputRecord = new OutputRecord
            {
                // Bottom-left corner
                Lon1 = record.MinLon,
                Lat1 = record.MinLat,

                // Top-left corner
                Lon2 = record.MinLon,
                Lat2 = record.MaxLat,

                // Top-right corner
                Lon3 = record.MaxLon,
                Lat3 = record.MaxLat,

                // Bottom-right corner
                Lon4 = record.MaxLon,
                Lat4 = record.MinLat
            };

            csv.WriteRecord(outputRecord);
            csv.NextRecord();
        }
    }

    private static double ParseCoordinate(CsvReader csv, int index, string name, double limit, int row)
    {
        string text = csv.GetField(index) ?? string.Empty;

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
            !double.IsFinite(value))
        {
            throw new DgbBoxesFormatException(row, $"{name} \"{text}\" is not a number.");
        }

        if (value < -limit || value > limit)
        {
            throw new DgbBoxesFormatException(row, $"{name} {Format(value)} is outside -{limit}..{limit}.");
        }

        return value;
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
