namespace AEDT.DGBBoxes.Converter.Cli;

/// <summary>
/// Parses the command line and runs the conversion.
/// </summary>
public static class CommandLine
{
    /// <summary>
    /// Exit code for a successful conversion.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Exit code for invalid arguments or a failed conversion.
    /// </summary>
    public const int Failure = 1;

    /// <summary>
    /// Runs the converter.
    /// </summary>
    /// <param name="args">Input file and output file.</param>
    /// <param name="stdout">Writer for progress output.</param>
    /// <param name="stderr">Writer for errors.</param>
    /// <returns><see cref="Success"/> or <see cref="Failure"/>.</returns>
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length != 2)
        {
            stderr.WriteLine($"Error: expected 2 arguments but got {args.Length}.");
            stderr.WriteLine("Usage: AEDT.DGBBoxes.Converter.Cli <inputFile> <outputFile>");
            stderr.WriteLine("Converts old format DGBBoxes files into the new AEDT format.");
            return Failure;
        }

        string inputFile = args[0];
        string outputFile = args[1];

        try
        {
            List<InputRecord> records;
            using (var reader = new StreamReader(inputFile))
            {
                records = DgbBoxesConverter.Read(reader);
            }

            // Convert fully before touching the output file, so a bad input leaves no partial file behind.
            var converted = new StringWriter();
            DgbBoxesConverter.Write(records, converted);
            File.WriteAllText(outputFile, converted.ToString());

            stdout.WriteLine($"Successfully converted {records.Count} polygons to new format in {outputFile}");
            return Success;
        }
        catch (DgbBoxesFormatException ex)
        {
            stderr.WriteLine($"Error: {inputFile}, {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // FileNotFoundException and DirectoryNotFoundException are IOExceptions.
            stderr.WriteLine($"Error: {ex.Message}");
        }

        return Failure;
    }
}
