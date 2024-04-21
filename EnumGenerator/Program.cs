using System.Runtime.CompilerServices;
using System.Text;

namespace EnumGenerator;

internal class Program
{

    static void Main(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Need five args.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine($"    EnumGenerator.exe [character table file] [equip table file] [fashion table file] [weapon fashion table file] [output header file]");
            return;
        }

        var characterTableFile = args[0];
        var equipTableFile = args[1];
        var fashionTableFile = args[2];
        var weaponFashionTableFile = args[3];
        var output = args[4];

        ThrowIfFileNotFound(characterTableFile);
        ThrowIfFileNotFound(equipTableFile);
        ThrowIfFileNotFound(fashionTableFile);
        ThrowIfFileNotFound(weaponFashionTableFile);

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
        var fashionNamesCnt = fashionRawNames.Length;
        var fashionNamesArray = new string[fashionNamesCnt];
        for (var i = 0; i < fashionNamesCnt; i++)
        {
            fashionNamesArray[i] = $"{characterNames[characterIds.IndexOf(fashionCharacterIds[i])]}_{fashionRawNames[i]}";
        }
        var fashionNames = fashionNamesArray.AsSpan();

        var weaponFashionTable = new Table(weaponFashionTableFile, "ResonanceModelTransId3[2]");
        var weaponFashionIds = weaponFashionTable.GetEntries("Id");
        var weaponFashionRawNames = weaponFashionTable.GetEntries("Name");
        var weaponFashionRawDescriptions = weaponFashionTable.GetEntries("Description");
        var weaponFashionNamesCnt = weaponFashionRawNames.Length;
        var weaponFashionNamesArray = new string[weaponFashionNamesCnt];
        for (var i = 0; i < weaponFashionNamesCnt; i++)
        {
            var weaponDescription = weaponFashionRawDescriptions[i];
            weaponFashionNamesArray[i] = $"{weaponDescription[(weaponDescription.LastIndexOf('—') + 1)..]}_{weaponFashionRawNames[i]}";
        }
        var weaponFashionNames = weaponFashionNamesArray.AsSpan();

        var characterHeader = ConvertToHeader("Character", ref characterNames, ref characterIds);
        var equipHeader = ConvertToHeader("Equip", ref equipNames, ref equipIds);
        var fashionHeader = ConvertToHeader("Fashion", ref fashionNames, ref fashionIds);
        var weaponFashionHeader = ConvertToHeader("WeaponFashion", ref weaponFashionNames, ref weaponFashionIds);

        using var writer = new StreamWriter(File.Open(output, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false), leaveOpen: false);
        writer.WriteLine(characterHeader);
        writer.WriteLine();
        writer.WriteLine(equipHeader);
        writer.WriteLine();
        writer.WriteLine(fashionHeader);
        writer.WriteLine();
        writer.WriteLine(weaponFashionHeader);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfFileNotFound(string file)
    {
        if (!File.Exists(file))
        {
            throw new FileNotFoundException("target table file not found.", file);
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
