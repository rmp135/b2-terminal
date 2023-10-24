using B2Terminal;
using B2Terminal.Commands;
using Microsoft.Extensions.DependencyInjection;

await new ServiceCollection()
    .AddHttpClient()
    .AddSingleton<IAPITasks, APITasks>()
    .AddSingleton<IConsoleProvider, ConsoleProvider>()
    .AddSingleton<ICommand, CD>()
    .AddSingleton<ICommand, LS>()
    .AddSingleton<ICommand, LLS>()
    .AddSingleton<ICommand, LPWD>()
    .AddSingleton<ICommand, PWD>()
    .AddSingleton<ICommand, GET>()
    .AddSingleton<ICommand, PUT>()
    .AddSingleton<Client>()
    .BuildServiceProvider()
    .GetRequiredService<Client>()
    .RunAsync(args);