using Spectre.Console;

AnsiConsole.Write(
    new FigletText("TerminalMoney")
    .Centered()
    .Color(Color.DeepSkyBlue1)
);

AnsiConsole.MarkupLine("[bold hotpink]Welcome to your terminal budgetting app![/]");

var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
    .Title("What do you want to do?")
    .AddChoices([
        "Dashboard",
        "Add transaction",
        "View Transactions",
        "Exit"
    ])
);

AnsiConsole.MarkupLine($"You selected: [green]{choice}[/]");