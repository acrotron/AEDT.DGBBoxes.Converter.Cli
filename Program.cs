using System.Globalization;
using AEDT.DGBBoxes.Converter.Cli;
using CsvHelper;
using CsvHelper.Configuration;

List<InputRecord> ReadInputFile(string inputFile)
{
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        IgnoreBlankLines = true,  // Skip blank lines
        MissingFieldFound = null, // Don't throw on missing fields
        BadDataFound = context =>
        {
            Console.WriteLine($"Warning: Bad data found at row {context.RawRecord}");
        }
    };

    using var reader = new StreamReader(inputFile);
    using var csv = new CsvReader(reader, config);
    csv.Context.RegisterClassMap<FileFormatMap>();

    var records = new List<InputRecord>();

    while (csv.Read())
    {
        try
        {
            // Check if we have enough fields and they're not empty
            if (csv.Parser.Count >= 5 &&
                !string.IsNullOrWhiteSpace(csv.GetField(1)) &&
                !string.IsNullOrWhiteSpace(csv.GetField(2)) &&
                !string.IsNullOrWhiteSpace(csv.GetField(3)) &&
                !string.IsNullOrWhiteSpace(csv.GetField(4)))
            {
                var record = csv.GetRecord<InputRecord>();
                records.Add(record);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Skipping invalid row {csv.Parser.RawRow}: {ex.Message}");
        }
    }

    return records;
}

void WriteOutputFormat(List<InputRecord> records, string outputFile)
{
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = ", ",  // Add space after delimiter
    };

    using var writer = new StreamWriter(outputFile);
    using var csv = new CsvWriter(writer, config);

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

if (args.Length != 2)
{
    Console.WriteLine("Usage: CoordinateConverter <inputFile> <outputFile>");
    Console.WriteLine("Converts old format DGBBoxes files into the new AEDT format.");
    return;
}

var inputFile = args[0];
var outputFile = args[1];

try
{
    var records = ReadInputFile(inputFile);
    WriteOutputFormat(records, outputFile);
    Console.WriteLine($"Successfully converted {records.Count} polygons to new format in {outputFile}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error processing file: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
