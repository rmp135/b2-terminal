namespace B2Terminal.Commands;

public class CD : ICommand
{
    private readonly IAPITasks APITasks;
    private readonly IConsoleProvider ConsoleProvider;

    public CD(IAPITasks apiTasks,
        IConsoleProvider consoleProvider
    )
    {
        APITasks = apiTasks;
        ConsoleProvider = consoleProvider;
    }

    public string Command => "cd";

    public async Task Run(
        Client client,
        string arguments
    )
    {
        if (arguments == "..")
        {
            NavigateUpwards(client);
            return;
        }

        // At the root directory we have to look for a bucket
        if (client.CurrentBucket is null)
        {
            await NavigateIntoBucket(client, arguments);
            return;
        }

        await NavigateIntoDirectory(client, arguments);
    }

    private async Task NavigateIntoDirectory(
        Client client,
        string path
    )
    {
        var pathWithSuffix = client.CurrentPath == "" ? "" : $"{client.CurrentPath}/";
        var files = await APITasks.GetFilesAsync(client.CurrentBucket.BucketId, pathWithSuffix);
        var fullPath = $"{pathWithSuffix}{path}/";

        var foundDirectory = files.FirstOrDefault(x => x.FileName.Equals(fullPath, StringComparison.InvariantCultureIgnoreCase));
        if (foundDirectory is null)
        {
            ConsoleProvider.WriteLine($"Directory {path} does not exist");
            return;
        }

        client.CurrentPath = foundDirectory.FileName[..^1];
    }

    private async Task NavigateIntoBucket(
        Client client,
        string path
    )
    {
        var buckets = await APITasks.GetBucketsAsync();
        client.CurrentBucket = buckets.FirstOrDefault(x => x.BucketName.Equals(path));
        if (client.CurrentBucket is null)
        {
            ConsoleProvider.WriteLine($"Bucket {path} does not exist");
            return;
        }

        client.CurrentPath = "";
    }

    private void NavigateUpwards(
        Client client
    )
    {
        if (client.CurrentBucket is null)
        {
            return;
        }

        if (client.CurrentPath == "")
        {
            client.CurrentBucket = null;
        }
        else
        {
            client.CurrentPath = client.CurrentPath[..^1];
            var lastSlash = client.CurrentPath.LastIndexOf('/');
            client.CurrentPath = lastSlash == -1 ? "" : client.CurrentPath[0..lastSlash];
        }
    }
}