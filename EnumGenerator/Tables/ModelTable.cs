using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace EnumGenerator.Tables;

internal sealed class ModelTable : Table
{

    private readonly Table _npcTable;
    public readonly Dictionary<string, List<(string, string)>> NpcModelMap;   // <NpcId, List<(ModelId, PrefabPath)>>

    public ModelTable(string tableFile, string npcTableFile) : base(tableFile, "ModelPath", false)
    {
        _npcTable = new Table(npcTableFile, "ModelId");
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
        for (int i = 0; i < _npcTable.LineCount; i++)
        {
            var line = _npcTable.GetLine(i);
            var npcId = line[0];
            var npcModelId = line[11];
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

            var npcIdCnt = npcIds.Count;
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
        foreach (var (_, modelInfo) in NpcModelMap)
        {
            var line = new StringBuilder();
            line.Append('{');
            var infoCnt = modelInfo.Count;
            for (var i = 0; i < infoCnt; i++)
            {
                var (modelId, prefabPath) = modelInfo[i];
                line.Append($"{{\"{modelId}\", \"{prefabPath}\"}}");
                if (i + 1 != infoCnt)
                {
                    line.Append(", ");
                }
            }
            line.Append('}');
            infoList.Add(line.ToString());
        }
        return CollectionsMarshal.AsSpan(infoList);
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
