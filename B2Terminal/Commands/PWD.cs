namespace B2Terminal.Commands;

public class PWD : ICommand
{
    private readonly IConsoleProvider ConsoleProvider;

    public string Command => "pwd";
    
    public PWD(
        IConsoleProvider consoleProvider
    )
    {
        ConsoleProvider = consoleProvider;
    }

    public Task Run(
        Client client,
        string arguments
    )
    {
        if (client.CurrentBucket is null)
        {
            ConsoleProvider.WriteLine("/");
        }
        else
        {
            var fullPath = string.Join('/', client.CurrentBucket.BucketName, client.CurrentPath);
            ConsoleProvider.WriteLine(fullPath);
        }
        return Task.CompletedTask;
    }
}