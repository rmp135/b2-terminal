using CommandLine;

namespace B2Terminal;

public class CommandArguments
{
    [Option("account", Required = true, HelpText = "The account ID to use.")]
    public string AccountID { get; set; } = "";
    
    [Option("key", Required = true, HelpText = "The application key to use.")]
    public string ApplicationKey { get; set; } = "";
}