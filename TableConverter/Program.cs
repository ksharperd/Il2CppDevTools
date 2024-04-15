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

        if (!Directory.Exists(input))
        {
            Console.WriteLine("Input directory not found.");
            return;
        }
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var rawTableFiles = Directory.GetFiles(input, "*.tab.bytes", SearchOption.AllDirectories);
        foreach (var fileName in rawTableFiles)
        {
            var tempFileName = fileName.Replace(input, string.Empty).Replace(".tab.bytes", ".tsv");
            if (tempFileName.StartsWith(Path.DirectorySeparatorChar))
            {
                tempFileName = tempFileName.Remove(0, 1);
            }
            var newFilename = Path.Combine(output, tempFileName);
            var newFileInfo = new FileInfo(newFilename);
            if (!Directory.Exists(newFileInfo.Directory!.FullName))
            {
                Directory.CreateDirectory(newFileInfo.Directory!.FullName);
            }

            var bytes = File.ReadAllBytes(fileName);
            var newBytes = new byte[bytes.Length - 128];
            Array.Copy(bytes, 128, newBytes, 0, newBytes.Length);
            File.WriteAllBytes(newFilename, newBytes);

            Console.WriteLine($"Processed {fileName}");
        }
    }

}
