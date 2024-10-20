using System.Globalization;

namespace EnumGenerator;

internal static class IdentifierVerifier
{

    private static readonly List<Range> _standardRanges;
    private static readonly List<Range> _noLeadingRanges;

    static IdentifierVerifier()
    {
        _standardRanges = ParseRangesFromStringRange("0041-005A、005F、0061-007A、00A8、00AA、00AD、00AF、00B2-00B5、00B7-00BA、00BC-00BE、00C0-00D6、00D8-00F6、00F8-00FF、0100-02FF、0370-167F、1681-180D、180F-1DBF、1E00-1FFF、200B-200D、202A-202E、203F-2040、2054、2060-206F、2070-20CF、2100-218F、2460-24FF、2776-2793、2C00-2DFF、2E80-2FFF、3004-3007、3021-302F、3031-303F、3040-D7FF、F900-FD3D、FD40-FDCF、FDF0-FE1F、FE30-FE44、FE47-FFFD、10000-1FFFD、20000-2FFFD、30000-3FFFD、40000-4FFFD、50000-5FFFD、60000-6FFFD、70000-7FFFD、80000-8FFFD、90000-9FFFD、A0000-AFFFD、B0000-BFFFD、C0000-CFFFD、D0000-DFFFD、E0000-EFFFD");
        _noLeadingRanges = ParseRangesFromStringRange("0030-0039、0300-036F、1DC0-1DFF、20D0-20FF、FE20-FE2F");
    }

    public static bool VerifyChar(char c, bool first = false)
    {
        return first ? VerifyCore(_standardRanges, c) : (VerifyCore(_standardRanges, c) || VerifyCore(_noLeadingRanges, c));
    }

    public static bool IsValidIdentifier(this char c, bool first = false)
    {
        return VerifyChar(c, first);
    }

    private static bool VerifyCore(List<Range> ranges, int c)
    {
        return ranges.Any(range => c >= range.Start.Value && c <= range.End.Value);
    }

    private static List<Range> ParseRangesFromStringRange(string rangesStr)
    {
        var ranges = rangesStr.Split('、');
        List<Range> list = [];
        foreach (var range in ranges)
        {
            if (range.Contains('-'))
            {
                var splitRange = range.Split('-');
                list.Add(new Range(int.Parse(splitRange[0], NumberStyles.HexNumber), int.Parse(splitRange[1], NumberStyles.HexNumber)));
                continue;
            }
            var singleRange = int.Parse(range, NumberStyles.HexNumber);
            list.Add(new Range(singleRange, singleRange));
        }
        return list;
    }

}
