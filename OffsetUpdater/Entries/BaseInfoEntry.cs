namespace OffsetUpdater.Entries;

internal sealed class BaseInfoEntry : OffsetEntry
{

    public override bool NamespaceExists() => false;

    public override void FixNamespace() => throw new NotImplementedException();

    public string Header = string.Empty;

    public override string ToString()
    {
        return $"{Header}({Offset}, {Name});";
    }

    public static bool operator ==(BaseInfoEntry left, BaseInfoEntry right)
    {
        return left.Name == right.Name;
    }

    public static bool operator !=(BaseInfoEntry left, BaseInfoEntry right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not BaseInfoEntry entry)
        {
            return false;
        }

        return this == entry;
    }

    public override int GetHashCode()
    {
        return Offset.GetHashCode();
    }
}