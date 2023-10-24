using Spectre.Console;
using Spectre.Console.Rendering;

namespace B2Terminal.Tests;

// The classes found herein are taken verbatim from the Spectre.Console source code.

/// <summary>
/// A testable console.
/// </summary>
public sealed class TestConsole : IAnsiConsole, IDisposable
{
    private readonly IAnsiConsole _console;
    private readonly StringWriter _writer;
    private IAnsiConsoleCursor? _cursor;

    /// <inheritdoc/>
    public Profile Profile => _console.Profile;

    /// <inheritdoc/>
    public IExclusivityMode ExclusivityMode => _console.ExclusivityMode;

    /// <summary>
    /// Gets the console input.
    /// </summary>
    public TestConsoleInput Input { get; }

    /// <inheritdoc/>
    public RenderPipeline Pipeline => _console.Pipeline;

    /// <inheritdoc/>
    public IAnsiConsoleCursor Cursor => _cursor ?? _console.Cursor;

    /// <inheritdoc/>
    IAnsiConsoleInput IAnsiConsole.Input => Input;

    /// <summary>
    /// Gets the console output.
    /// </summary>
    public string Output => _writer.ToString();

    /// <summary>
    /// Gets the console output lines.
    /// </summary>
    public IReadOnlyList<string> Lines => Output.NormalizeLineEndings().TrimEnd('\n').Split(new char[] { '\n' });

    /// <summary>
    /// Gets or sets a value indicating whether or not VT/ANSI sequences
    /// should be emitted to the console.
    /// </summary>
    public bool EmitAnsiSequences { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestConsole"/> class.
    /// </summary>
    public TestConsole()
    {
        _writer = new StringWriter();
        _cursor = new NoopCursor();

        Input = new TestConsoleInput();
        EmitAnsiSequences = false;

        _console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = (ColorSystemSupport)ColorSystem.TrueColor,
            Out = new AnsiConsoleOutput(_writer),
            Interactive = InteractionSupport.No,
            ExclusivityMode = new NoopExclusivityMode(),
            Enrichment = new ProfileEnrichment
            {
                UseDefaultEnrichers = false,
            },
        });

        _console.Profile.Width = 80;
        _console.Profile.Height = 24;
        _console.Profile.Capabilities.Ansi = true;
        _console.Profile.Capabilities.Unicode = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _writer.Dispose();
    }

    /// <inheritdoc/>
    public void Clear(bool home)
    {
        _console.Clear(home);
    }

    /// <inheritdoc/>
    public void Write(IRenderable renderable)
    {
        if (EmitAnsiSequences)
        {
            _console.Write(renderable);
        }
        else
        {
            foreach (var segment in renderable.GetSegments(this))
            {
                if (segment.IsControlCode)
                {
                    continue;
                }

                Profile.Out.Writer.Write(segment.Text);
            }
        }
    }

    internal void SetCursor(IAnsiConsoleCursor? cursor)
    {
        _cursor = cursor;
    }
}

/// <summary>
/// Represents a testable console input mechanism.
/// </summary>
public sealed class TestConsoleInput : IAnsiConsoleInput
{
    private readonly Queue<ConsoleKeyInfo> _input;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestConsoleInput"/> class.
    /// </summary>
    public TestConsoleInput()
    {
        _input = new Queue<ConsoleKeyInfo>();
    }

    /// <summary>
    /// Pushes the specified text to the input queue.
    /// </summary>
    /// <param name="input">The input string.</param>
    public void PushText(string input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        foreach (var character in input)
        {
            PushCharacter(character);
        }
    }

    /// <summary>
    /// Pushes the specified text followed by 'Enter' to the input queue.
    /// </summary>
    /// <param name="input">The input.</param>
    public void PushTextWithEnter(string input)
    {
        PushText(input);
        PushKey(ConsoleKey.Enter);
    }

    /// <summary>
    /// Pushes the specified character to the input queue.
    /// </summary>
    /// <param name="input">The input.</param>
    public void PushCharacter(char input)
    {
        var control = char.IsUpper(input);
        _input.Enqueue(new ConsoleKeyInfo(input, (ConsoleKey)input, false, false, control));
    }

    /// <summary>
    /// Pushes the specified key to the input queue.
    /// </summary>
    /// <param name="input">The input.</param>
    public void PushKey(ConsoleKey input)
    {
        _input.Enqueue(new ConsoleKeyInfo((char)input, input, false, false, false));
    }

    /// <summary>
    /// Pushes the specified key to the input queue.
    /// </summary>
    /// <param name="consoleKeyInfo">The input.</param>
    public void PushKey(ConsoleKeyInfo consoleKeyInfo)
    {
        _input.Enqueue(consoleKeyInfo);
    }

    /// <inheritdoc/>
    public bool IsKeyAvailable()
    {
        return _input.Count > 0;
    }

    /// <inheritdoc/>
    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        if (_input.Count == 0)
        {
            throw new InvalidOperationException("No input available.");
        }

        return _input.Dequeue();
    }

    /// <inheritdoc/>
    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        return Task.FromResult(ReadKey(intercept));
    }
}

internal sealed class NoopExclusivityMode : IExclusivityMode
{
    public T Run<T>(Func<T> func)
    {
        return func();
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> func)
    {
        return await func().ConfigureAwait(false);
    }
}

internal sealed class NoopCursor : IAnsiConsoleCursor
{
    public void Move(CursorDirection direction, int steps)
    {
    }

    public void SetPosition(int column, int line)
    {
    }

    public void Show(bool show)
    {
    }
}

public static class StringExtensions
{
    /// <summary>
    /// Returns a new string with all lines trimmed of trailing whitespace.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>A new string with all lines trimmed of trailing whitespace.</returns>
    public static string TrimLines(this string value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var result = new List<string>();
        foreach (var line in value.NormalizeLineEndings().Split(new[] { '\n' }))
        {
            result.Add(line.TrimEnd());
        }

        return string.Join("\n", result);
    }

    /// <summary>
    /// Returns a new string with normalized line endings.
    /// </summary>
    /// <param name="value">The string to normalize line endings for.</param>
    /// <returns>A new string with normalized line endings.</returns>
    public static string NormalizeLineEndings(this string value)
    {
        if (value != null)
        {
            value = value.Replace("\r\n", "\n");
            return value.Replace("\r", string.Empty);
        }

        return string.Empty;
    }
}