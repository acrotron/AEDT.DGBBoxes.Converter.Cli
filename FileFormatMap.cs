using CsvHelper.Configuration;

namespace AEDT.DGBBoxes.Converter.Cli;

public sealed class FileFormatMap : ClassMap<InputRecord>
{
    public FileFormatMap()
    {
        Map(m => m.Id).Index(0);
        Map(m => m.MinLon).Index(1);
        Map(m => m.MaxLon).Index(2);
        Map(m => m.MinLat).Index(3);
        Map(m => m.MaxLat).Index(4);
    }
}
