using System.Globalization;
using AwesomeAssertions;

namespace AEDT.DGBBoxes.Converter.Cli.Tests;

[TestClass]
public class CommandLineTests
{
    private const string Example = "TestData/example.csv";
    private const string ExampleExpected = "TestData/example.expected.csv";

    private string _dir = "";

    [TestInitialize]
    public void Initialize()
    {
        _dir = Path.Combine(Path.GetTempPath(), "DGBBoxes.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_dir, recursive: true);
    }

    [TestMethod]
    public void Run_Example_MatchesGoldenFile()
    {
        // Arrange - the golden file is the output of the previous version for example.csv
        string output = Path.Combine(_dir, "out.csv");

        // Act
        int exitCode = Run(out _, Example, output);

        // Assert
        exitCode.Should().Be(CommandLine.Success);
        File.ReadAllBytes(output).Should().Equal(File.ReadAllBytes(ExampleExpected));
    }

    [TestMethod]
    public void Run_CommaDecimalCulture_MatchesGoldenFile()
    {
        // Arrange
        string output = Path.Combine(_dir, "out.csv");
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("nl-NL");

        int exitCode;
        try
        {
            // Act
            exitCode = Run(out _, Example, output);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        // Assert
        exitCode.Should().Be(CommandLine.Success);
        File.ReadAllBytes(output).Should().Equal(File.ReadAllBytes(ExampleExpected));
    }

    [TestMethod]
    public void Run_QuotedFieldsAfterSpace_AreAccepted()
    {
        // Arrange
        string input = WriteInput("\"8\", \"-104.1\", \"-104.0\", \"39.8\", \"39.9\"", "END,,");
        string output = Path.Combine(_dir, "out.csv");

        // Act
        int exitCode = Run(out _, input, output);

        // Assert
        exitCode.Should().Be(CommandLine.Success);
        File.ReadAllText(output).Should().Be("-104.1, 39.8, -104.1, 39.9, -104, 39.9, -104, 39.8\r\n");
    }

    [TestMethod]
    [DataRow("0, -104,67, -104,66, 39,86, 39,87", "expected 5 fields", DisplayName = "comma decimals (used to convert to garbage)")]
    [DataRow("0, -104.1, -104.0, 39.8, 39.9, 7", "expected 5 fields", DisplayName = "six fields")]
    [DataRow("0, -104.1, -104.0, 39.8", "expected 5 fields", DisplayName = "four fields")]
    [DataRow("0, -104.1, , 39.8, 39.9", "maxLon \"\" is not a number", DisplayName = "empty field")]
    [DataRow("0, -104.1, abc, 39.8, 39.9", "maxLon \"abc\" is not a number", DisplayName = "non-numeric")]
    [DataRow("0, -104.1, NaN, 39.8, 39.9", "is not a number", DisplayName = "NaN")]
    [DataRow("0, -104.0, -104.1, 39.8, 39.9", "minLon -104 is greater than maxLon -104.1", DisplayName = "swapped longitudes")]
    [DataRow("0, -104.1, -104.0, 39.9, 39.8", "minLat 39.9 is greater than maxLat 39.8", DisplayName = "swapped latitudes")]
    [DataRow("0, 179.9, -179.9, 39.8, 39.9", "is greater than maxLon", DisplayName = "antimeridian box")]
    [DataRow("0, -104.1, -104.0, 39.8, 95", "maxLat 95 is outside -90..90", DisplayName = "latitude out of range")]
    public void Run_InvalidRow_FailsWithRowNumberAndWritesNothing(string badRow, string message)
    {
        // Arrange - the bad row is row 2
        string input = WriteInput("0, -104.2, -104.1, 39.8, 39.9", badRow, "END,,");
        string output = Path.Combine(_dir, "out.csv");

        // Act
        int exitCode = Run(out string stderr, input, output);

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.Should().Contain("row 2:").And.Contain(message);
        File.Exists(output).Should().BeFalse();
    }

    [TestMethod]
    public void Run_NoBoxes_Fails()
    {
        // Arrange - e.g. a semicolon-separated file used to "convert 0 polygons" successfully
        string input = WriteInput("END,,");
        string output = Path.Combine(_dir, "out.csv");

        // Act
        int exitCode = Run(out string stderr, input, output);

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.Should().Contain("no boxes");
        File.Exists(output).Should().BeFalse();
    }

    [TestMethod]
    [DataRow(new string[0], DisplayName = "no arguments")]
    [DataRow(new[] { "in.csv" }, DisplayName = "one argument")]
    [DataRow(new[] { "in.csv", "out.csv", "extra" }, DisplayName = "three arguments")]
    public void Run_WrongArgumentCount_FailsWithUsage(string[] args)
    {
        // Act
        var stderr = new StringWriter();
        int exitCode = CommandLine.Run(args, TextWriter.Null, stderr);

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.ToString().Should().Contain("Usage: AEDT.DGBBoxes.Converter.Cli");
    }

    [TestMethod]
    public void Run_MissingInputFile_Fails()
    {
        // Act
        int exitCode = Run(out string stderr, Path.Combine(_dir, "missing.csv"), Path.Combine(_dir, "out.csv"));

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.Should().StartWith("Error:").And.Contain("missing.csv");
    }

    [TestMethod]
    public void Run_OutputDirectoryMissing_Fails()
    {
        // Act
        int exitCode = Run(out string stderr, Example, Path.Combine(_dir, "no-such-dir", "out.csv"));

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.Should().StartWith("Error:");
    }

    private string WriteInput(params string[] lines)
    {
        string path = Path.Combine(_dir, "in.csv");
        File.WriteAllLines(path, lines);
        return path;
    }

    private static int Run(out string stderr, params string[] args)
    {
        var errors = new StringWriter();
        int exitCode = CommandLine.Run(args, TextWriter.Null, errors);
        stderr = errors.ToString();
        return exitCode;
    }
}
