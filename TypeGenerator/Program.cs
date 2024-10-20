using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Il2CppInspector.Reflection;

namespace TypeGenerator;

internal class Program
{

    private static HashSet<TypeInfo> _declaredTypes = [];
    private static readonly HashSet<TypeInfo> _neededEnums = [];
    private static readonly Dictionary<TypeInfo, string> _neededClassesNames = [];

    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Need three args.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine($"    TypeGenerator.exe [binary file] [metadata file] [output dir]");
            return;
        }

        var binaryFile = args[0];
        var metadataFile = args[1];
        var outputDir = args[2];

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        Il2CppInspector.Il2CppInspector? inspector;
        try
        {
            var inspectors = Il2CppInspector.Il2CppInspector.LoadFromFile(binaryFile, metadataFile);
            inspector = inspectors[0];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return;
        }

        Console.WriteLine("Finding types with MessagePackObjectAttribute...");

        var typeModel = new TypeModel(inspector);

        var messagePackObjectAttribute = typeModel.GetType("MessagePack.MessagePackObjectAttribute");

        var messageObjects = typeModel.TypesByDefinitionIndex.Where(t => t.CustomAttributes.Any(attr => attr.AttributeType == messagePackObjectAttribute));
        Console.WriteLine($"Found {(messageObjects.TryGetNonEnumeratedCount(out var countOfMessageObjects) ? countOfMessageObjects : messageObjects.Count())} type(s).");

        Console.WriteLine("Analysing type dependencies...");

        _declaredTypes = messageObjects.ToHashSet();
        foreach (var message in messageObjects)
        {
            foreach (var field in message.DeclaredFields)
            {
                HandleMemberInfo(field);
            }

            foreach (var prop in message.DeclaredProperties)
            {
                HandleMemberInfo(prop);
            }
        }

        Console.WriteLine("Writing constructed types...");

        using var protoWriter = new StreamWriter(File.Open(Path.Combine(outputDir, "Types.cs"), FileMode.Create, FileAccess.ReadWrite, FileShare.Read));
        var proto = new StringBuilder();
        foreach (var message in messageObjects)
        {
            proto.Append("[global::MessagePack.MessagePackObject(true)]\n");
            proto.Append($"public partial class {message.CSharpName}\n{{\n");

            foreach (var field in message.DeclaredFields)
            {
                HandleMemberInfo(field, proto);
            }

            foreach (var prop in message.DeclaredProperties)
            {
                HandleMemberInfo(prop, proto);
            }

            proto.Append("}\n\n");

            protoWriter.Write(proto);

            proto.Clear();
        }

        Console.WriteLine("Writing needed enums...");

        foreach (var enumType in _neededEnums)
        {
            var isSystem = enumType.Namespace.StartsWith("System");
            proto.Append($"public enum {(isSystem ? FixEnumName(enumType.FullName) : _neededClassesNames[enumType])}\n{{\n");

            var enumNames = enumType.GetEnumNames();
            var enumValues = enumType.GetEnumValues();

            var index = 0;
            foreach (var enumValue in enumValues)
            {
                proto.Append($"    {enumNames[index]} = {enumValue},\n");
                ++index;
            }

            proto.Append("}\n\n");

            protoWriter.Write(proto);

            proto.Clear();
        }

        Console.WriteLine("Process done");
    }

    private static void HandleMemberInfo(MemberInfo info, StringBuilder? builder = null)
    {
        TypeInfo? underlyingType = null;

        if (info.MemberType == System.Reflection.MemberTypes.Property)
        {
            underlyingType = ((PropertyInfo)info).PropertyType;
        }
        else if (info.MemberType == System.Reflection.MemberTypes.Field)
        {
            underlyingType = ((FieldInfo)info).FieldType;
        }

        if (underlyingType is null)
        {
            return;
        }

        if (builder is null)
        {
            AnalyseDependencies(underlyingType);
            return;
        }
        else
        {
            if (_neededClassesNames.Count == 0)
            {
                throw new InvalidOperationException("Can not get mapped type name before dependencies analyse performed.");
            }
        }

        var unmannedTypeName = underlyingType.CSharpName;
        var mappedTypeName = "dynamic";
        ref var nameRef = ref CollectionsMarshal.GetValueRefOrNullRef(_neededClassesNames, underlyingType);
        if (underlyingType.Namespace.StartsWith("System"))
        {
            mappedTypeName = unmannedTypeName;
        }
        else if (!Unsafe.IsNullRef(ref nameRef))
        {
            mappedTypeName = nameRef;
        }

        builder.Append($"    public {mappedTypeName} {info.Name} {{ get; set; }}\n");
    }

    private static void AnalyseDependencies(TypeInfo type)
    {
        var isSystem = type.Namespace.StartsWith("System");

        if (type.IsEnum && !isSystem)
        {
            _neededEnums.Add(type);
        }

        if (type.ElementType is { } elementType)
        {
            AnalyseDependencies(elementType);
        }

        foreach (var genericParameter in type.GetGenericArguments())
        {
            AnalyseDependencies(genericParameter);
        }

        _neededClassesNames.TryAdd(type, GetMappedTypeName(type, isSystem));
    }

    private static string GetMappedTypeName(TypeInfo type, bool isSystem = false)
    {
        if (type.ElementType is { } elementType)
        {
            return type.CSharpName.Replace(elementType.CSharpName, GetMappedTypeName(elementType, isSystem));
        }

        var genericArguments = type.GetGenericArguments();
        if (genericArguments.Length > 0)
        {
            var mappedGenericArgumentNames = new List<string>();
            foreach (var genericParameter in genericArguments)
            {
                mappedGenericArgumentNames.Add(GetMappedTypeName(genericParameter, isSystem));
            }
            var index = 0;
            var count = mappedGenericArgumentNames.Count;
            var mappedNameBuilder = new StringBuilder(type.CSharpBaseName);
            mappedNameBuilder.Append('<');
            foreach (var mappedGenericArgumentName in mappedGenericArgumentNames)
            {
                if (index == count)
                {
                    break;
                }

                mappedNameBuilder.Append(mappedGenericArgumentName);

                if (index != (count - 1))
                {
                    mappedNameBuilder.Append(',');
                }

                ++index;
            }
            mappedNameBuilder.Append('>');
            return mappedNameBuilder.ToString();
        }

        if (type.IsEnum)
        {
            return FixEnumName(type.FullName);
        }

        if (isSystem)
        {
            return type.CSharpName;
        }

        if (_declaredTypes.Contains(type))
        {
            return type.CSharpName;
        }

        return "dynamic";
    }

    private static string FixEnumName(string enumName)
    {
        return enumName.Replace('+', '_').Replace('.', '_');
    }

}
