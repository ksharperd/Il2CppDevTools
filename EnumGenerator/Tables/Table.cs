using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EnumGenerator.Tables;

[DebuggerDisplay("Name = {_name}, RowCount = {RowCount}, LineCount = {LineCount}")]
internal class Table
{

    protected readonly Dictionary<string, List<string>> _entries;
    protected readonly string _name;

    public readonly int RowCount;
    public readonly int LineCount;

    public Table(string tableFile, string? requiredName, bool replaceInvalidChar = true)
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

        RowCount = _entries.Count;
        LineCount = 0;

        var maxIndex = headers.Length;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            ++LineCount;
            var rawValues = line.Split('\t');
            for (int i = 0; i < maxIndex; i++)
            {
                ref var values = ref CollectionsMarshal.GetValueRefOrNullRef(_entries, headers[i])!;
                var value = rawValues[i];
                var cpyValueSpan = new Span<char>(value.ToCharArray());
                if (replaceInvalidChar)
                {
                    for (int j = 0; j < cpyValueSpan.Length; j++)
                    {
                        if (!char.IsLetterOrDigit(cpyValueSpan[j]))
                        {
                            cpyValueSpan[j] = '_';
                        }
                    }
                }
                value = cpyValueSpan.ToString();
                values.Add(value);
            }
        }

        Console.WriteLine($"{tableFile} init done.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<string> GetRow(string header)
    {
        return CollectionsMarshal.AsSpan(_entries[header]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<string> GetLine(int index)
    {
        var lines = new List<string>();
        foreach (var (_, row) in _entries)
        {
            lines.Add(row[index]);
        }
        return CollectionsMarshal.AsSpan(lines);
    }

}
