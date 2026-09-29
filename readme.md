# AEDT DGBBoxes converter

This is a converter for DGBBoxes, which is an old AEDT (https://aedt.faa.gov) format, to the new AEDT format.

## Formats

Old format, one box per row, fields separated by `,` (spaces and quotes around fields are allowed):

```
id, minLon, maxLon, minLat, maxLat
```

New format, one box per row with its four corners (bottom-left, top-left, top-right, bottom-right) as
longitude/latitude pairs, separated by `, `; the id is not written:

```
minLon, minLat, minLon, maxLat, maxLon, maxLat, maxLon, minLat
```

Blank lines and the `END` trailer line are skipped. Any other invalid row (not exactly five fields, a coordinate that
is not a number or is out of range, a minimum greater than its maximum) stops the conversion with an error that names
the row; no output file is written then.

## Usage

```bash
AEDT.DGBBoxes.Converter.Cli <inputFile> <outputFile>
```

| Exit code | Meaning                                                            |
|-----------|--------------------------------------------------------------------|
| 0         | The output file was written                                        |
| 1         | Invalid arguments, an unreadable file, or an invalid row          |

Errors are written to stderr.

## Build and test

```bash
dotnet build
dotnet test
```

## License

MIT
