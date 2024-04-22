using System.Runtime.InteropServices;

namespace EnumGenerator.Tables;

internal sealed class CharacterSkillTable : Table
{

    private readonly UpgradeDesTable _upgradeDesTable;
    private readonly Dictionary<string, List<string>> _characterSkills;
    public readonly int TotalSkills;

    public CharacterSkillTable(string tableFile, string upgradeDesTable) : base(tableFile, "SkillGroupId[9]")
    {
        _upgradeDesTable = new(upgradeDesTable);
        _characterSkills = [];
        for (int i = 0; i < LineCount; i++)
        {
            var line = GetLine(i);
            var characterId = line[0];
            var skillIds = new List<string>();
            for (int j = RowCount - 1; j >= 1; j -= 2)
            {
                var id = line[j];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }
                skillIds.Add(id.Remove(id.Length - 1));
            }
            _characterSkills.TryAdd(characterId, skillIds);
            TotalSkills += skillIds.Count;
        }
    }

    public string this[string skillId] => LookupSkillById(skillId);

    public Span<string> GetSkillLevelsById(string skillId)
    {
        var levels = _upgradeDesTable.GetSkillLevelsById(skillId).ToList();
        return CollectionsMarshal.AsSpan(levels);
    }

    /// <returns>format (min, max)</returns>
    public (int, int) GetSkillLevelInfoById(string skillId)
    {
        return _upgradeDesTable.GetLevelInfo(skillId);
    }

    public Span<string> GetSkillsByCharacterId(string characterId)
    {
        var skills = _characterSkills[characterId];
        return CollectionsMarshal.AsSpan(skills);
    }

    public Span<string> GetCharacterIds()
    {
        var characterIds = new List<string>();
        foreach (var (id, skills) in _characterSkills)
        {
            characterIds.AddRange(Enumerable.Repeat(id, skills.Count));
        }
        return CollectionsMarshal.AsSpan(characterIds);
    }

    private string LookupSkillById(string skillId)
    {
        return _upgradeDesTable.GetSkillNameById(skillId);
    }

    private sealed class UpgradeDesTable : Table
    {

        private readonly Dictionary<string, (string, (int, int))> _upgradeDes;

        public UpgradeDesTable(string tableFile) : base(tableFile, "SkillId")
        {
            _upgradeDes = [];
            var skillIds = new HashSet<string>();
            foreach (var skillId in GetRow("SkillId"))
            {
                skillIds.Add(skillId);
            }
            foreach (var skillId in skillIds)
            {
                _upgradeDes.TryAdd(skillId, (FirstValueByIndex(this, skillId, 1, 3), GetLevelInfo(skillId)));
            }
        }

        public IEnumerable<string> GetSkillLevelsById(string skillId)
        {
            var (name, (min, max)) = _upgradeDes[skillId];
            for (var i = min; i <= max; i++)
            {
                yield return $"{name}_LV{i}";
            }
        }

        public string GetSkillNameById(string skillId)
        {
            return _upgradeDes[skillId].Item1;
        }

        public (int, int) GetLevelInfo(string skillId)
        {
            bool found = false;
            var minLevel = "-1";
            var maxLevel = "0";
            for (int i = 0; i < LineCount; i++)
            {
                var line = GetLine(i);
                if (line[1] == skillId)
                {
                    found = true;
                    if (minLevel == "-1")
                    {
                        minLevel = line[2];
                    }
                    maxLevel = line[2];
                }
                else if (found)
                {
                    break;
                }
            }
            return (int.Parse(minLevel), int.Parse(maxLevel));
        }

    }

    private static string FirstValueByIndex(Table table, string value, int valueIndex, int index)
    {
        for (int i = 0; i < table.LineCount; i++)
        {
            var line = table.GetLine(i);
            if (line[valueIndex] == value)
            {
                return line[index];
            }
        }
        return string.Empty;
    }

}
