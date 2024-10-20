using System.Text;

using Il2CppInspector.Reflection;

namespace TypeGenerator;

internal class Program
{

    private static Il2CppInspector.Il2CppInspector? _inspector;

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

        try
        {
            var inspectors = Il2CppInspector.Il2CppInspector.LoadFromFile(binaryFile, metadataFile);
            _inspector = inspectors[0];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return;
        }

        Console.WriteLine("Finding types with MessagePackObjectAttribute...");

        var typeModel = new TypeModel(_inspector);

        var messagePackObjectAttribute = typeModel.GetType("MessagePack.MessagePackObjectAttribute");

        var messageObjects = typeModel.TypesByDefinitionIndex.Where(t => t.CustomAttributes.Any(attr => attr.AttributeType == messagePackObjectAttribute));
        Console.WriteLine($"Found {(messageObjects.TryGetNonEnumeratedCount(out var countOfMessageObjects) ? countOfMessageObjects : messageObjects.Count())} type(s).");

        Console.WriteLine("Writing constructed types...");

        using var protoWriter = new StreamWriter(File.Open(Path.Combine(outputDir, "Types.cs"), FileMode.Create, FileAccess.ReadWrite, FileShare.Read));
        foreach (var message in messageObjects)
        {
            var name = message.CSharpName;
            var fields = message.DeclaredFields;
            var props = message.DeclaredProperties;

            var proto = new StringBuilder();
            proto.Append("[global::MessagePack.MessagePackObject(true)]\n");
            proto.Append($"public partial class {name}\n{{\n");

            foreach (var field in fields)
            {
                proto.Append($"    public {field.FieldType.CSharpName} {field.Name} {{ get; set; }}\n");
            }

            foreach (var prop in props)
            {
                proto.Append($"    public {prop.PropertyType.CSharpName} {prop.Name} {{ get; set; }}\n");
            }

            proto.Append("}\n\n");

            protoWriter.Write(proto);
        }

        Console.WriteLine("Process done");
    }
}
