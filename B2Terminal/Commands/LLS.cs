namespace B2Terminal.Commands;

public class LLS : ICommand
{
    private readonly IConsoleProvider ConsoleProvider;
    
    public string Command => "lls";

    public LLS(
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
        var files = Directory.GetFiles(client.LocalPath)
            .Select(Path.GetFileName);
        var directories = Directory.GetDirectories(client.LocalPath)
            .Select(s => $"{Path.GetFileName(s)}\\");
        foreach (var file in files)
        {
            ConsoleProvider.WriteLine(file);
        }
        foreach (var directory in directories)
        {
            ConsoleProvider.WriteLine(directory);
        }
        return Task.CompletedTask;
    }
}