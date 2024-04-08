using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using OffsetUpdater.Entries;

namespace OffsetUpdater;

internal partial class Program
{

    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Need two args.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine($"    OffsetUpdater.exe [input file] [output file]");
            return;
        }

        var input = args[0];
        var output = args[1];

        if (!File.Exists(input))
        {
            Console.WriteLine("Input file not found.");
            return;
        }
        if (!File.Exists(output))
        {
            Console.WriteLine("Output file not found.");
            return;
        }

        var inputLines = File.ReadAllLines(input);
        var outputLines = File.ReadAllLines(output);
        var inputDictionary = new Dictionary<string, OffsetEntry>();

        foreach (var line in inputLines)
        {
            var entry = OffsetEntry.Parse(line);
            if (entry is null)
            {
                continue;
            }
            inputDictionary.Add(entry.Name, entry);
        }

        var tempFile = Path.ChangeExtension(output, $".new{Path.GetExtension(output)}");
        using var tempFileStream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        var tempFileWriter = new StreamWriter(tempFileStream, new UTF8Encoding(false));
        foreach (var line in outputLines)
        {
            var oldEntry = OffsetEntry.Parse(line);
            if (oldEntry is null)
            {
                tempFileWriter.WriteLine(line);
                continue;
            }
            Console.WriteLine($"Processing '{oldEntry.Name}'...");
            ref var newEntry = ref CollectionsMarshal.GetValueRefOrNullRef(inputDictionary, oldEntry.Name);
            if (Unsafe.IsNullRef(ref newEntry))
            {
                Console.WriteLine($"Error at updating '{oldEntry.Name}'.");
                tempFileWriter.WriteLine($"// {oldEntry}");
                continue;
            }
            oldEntry.Offset = newEntry.Offset;
            tempFileWriter.WriteLine(oldEntry.ToString());
        }
        tempFileWriter.Flush();
    }

}
