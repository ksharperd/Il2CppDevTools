using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using EnumGenerator.Tables;

namespace EnumGenerator;

internal class Program
{

    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Need two args.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine($"    EnumGenerator.exe [table dir] [output header file]");
            return;
        }

        var tableDir = args[0];
        if (!Directory.Exists(tableDir))
        {
            Console.WriteLine("Table directory not found.");
            return;
        }

        var characterTableFile = Path.Combine(tableDir, "Share", "Character", "Character.tab");
        var equipTableFile = Path.Combine(tableDir, "Share", "Equip", "Equip.tab");
        var fashionTableFile = Path.Combine(tableDir, "Share", "Fashion", "Fashion.tab");
        var weaponFashionTableFile = Path.Combine(tableDir, "Client", "WeaponFashion", "WeaponFashionRes.tab");
        // Character Skills
        var characterSkillTableFile = Path.Combine(tableDir, "Share", "Character", "Skill", "CharacterSkill.tab");
        var characterSkillPoolTableFile = Path.Combine(tableDir, "Client", "Character", "Skill", "CharacterSkillUpgradeDes.tab");
        // Enhance Skills
        var enhanceSkillTableFile = Path.Combine(tableDir, "Share", "Character", "EnhanceSkill", "EnhanceSkill.tab");
        var enhanceSkillPoolTableFile = Path.Combine(tableDir, "Client", "Character", "EnhanceSkill", "EnhanceSkillUpgradeDes.tab");
        // Npc Model Map
        var modelTableFile = Path.Combine(tableDir, "Client", "ResourceLut", "Model", "Model.tab");
        var npcTableFile = Path.Combine(tableDir, "Share", "Fight", "Npc", "Npc", "Npc.tab");

        var output = args[1];

        ThrowIfPathNotFound(characterTableFile);
        ThrowIfPathNotFound(equipTableFile);
        ThrowIfPathNotFound(fashionTableFile);
        ThrowIfPathNotFound(weaponFashionTableFile);
        ThrowIfPathNotFound(characterSkillTableFile);
        ThrowIfPathNotFound(characterSkillPoolTableFile);
        ThrowIfPathNotFound(modelTableFile);
        ThrowIfPathNotFound(npcTableFile);

        var characterTable = new Table(characterTableFile, "StoryChapterId");
        var characterIds = characterTable.GetRow("Id");
        var characterNames = characterTable.GetRow("LogName");

        var equipTable = new Table(equipTableFile, "DefaultLock");
        var equipIds = equipTable.GetRow("Id");
        var equipNames = equipTable.GetRow("LogName");

        var fashionTable = new Table(fashionTableFile, "Series");
        var fashionIds = fashionTable.GetRow("Id");
        var fashionCharacterIds = fashionTable.GetRow("CharacterId");
        var fashionRawNames = fashionTable.GetRow("Name");
        var fashionNamesCnt = fashionRawNames.Length;
        var fashionNamesArray = new string[fashionNamesCnt];
        for (var i = 0; i < fashionNamesCnt; i++)
        {
            fashionNamesArray[i] = $"{characterNames[characterIds.IndexOf(fashionCharacterIds[i])]}_{fashionRawNames[i]}";
        }
        var fashionNames = fashionNamesArray.AsSpan();

        var weaponFashionTable = new Table(weaponFashionTableFile, "ResonanceModelTransId3[2]");
        var weaponFashionIds = weaponFashionTable.GetRow("Id");
        var weaponFashionRawNames = weaponFashionTable.GetRow("Name");
        var weaponFashionRawDescriptions = weaponFashionTable.GetRow("Description");
        var weaponFashionNamesCnt = weaponFashionRawNames.Length;
        var weaponFashionNamesArray = new string[weaponFashionNamesCnt];
        for (var i = 0; i < weaponFashionNamesCnt; i++)
        {
            var weaponDescription = weaponFashionRawDescriptions[i];
            weaponFashionNamesArray[i] = $"{weaponDescription[(weaponDescription.LastIndexOf('—') + 1)..]}_{weaponFashionRawNames[i]}";
        }
        var weaponFashionNames = weaponFashionNamesArray.AsSpan();

        var characterSkillTable = new SkillTable(characterSkillTableFile, characterSkillPoolTableFile);
        var (characterSkillIdsList, characterSkillNamesList) = HandleSkillTable(characterSkillTable, ref characterNames, ref characterIds);
        var characterSkillIds = CollectionsMarshal.AsSpan(characterSkillIdsList);
        var characterSkillNames = CollectionsMarshal.AsSpan(characterSkillNamesList);

        var enhanceSkillTable = new SkillTable(enhanceSkillTableFile, enhanceSkillPoolTableFile);
        var (enhanceSkillIdsList, enhanceSkillNamesList) = HandleSkillTable(enhanceSkillTable, ref characterNames, ref characterIds);
        var enhanceSkillIds = CollectionsMarshal.AsSpan(enhanceSkillIdsList);
        var enhanceSkillNames = CollectionsMarshal.AsSpan(enhanceSkillNamesList);

        var npcModelTable = new ModelTable(modelTableFile, npcTableFile);
        var npcModelIds = CollectionsMarshal.AsSpan(npcModelTable.NpcModelMap.Keys.ToList());
        var npcModelInfo = npcModelTable.GetModelInformationMap();

        var npcNames = npcModelTable.GetNpcNamesFixed();
        var npcIds = npcModelTable.NpcTable.GetRow("Id");
        foreach (var npcModelId in npcModelIds)
        {
            ref var npcName = ref npcNames[npcIds.IndexOf(npcModelId)];
            npcModelIds.Replace(npcModelId, $"Npc::{npcName}");
        }

        var characterEnum = ConvertToCppEnum("Character", ref characterNames, ref characterIds);
        var equipEnum = ConvertToCppEnum("Equip", ref equipNames, ref equipIds);
        var fashionEnum = ConvertToCppEnum("Fashion", ref fashionNames, ref fashionIds);
        var weaponFashionEnum = ConvertToCppEnum("WeaponFashion", ref weaponFashionNames, ref weaponFashionIds);
        var characterSkillEnum = ConvertToCppEnum("CharacterSkill", ref characterSkillNames, ref characterSkillIds);
        var enhanceSkillEnum = ConvertToCppEnum("EnhanceSkill", ref enhanceSkillNames, ref enhanceSkillIds);
        var npcEnum = ConvertToCppEnum("Npc", ref npcNames, ref npcIds);

        var npcModelMap = ConvertToCppMap("NpcModelMap", ref npcModelIds, ref npcModelInfo, "Npc", "std::vector<std::pair<std::string, std::string>>");

        using var writer = new StreamWriter(File.Open(output, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false), leaveOpen: false);
        WriteHeader(writer, characterEnum);
        WriteHeader(writer, equipEnum);
        WriteHeader(writer, fashionEnum);
        WriteHeader(writer, weaponFashionEnum);
        WriteHeader(writer, characterSkillEnum);
        WriteHeader(writer, enhanceSkillEnum);
        WriteHeader(writer, npcEnum);
        WriteHeader(writer, npcModelMap, false);

        Console.WriteLine($"Generate success.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfPathNotFound(string path)
    {
        if (!Path.Exists(path))
        {
            throw new IOException($"target path not found: {path}");
        }
    }

    private static string ConvertToCppEnum(string name, ref Span<string> leftExp, ref Span<string> rightExp, string? inheritClass = "uint32_t")
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"enum class {name}{(inheritClass is not null ? " : " + inheritClass : "")}\n{{");
        var maxIndex = leftExp.Length;
        for (var i = 0; i < maxIndex; i++)
        {
            var left = leftExp[i];
            if (!left[0].IsValidIdentifier(true))
            {
                left = string.Concat("_", left);
            }
            stringBuilder.AppendLine($"\t{left} = {rightExp[i]},");
        }
        stringBuilder.Append("};");

        Console.WriteLine($"Processed {name}");
        return stringBuilder.ToString();
    }

    private static string ConvertToCppMap(string name, ref Span<string> keyExp, ref Span<string> valueExp, string keyClass = "uint32_t", string valueClass = "std::string")
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"static const std::map<{keyClass}, {valueClass}> {name}\n{{");
        var maxIndex = keyExp.Length;
        for (var i = 0; i < maxIndex; i++)
        {
            stringBuilder.AppendLine($"\t{{{keyExp[i]}, {valueExp[i]}}},");
        }
        stringBuilder.Append("};");

        Console.WriteLine($"Processed {name}");
        return stringBuilder.ToString();
    }

    private static (List<string>, List<string>) HandleSkillTable(SkillTable skillTable, ref Span<string> namesTable, ref Span<string> idsTable)
    {
        var skillIdsList = new List<string>(skillTable.TotalSkills);
        var skillNamesList = new List<string>(skillTable.TotalSkills);
        foreach (var characterId in idsTable)
        {
            var skillIds = skillTable.GetSkillsByCharacterId(characterId);
            skillIdsList.AddRange(skillIds);
            foreach (var skillId in skillIds)
            {
                skillNamesList.Add($"{namesTable[idsTable.IndexOf(characterId)]}_{skillTable[skillId]}");
            }
        }
        return (skillIdsList, skillNamesList);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteHeader(StreamWriter writer, string content, bool writeNewLine = true)
    {
        writer.WriteLine(content);
        if (writeNewLine)
        {
            writer.WriteLine();
        }
    }

}
