using ByteSizeLib;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace B2Terminal.Commands;

public class LS : ICommand
{
    private readonly IAPITasks APITasks;
    private readonly IConsoleProvider ConsoleProvider;

    public LS(IAPITasks apiTasks,
        IConsoleProvider consoleProvider
    )
    {
        APITasks = apiTasks;
        ConsoleProvider = consoleProvider;
    }

    public string Command => "ls";

    public async Task Run(
        Client client,
        string arguments
    )
    {
        // Root directory is a special case
        if (client.CurrentBucket is null)
        {
            await ListBucketsAsync();
            return;
        }
        await ListFilesAsync(client);
    }

    private async Task ListFilesAsync(
        Client client
    )
    {
        var pathWithSuffix = client.CurrentPath == "" ? "" : $"{client.CurrentPath}/";
        var allFiles = await APITasks.GetFilesAsync(client.CurrentBucket.BucketId, pathWithSuffix);
        var fileNames = allFiles
            .Where(f => f.Action == "upload");
        var grid = new Grid()
            .AddColumn()
            .AddColumn()
            .AddRow("Size", "Name");
        
        foreach (var file in fileNames)
        {
            grid.AddRow(
                new Text(ByteSize.FromBytes(long.Parse(file.ContentLength)).ToString()),
                new Text(Path.GetFileName(file.FileName))
            );
        }

        var folders = allFiles
            .Where(f => f.Action == "folder")
            .Select(f => f.FileName.Trim('/'))
            .Select(Path.GetFileName);

        foreach (var folder in folders)
        {
            grid.AddRow(
                "-",
                $"{folder}/"
            );
        }
        
        ConsoleProvider.WriteGrid(grid);
    }

    private async Task ListBucketsAsync()
    {
        var buckets = await APITasks.GetBucketsAsync();
        foreach (var bucket in buckets)
        {
            ConsoleProvider.WriteLine(bucket.BucketName);
        }
    }
}