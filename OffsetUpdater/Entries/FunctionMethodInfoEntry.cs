namespace OffsetUpdater.Entries;

internal sealed class FunctionMethodInfoEntry : OffsetEntry
{

    public override bool NamespaceExists() => false;

    public override void FixNamespace() => throw new NotImplementedException();

    public override string ToString()
    {
        return $"DO_APP_FUNC_METHODINFO({Offset}, {Name});";
    }

    public static bool operator ==(FunctionMethodInfoEntry left, FunctionMethodInfoEntry right)
    {
        return left.Name == right.Name;
    }

    public static bool operator !=(FunctionMethodInfoEntry left, FunctionMethodInfoEntry right)
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

        if (obj is not FunctionMethodInfoEntry entry)
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