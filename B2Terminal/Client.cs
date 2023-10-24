using B2Net.Models;
using B2Terminal.Commands;
using CommandLine;

namespace B2Terminal;

public class Client
{
    /// <summary>
    /// The current path relative to the current bucket.
    /// Does not contain trailing or leading slashes.
    /// </summary>
    public string CurrentPath { get; set; } = "";
    public B2Bucket? CurrentBucket { get; set; }
    
    public string LocalPath { get; private set; } = "";

    private readonly IEnumerable<ICommand> Commands;
    private readonly IConsoleProvider ConsoleProvider;
    private readonly IAPITasks APITasks;

    public Client(
        IEnumerable<ICommand> commands,
        IConsoleProvider consoleProvider,
        IAPITasks apiTasks
    )
    {
        Commands = commands;
        ConsoleProvider = consoleProvider;
        APITasks = apiTasks;
    }

    public async Task RunAsync(IEnumerable<string> args)
    {
        var parsedArguments = Parser.Default.ParseArguments<CommandArguments>(args);
        if (parsedArguments is not Parsed<CommandArguments>)
        {
            return;
        }
        LocalPath = Environment.CurrentDirectory;

        await Login(parsedArguments.Value.AccountID, parsedArguments.Value.ApplicationKey);
        
        while (true)
        {
            ConsoleProvider.Write($"{CurrentPathAsString}> ");
            var input = Console.ReadLine() ?? "";
            await ParseInput(input);
        }
    }

    private async Task Login(string accountID, string applicationKey)
    {
        while (true)
        {
            try
            {
                Console.WriteLine("Authorising...");
                await APITasks.Authorise(accountID, applicationKey);
                Console.WriteLine("Authorisation successful");
                break;
            }
            catch (AuthorizationException e)
            {
                ConsoleProvider.WriteLine("Authorisation failed.");
                ConsoleProvider.WriteLine(e.Message);
                ConsoleProvider.WriteLine("Please re-enter your credentials.");
                accountID = ConsoleProvider.Ask("Enter your account ID:");
                applicationKey = ConsoleProvider.Ask("Enter your application key:");
            }
        }
    }

    private async Task ParseInput(
        string input
    )
    {
        var split = input.Split(' ');
        
        var toRun = Commands.FirstOrDefault(x => x.Command == split.First());
        if (toRun is null)
        {
            ConsoleProvider.WriteLine($"Command {input} not found");
            return;
        }
        await toRun.Run(this, string.Join(' ', split.Skip(1)).Trim());
    }

    private string CurrentPathAsString
    {
        get
        {
            var path = "";
            if (CurrentBucket is not null)
            {
                path = CurrentBucket.BucketName;
            }

            if (!string.IsNullOrEmpty(CurrentPath))
            {
                path = $"{path}/{CurrentPath}";
            }

            return path;
        }
    }
}