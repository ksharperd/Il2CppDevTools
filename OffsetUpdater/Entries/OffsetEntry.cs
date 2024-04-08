using System.Buffers;

namespace OffsetUpdater.Entries;

internal abstract class OffsetEntry
{

    protected static readonly SearchValues<string> _defaultNamespaceSearchValues = SearchValues.Create(["::"], StringComparison.Ordinal);
    private static readonly SearchValues<string> _defaultSearchValues = SearchValues.Create(["DO_APP_FUNC(", "DO_APP_FUNC_METHODINFO("], StringComparison.Ordinal);

    public string Offset { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public abstract bool NamespaceExists();

    public abstract void FixNamespace();

    public override abstract string ToString();

    public abstract override bool Equals(object? obj);

    public abstract override int GetHashCode();

    public static OffsetEntry? Parse(string content)
    {
        if (!MemoryExtensions.ContainsAny(content, _defaultSearchValues))
        {
            return default;
        }
        OffsetEntry? result = null;
        if (content.StartsWith("DO_APP_FUNC("))
        {
            result = ParseFunctionEntry(content);
        }
        else if (content.StartsWith("DO_APP_FUNC_METHODINFO("))
        {
            result = ParseFunctionMethodInfoEntry(content);
        }
        if (result is null)
        {
            return default;
        }
        if (result.NamespaceExists())
        {
            result.FixNamespace();
        }
        return result;
    }

    public static List<OffsetEntry> ParseAll(string[] content)
    {
        var entries = new List<OffsetEntry>();
        foreach (var line in content)
        {
            var entry = Parse(line);
            if (entry is null)
            {
                continue;
            }
            entries.Add(entry);
        }
        return entries;
    }

    private static FunctionEntry ParseFunctionEntry(string content)
    {
        var entry = new FunctionEntry();
        var contents = content.Split(',');
        entry.Offset = contents[0].Replace("DO_APP_FUNC(", "");
        entry.ReturnType = contents[1].Trim();
        entry.Name = contents[2].Trim();
        var args = content.AsSpan()[(content.IndexOf(" (") + 2)..].ToString();
        entry.Args = args.Remove(args.IndexOf(')'));
        return entry;
    }

    private static FunctionMethodInfoEntry ParseFunctionMethodInfoEntry(string content)
    {
        var entry = new FunctionMethodInfoEntry();
        var contents = content.Split(',');
        entry.Offset = contents[0].Replace("DO_APP_FUNC_METHODINFO(", "");
        var name = contents[1];
        entry.Name = name[1..^2];
        return entry;
    }
}
