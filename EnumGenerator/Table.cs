using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EnumGenerator;

[DebuggerDisplay("Name = {_name}, HeaderCount = {_entries.Count}")]
internal class Table
{

    private static readonly SearchValues<char> _replaceChars = SearchValues.Create("·.- &");

    private readonly Dictionary<string, List<string>> _entries;
    private readonly string _name;

    public Table(string tableFile, string? requiredName)
    {
        if (!File.Exists(tableFile))
        {
            throw new FileNotFoundException(null, tableFile);
        }
        _name = tableFile[(tableFile.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];

        var lines = File.ReadAllLines(tableFile).AsSpan();
        if (lines.Length < 2)
        {
            throw new InvalidOperationException($"{tableFile} is invalid.");
        }
        var headers = lines[0].Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (requiredName is not null && !headers.Contains(requiredName))
        {
            throw new InvalidOperationException($"no required data found in {tableFile}");
        }
        lines = lines[1..];
        _entries = [];
        foreach (var header in headers)
        {
            if (!_entries.TryAdd(header, []))
            {
                throw new NotSupportedException("duplicate key are not supported.");
            }
        }

        var maxIndex = headers.Length;
        var placeHolder = '_';
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            var rawValues = line.Split('\t');
            for (int i = 0; i < maxIndex; i++)
            {
                ref var values = ref CollectionsMarshal.GetValueRefOrNullRef(_entries, headers[i])!;
                var value = rawValues[i];
                if (value.AsSpan().IndexOfAny(_replaceChars) != -1)
                {
                    value = value.Replace('·', placeHolder).Replace('.', placeHolder).Replace('-', placeHolder).Replace(' ', placeHolder).Replace('&', placeHolder);
                }
                values.Add(value);
            }
        }
    }

    public Span<string> GetEntries(string header)
    {
        return CollectionsMarshal.AsSpan(_entries[header]);
    }

}
