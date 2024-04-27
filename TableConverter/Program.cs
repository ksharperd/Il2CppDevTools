namespace TableConverter;

internal partial class Program
{

    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Need two args.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine($"    TableConverter.exe [input dir] [output dir]");
            return;
        }

        var input = args[0];
        var output = args[1];
        var renameOnly = (args.Length >= 3) && (args[2] == "--rename_only");

        if (!Directory.Exists(input))
        {
            Console.WriteLine("Input directory not found.");
            return;
        }
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var inputSuffix = renameOnly ? ".tab" : ".tab.bytes";
        var outputSuffix = ".tsv";

        var targetTableDes = new Dictionary<string, string>();
        foreach (var targetTableFile in Directory.GetFiles(output, "*.tsv", SearchOption.AllDirectories))
        {
            targetTableDes.Add(Path.GetRelativePath(output, targetTableFile), new FileInfo(targetTableFile).Name.Replace(outputSuffix, string.Empty));
        }
        var targetTableParents = targetTableDes.Values;

        foreach (var rawTableFile in Directory.GetFiles(input, renameOnly ? "*.tab" : "*.tab.bytes", SearchOption.AllDirectories))
        {
            var rawTableFileInfo = new FileInfo(rawTableFile);
            var shortFileName = rawTableFileInfo.Name.Replace(inputSuffix, string.Empty);
            var conflict = (shortFileName.Count(char.IsNumber) == shortFileName.Length) && targetTableParents.Any(parent => string.Equals(parent, Directory.GetParent(rawTableFile)!.Name, StringComparison.OrdinalIgnoreCase));

            var fakeFileName = rawTableFile.Replace(rawTableFileInfo.Name, string.Empty);
            fakeFileName = fakeFileName.Remove(fakeFileName.Length - 1) + outputSuffix;

            IEnumerable<KeyValuePair<string, string>> results = [];
            if (!(results = targetTableDes.Where(v => (conflict ? Path.GetRelativePath(input, fakeFileName) : rawTableFile.Replace(inputSuffix, outputSuffix)).EndsWith(v.Key, StringComparison.OrdinalIgnoreCase))).Any())
            {
                continue;
            }

            var result = results.First();
            var targetFileName = result.Key;

            Console.WriteLine($"Processing {targetFileName}");
            if (conflict)
            {
                Console.WriteLine($"Conflict between {targetFileName} and {Path.GetRelativePath(input, rawTableFile)}");
            }

            var newFilename = Path.Combine(output, targetFileName);
            var newFileInfo = new FileInfo(newFilename);
            if (!Directory.Exists(newFileInfo.Directory!.FullName))
            {
                Directory.CreateDirectory(newFileInfo.Directory!.FullName);
            }

            if (renameOnly)
            {
                File.Copy(rawTableFile, newFilename, true);
                continue;
            }

            var bytes = File.ReadAllBytes(rawTableFile);
            var newBytes = new byte[bytes.Length - 128];
            Array.Copy(bytes, 128, newBytes, 0, newBytes.Length);
            File.WriteAllBytes(newFilename, newBytes);

            Console.WriteLine($"Processed {rawTableFile}");
        }
    }

}
