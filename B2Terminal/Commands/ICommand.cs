namespace B2Terminal.Commands;

public interface ICommand
{
    string Command { get; }

    Task Run (
        Client client,
        string arguments
    );
}