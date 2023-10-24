using Spectre.Console;
using Spectre.Console.Rendering;

namespace B2Terminal;

public interface IConsoleProvider
{
    void WriteLine(string line);

    void Write(string s);

    void WriteGrid(
        Grid grid
    );

    /// <summary>
    /// Creates an <see cref="AnsiConsole.Progress"/> bar for the given stream.
    /// </summary>
    /// <param name="fileName">The filename to show in the progress.</param>
    /// <param name="maxSize">The max size of the file.</param>
    /// <param name="fileStream">The stream to monitor progress for.</param>
    /// <param name="token">Cancellation token.</param>
    Task TransferProgress(
        string fileName,
        long maxSize,
        Stream fileStream,
        CancellationToken token
    );

    bool Confirm(
        string text
    );

    string Ask(
        string text
    );
}

public class ConsoleProvider : IConsoleProvider
{
    public void WriteLine(string line)
    {
        AnsiConsole.MarkupLine(line);
    }

    public void Write(
        string s
    )
    {
        AnsiConsole.Markup(s);
    }

    public void WriteGrid(
        Grid grid
    )
    {
        AnsiConsole.Write(grid);
    }

    public async Task TransferProgress(
        string fileName,
        long maxSize,
        Stream fileStream,
        CancellationToken token
    )
    {
        await AnsiConsole
            .Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn(),
                new TransferSpeedColumn()
            )
            .StartAsync(
                async args =>
                {
                    var t = args.AddTask(fileName, false, maxSize);
                    t.Value = fileStream.Position;
                    t.StartTask();
                    while (!(t.IsFinished || token.IsCancellationRequested))
                    {
                        try
                        {
                            await Task.Delay(200, token);
                            t.Value = fileStream.Position;
                        }
                        catch (TaskCanceledException)
                        {
                        }
                    }
                }
            );
    }
    
    public string Ask(
        string text
    )
    {
        return AnsiConsole.Ask<string>(text);
    }
    
    public bool Confirm(
        string text
    )
    {
        return AnsiConsole.Confirm(text);
    }
}