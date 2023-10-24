namespace B2Terminal.Commands;

public class GET : ICommand
{
    public string Command => "get";

    private readonly IAPITasks APITasks;
    private readonly IConsoleProvider ConsoleProvider;

    public GET(
        IAPITasks apiTasks,
        IConsoleProvider consoleProvider
    )
    {
        APITasks = apiTasks;
        ConsoleProvider = consoleProvider;
    }

    public async Task Run(
        Client client,
        string arguments
    )
    {
        if (client.CurrentBucket is null)
        {
            ConsoleProvider.WriteLine("You must be in a bucket to download files");
            return;
        }

        var pathWithSuffix = client.CurrentPath == "" ? "" : $"{client.CurrentPath}/";
        var files = await APITasks.GetFilesAsync(
            client.CurrentBucket.BucketId,
            pathWithSuffix
        );
        
        var filePath = $"{pathWithSuffix}{arguments}";

        var file = files.FirstOrDefault(
            x => x.Action == "upload" && x.FileName.Equals(filePath, StringComparison.InvariantCultureIgnoreCase)
        );

        if (file is null)
        {
            ConsoleProvider.WriteLine($"File {arguments} not found");
            return;
        }

        var downloadTokenSource = new CancellationTokenSource();
        var token = downloadTokenSource.Token;
        
        var localPath = Path.Join(client.LocalPath, Path.GetFileName(file.FileName));
        
        var beginAt = 0L;
        
        if (File.Exists(localPath))
        {
            if (ConsoleProvider.Confirm("File already exists. Do you want to resume?"))
            {
                var fileSize = new FileInfo(localPath);
                beginAt = fileSize.Length;
            }
        }

        var response = await APITasks.DownloadFile(
            file.FileId,
            token,
            beginAt,
            endAt: long.Parse(file.ContentLength)
        );
        
        if (!response.IsSuccessStatusCode)
        {
            ConsoleProvider.WriteLine($"Error downloading file: {response.ReasonPhrase}");
            return;
        }

        var fileName = Path.GetFileName(file.FileName);

        await using var stream = await response.Content.ReadAsStreamAsync(token);

        await using var fileStream = new FileStream(
            Path.Join(client.LocalPath, fileName),
            FileMode.Append,
            FileAccess.Write,
            FileShare.None,
            4096,
            true
        );

        // Stream is not awaited to allow progress to be monitored
        _ = stream.CopyToAsync(fileStream, token);

        // Cancel the download if the user presses Ctrl+C
        Console.CancelKeyPress += OnConsoleOnCancelKeyPress;

        try
        {
            await ConsoleProvider.TransferProgress(
                fileName,
                long.Parse(file.ContentLength),
                fileStream,
                token
            );

            ConsoleProvider.WriteLine(
                downloadTokenSource.IsCancellationRequested ? "[red]Download cancelled.[/]" : "[green]Download complete[/]"
            );
        }
        finally
        {
            // Remove the event handler when the download is complete
            Console.CancelKeyPress -= OnConsoleOnCancelKeyPress;
        }
        return;

        void OnConsoleOnCancelKeyPress(
            object? _,
            ConsoleCancelEventArgs args
        )
        {
            args.Cancel = true;
            downloadTokenSource.Cancel();
        }
    }
}