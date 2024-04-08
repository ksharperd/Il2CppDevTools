using System.Text;

namespace OffsetUpdater.Entries;

internal sealed class FunctionEntry : OffsetEntry
{

    public string ReturnType = string.Empty;

    public string Args = string.Empty;

    private bool _namespaceFixed = false;
    private bool _hasNamespaceInReturnType = false;
    private bool _hasNamespaceInArgs = false;
    private List<string>? _namespacesReturnType;
    private List<string>? _namespacesArgs;
    private readonly object _lock = new();

    public override bool NamespaceExists()
    {
        if (_namespaceFixed)
        {
            return true;
        }
        _hasNamespaceInReturnType = MemoryExtensions.ContainsAny(ReturnType, _defaultNamespaceSearchValues);
        _hasNamespaceInArgs = MemoryExtensions.ContainsAny(Args, _defaultNamespaceSearchValues);
        return _hasNamespaceInReturnType || _hasNamespaceInArgs;
    }

    public override void FixNamespace()
    {
        lock (_lock)
        {
            if (_namespaceFixed)
            {
                return;
            }
            _namespaceFixed = true;
            if (_hasNamespaceInReturnType)
            {
                _namespacesReturnType = [];
                RemoveOrAddNamespaces(ref ReturnType, ref _namespacesReturnType);
            }
            if (_hasNamespaceInArgs)
            {
                _namespacesArgs = [];
                RemoveOrAddNamespaces(ref Args, ref _namespacesArgs);
            }
        }
    }

    public override string ToString()
    {
        if (_hasNamespaceInReturnType || _hasNamespaceInArgs)
        {
            if (!_namespaceFixed)
            {
                throw new InvalidOperationException("call FixNamespace() first.");
            }
            var returnType = ReturnType;
            if (_hasNamespaceInReturnType)
            {
                if (_namespacesReturnType is null)
                {
                    throw new InvalidOperationException("call FixNamespace() first.");
                }
                RemoveOrAddNamespaces(ref returnType, ref _namespacesReturnType, false);
            }
            var args = Args;
            if (_hasNamespaceInArgs)
            {
                if (_namespacesArgs is null)
                {
                    throw new InvalidOperationException("call FixNamespace() first.");
                }
                RemoveOrAddNamespaces(ref args, ref _namespacesArgs, false);
            }
            return $"DO_APP_FUNC({Offset}, {returnType}, {Name}, ({args}));";
        }
        return $"DO_APP_FUNC({Offset}, {ReturnType}, {Name}, ({Args}));";
    }

    public static bool operator ==(FunctionEntry left, FunctionEntry right)
    {
        return (left.ReturnType == right.ReturnType) && (left.Name == right.Name) && (left.Args == right.Args);
    }

    public static bool operator !=(FunctionEntry left, FunctionEntry right)
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

        if (obj is not FunctionEntry entry)
        {
            return false;
        }

        return this == entry;
    }

    public override int GetHashCode()
    {
        return Offset.GetHashCode();
    }

    private static bool RemoveOrAddNamespaces(ref string field, ref List<string> namespaces, bool remove = true)
    {
        var contents = field.Split(',');
        if (contents.Length < 2)
        {
            return remove ? RemoveNamespace(ref field, ref namespaces) : AddNamespace(ref field, ref namespaces, 0);
        }
        var builder = new StringBuilder();
        var subContent = string.Empty;
        int index = 0;
        foreach (var content in contents)
        {
            subContent = content;
            _ = remove ? RemoveNamespace(ref subContent, ref namespaces) : AddNamespace(ref subContent, ref namespaces, index);
            builder.Append(subContent + ",");
            ++index;
        }
        builder.Remove(builder.Length - 1, 1);
        field = builder.ToString();
        return true;
    }

    private static bool RemoveNamespace(ref string subContent, ref List<string> namespaces)
    {
        var index = MemoryExtensions.IndexOf(subContent, "::", StringComparison.Ordinal);
        if (index < 0)
        {
            namespaces.Add(string.Empty);
            return false;
        }
        namespaces.Add(subContent[..index]);
        subContent = subContent[(index + 2)..];
        return true;
    }

    private static bool AddNamespace(ref string subContent, ref List<string> namespaces, int index)
    {
        if ((index < 0) || (namespaces.Count < 1) || (namespaces.Count <= index))
        {
            return false;
        }
        var @namespace = index == 0 ? namespaces.First() : namespaces[index];
        if (@namespace == string.Empty)
        {
            return false;
        }
        subContent = index == 0 ? $"{@namespace}::{subContent}" : $"{@namespace}::{subContent}";
        return true;
    }
}
