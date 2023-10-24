namespace B2Terminal.Commands;

public class LPWD : ICommand
{
    private readonly IConsoleProvider ConsoleProvider;

    public string Command => "lpwd";
    
    public LPWD(
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
        ConsoleProvider.WriteLine(client.LocalPath);
        return Task.CompletedTask;
    }
}