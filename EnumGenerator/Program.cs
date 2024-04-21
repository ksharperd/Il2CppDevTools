using System.Runtime.CompilerServices;
using System.Text;

namespace EnumGenerator;

internal class Program
{

    static void Main(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Need four args.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine($"    EnumGenerator.exe [character table file] [equip table file] [fashion table file] [output header file]");
            return;
        }

        var characterTableFile = args[0];
        var equipTableFile = args[1];
        var fashionTableFile = args[2];
        var output = args[3];

        ThrowIfFileNotFound(characterTableFile, nameof(characterTableFile));
        ThrowIfFileNotFound(equipTableFile, nameof(equipTableFile));
        ThrowIfFileNotFound(fashionTableFile, nameof(fashionTableFile));

        var characterTable = new Table(characterTableFile, "StoryChapterId");
        var characterIds = characterTable.GetEntries("Id");
        var characterNames = characterTable.GetEntries("LogName");

        var equipTable = new Table(equipTableFile, "DefaultLock");
        var equipIds = equipTable.GetEntries("Id");
        var equipNames = equipTable.GetEntries("LogName");

        var fashionTable = new Table(fashionTableFile, "Series");
        var fashionIds = fashionTable.GetEntries("Id");
        var fashionCharacterIds = fashionTable.GetEntries("CharacterId");
        var fashionRawNames = fashionTable.GetEntries("Name");
        var maxLength = fashionRawNames.Length;
        var fashionNamesArray = new string[maxLength];
        for (var i = 0; i < maxLength; i++)
        {
            fashionNamesArray[i] = $"{characterNames[characterIds.IndexOf(fashionCharacterIds[i])]}_{fashionRawNames[i]}";
        }
        var fashionNames = fashionNamesArray.AsSpan();

        var characterHeader = ConvertToHeader("Character", ref characterNames, ref characterIds);
        var equipHeader = ConvertToHeader("Equip", ref equipNames, ref equipIds);
        var fashionHeader = ConvertToHeader("Fashion", ref fashionNames, ref fashionIds);

        using var writer = new StreamWriter(File.Open(output, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false), leaveOpen: false);
        writer.WriteLine(characterHeader);
        writer.WriteLine();
        writer.WriteLine(equipHeader);
        writer.WriteLine();
        writer.WriteLine(fashionHeader);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfFileNotFound(string file, string name)
    {
        if (!File.Exists(file))
        {
            throw new FileNotFoundException($"{name} not found.");
        }
    }

    private static string ConvertToHeader(string name, ref Span<string> leftExp, ref Span<string> rightExp, string? inheritClass = "uint32_t")
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"enum class {name}{(inheritClass is not null ? " : " + inheritClass : "")}\n{{");
        var maxIndex = leftExp.Length;
        for (var i = 0; i < maxIndex; i++)
        {
            var left = leftExp[i];
            if (char.IsDigit(left[0]))
            {
                left = '_' + left;
            }
            stringBuilder.AppendLine($"\t{left} = {rightExp[i]},");
        }
        stringBuilder.Append("};");
        return stringBuilder.ToString();
    }

}
