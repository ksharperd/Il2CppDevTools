using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace EnumGenerator.Tables;

internal sealed class ModelTable : Table
{

    public readonly Table NpcTable;
    public readonly Dictionary<string, List<(string, string)>> NpcModelMap;   // <NpcId, List<(ModelId, PrefabPath)>>

    public ModelTable(string tableFile, string npcTableFile) : base(tableFile, "ModelPath", false)
    {
        NpcTable = new Table(npcTableFile, "ModelId");
        NpcModelMap = [];

        var npcModelIdToPrefabPathMap = new Dictionary<string, List<(string, string)>>();
        var npcModelIdToNpcIdMap = new Dictionary<string, List<string>>();

        for (int i = 0; i < LineCount; i++)
        {
            var line = GetLine(i);
            var modelId = line[0];
            var prefabPath = line[1];
            modelId = CutModelId(modelId, out var cutIndex);
            var subModelId = cutIndex != -1 ? line[0][cutIndex..] : string.Empty;
            ref var info = ref CollectionsMarshal.GetValueRefOrAddDefault(npcModelIdToPrefabPathMap, modelId, out var exists);
            if (!exists)
            {
                info = [];
            }
            info!.Add((subModelId, prefabPath));
        }
        for (int i = 0; i < NpcTable.LineCount; i++)
        {
            var line = NpcTable.GetLine(i);
            var npcId = line[0];
            var npcModelId = line[13];
            npcModelId = CutModelId(npcModelId, out _);
            ref var npcIds = ref CollectionsMarshal.GetValueRefOrAddDefault(npcModelIdToNpcIdMap, npcModelId, out var exists);
            if (!exists)
            {
                npcIds = [];
            }
            npcIds!.Add(npcId);
        }

        foreach (var (modelId, modelInfo) in npcModelIdToPrefabPathMap)
        {
            ref var npcIds = ref CollectionsMarshal.GetValueRefOrNullRef(npcModelIdToNpcIdMap, modelId);
            if (Unsafe.IsNullRef(ref npcIds))
            {
                continue;
            }

            foreach (var npcId in npcIds)
            {
                ref var npcModelInfoList = ref CollectionsMarshal.GetValueRefOrAddDefault(NpcModelMap, npcId, out var exists);
                if (!exists)
                {
                    npcModelInfoList = [];
                }

                foreach (var (modelSubId, prefabPath) in modelInfo)
                {
                    npcModelInfoList!.Add(($"{modelId}{modelSubId}", prefabPath));
                }
            }
        }
    }

    public Span<string> GetModelInformationMap()
    {
        List<string> infoList = [];
        var line = new StringBuilder();
        foreach (var (_, modelInfo) in NpcModelMap)
        {
            line.Append('{');
            var infoCnt = modelInfo.Count;
            for (var i = 0; i < infoCnt; i++)
            {
                var (modelId, prefabPath) = modelInfo[i];
                line.Append($"{{\"{modelId}\", \"{prefabPath}\"}}");
                if ((i + 1) != infoCnt)
                {
                    line.Append(", ");
                }
            }
            line.Append('}');
            infoList.Add(line.ToString());
            line.Length = 0;
        }
        return CollectionsMarshal.AsSpan(infoList);
    }

    public Span<string> GetNpcNamesFixed()
    {
        var npcNames = NpcTable.GetRow("Name");
        var npcIds = NpcTable.GetRow("Id");
        for (var i = 0; i < npcNames.Length;)
        {
            ref var npcName = ref npcNames[i];
            if (!npcName[0].IsValidIdentifier(true))
            {
                npcName = string.Concat("_", npcName); ;
            }
            if (npcName.EndsWith("S_"))
            {
                npcName = npcName.Replace("S_", "SS");
            }
            // workaround: typo found in game data table :(
            if (npcName.Contains("_自控怪）"))
            {
                npcName = npcName.Replace("_自控怪）", "（自控怪）");
            }
            if (npcName != ((i + 1) == npcNames.Length ? string.Empty : npcNames[i + 1]))
            {
                ++i;
                continue;
            }
            var startIndex = i;
            var endIndex = startIndex;
            var npcIdPrefix = npcIds[startIndex][..5];
            while (npcIdPrefix == ((++endIndex) == npcIds.Length ? string.Empty : npcIds[endIndex][..5]))
            {
                continue;
            }
            var count = endIndex - startIndex;
            for (int j = 0; j < count; j++)
            {
                ref var name = ref npcNames[startIndex++];
                name = $"{name}_{j}";
            }
            i += count;
        }
        return npcNames;
    }

    private static string CutModelId(string modelName, out int cutIndex)
    {
        cutIndex = modelName.IndexOf("Md");
        if (cutIndex == -1)
        {
            cutIndex = modelName.IndexOf('_');
        }
        return cutIndex != -1 ? modelName.Remove(cutIndex) : modelName;
    }

}
