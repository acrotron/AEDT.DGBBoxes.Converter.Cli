namespace AEDT.DGBBoxes.Converter.Cli;

/// <summary>
/// Thrown when a row of an old-format DGBBoxes file is invalid.
/// </summary>
public sealed class DgbBoxesFormatException : Exception
{
    /// <summary>
    /// Creates an exception for an invalid row.
    /// </summary>
    /// <param name="row">1-based row number in the input file.</param>
    /// <param name="reason">Why the row is invalid.</param>
    /// <param name="innerException">The underlying parser exception, if any.</param>
    public DgbBoxesFormatException(int row, string reason, Exception? innerException = null)
        : base($"row {row}: {reason}", innerException)
    {
        Row = row;
    }

    /// <summary>
    /// 1-based row number in the input file.
    /// </summary>
    public int Row { get; }
}
