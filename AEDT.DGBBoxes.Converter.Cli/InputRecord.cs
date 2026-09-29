namespace AEDT.DGBBoxes.Converter.Cli;

public class InputRecord
{
    public string Id { get; set; } = string.Empty;
    public double MinLon { get; set; }
    public double MaxLon { get; set; }
    public double MinLat { get; set; }
    public double MaxLat { get; set; }
}
