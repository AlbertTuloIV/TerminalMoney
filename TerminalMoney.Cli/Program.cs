using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TM.Cli.Setup;
using TM.Cli.Simulations;
using TM.Data.Persistence;

AnsiConsole.Write(
    new FigletText("Terminal Money")
        .Centered()
        .Color(Color.DeepSkyBlue1));

AnsiConsole.MarkupLine("[bold hotpink]Welcome to your terminal budgeting app![/]");

await using var dbContext = TMDbContextFactory.Create();
await dbContext.Database.MigrateAsync();

var setupWizard = new SetupWizard(dbContext);
var settingsMenu = new SettingsMenu(dbContext, setupWizard);
var simulationMenu = new SimulationMenu(dbContext);

if (!await setupWizard.HasCompletedSetupAsync())
{
    await setupWizard.RunInitialSetupAsync();
}

while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What do you want to do?")
            .AddChoices([
                "Dashboard",
                "Simulation",
                "Settings",
                "Exit"
            ]));

    switch (choice)
    {
        case "Dashboard":
            await settingsMenu.ShowCurrentSetupAsync();
            break;
        case "Simulation":
            await simulationMenu.ShowAsync();
            break;
        case "Settings":
            await settingsMenu.ShowAsync();
            break;
        case "Exit":
            AnsiConsole.MarkupLine("[green]Goodbye![/]");
            return;
    }
}