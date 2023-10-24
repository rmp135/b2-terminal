using System.Security.Cryptography;

namespace B2Terminal.Commands;

public class PUT : ICommand
{
    private readonly IConsoleProvider ConsoleProvider;
    private readonly IAPITasks APITasks;
    
    public string Command => "put";
    
    public PUT(
        IConsoleProvider consoleProvider,
        IAPITasks apiTasks
    )
    {
        ConsoleProvider = consoleProvider;
        APITasks = apiTasks;
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

        var fileInfo = new FileInfo(arguments);
        if (!fileInfo.Exists)
        {
            ConsoleProvider.WriteLine($"File {arguments} not found");
            return;
        }
        
        var tokenSource = new CancellationTokenSource();
        var cancellationToken = tokenSource.Token;
        
        await using var fileStream = fileInfo.OpenRead();
        
        // Would be more efficient to hash the file as we upload it, but this is easier for now.
        using var sha1 = SHA1.Create();
        var hashBytes = await sha1.ComputeHashAsync(fileStream, cancellationToken);
        var sha1Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        // Reset the stream to the beginning so we can upload it.
        fileStream.Seek(0, SeekOrigin.Begin);
        
        // Cancel the download if the user presses Ctrl+C
        Console.CancelKeyPress += OnConsoleOnCancelKeyPress;

        var uploadPath = client.CurrentPath == "" ? fileInfo.Name : $"{client.CurrentPath}/{fileInfo.Name}";
        try
        {
            // Do not await so we can track progress.
            _ = APITasks.UploadFile(
                fileStream,
                cancellationToken,
                client.CurrentBucket.BucketId,
                uploadPath,
                sha1Hash
            );

            await ConsoleProvider.TransferProgress(arguments, fileStream.Length, fileStream, CancellationToken.None);
            ConsoleProvider.WriteLine(
                tokenSource.IsCancellationRequested ? "[red]Upload cancelled.[/]" : "[green]Upload complete[/]"
            );
        }
        finally
        {
            Console.CancelKeyPress -= OnConsoleOnCancelKeyPress;
        }

        return;
        
        void OnConsoleOnCancelKeyPress(
            object? _,
            ConsoleCancelEventArgs args
        )
        {
            args.Cancel = true;
            tokenSource.Cancel();
        }
    }
}