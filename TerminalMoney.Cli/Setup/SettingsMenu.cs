using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TM.Core.Models;
using TM.Data.Persistence;

namespace TM.Cli.Setup;

public class SettingsMenu(TMDbContext dbContext, SetupWizard setupWizard)
{
    public async Task ShowAsync()
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Settings")
                    .AddChoices([
                        "View current setup",
                        "Rerun setup",
                        "Back"
                    ])
            );

            switch (choice)
            {
                case "View current setup":
                    await ShowCurrentSetupAsync();
                    break;
                
                case "Rerun setup":
                    if(AnsiConsole.Confirm("This will replce your current setup profile. Continue?", false))
                    {
                        await setupWizard.RerunSetupAsync();
                    }
                    break;
                
                case "Back":
                    return;
            }
        }
    }

    public async Task ShowCurrentSetupAsync()
    {
        var profile = await dbContext.UserProfiles
            .Include(x => x.RegularIncomeSources)
            .Include(x => x.FinancialAccounts)
            .Include(x => x.CreditCards)
            .Include(x => x.DebtAccounts)
            .Include(x => x.PaycheckSamples)
            .FirstOrDefaultAsync();

        if(profile is null)
        {
            AnsiConsole.MarkupLine("[yellow]Setup has not been completed yet.[/]");
            return;
        }

        var table = new Table()
            .Title("Current setup")
            .AddColumn("Field")
            .AddColumn("Value");

        table.AddRow("Name", profile.Name);
        table.AddRow("Age", profile.Age.ToString());
        table.AddRow("Primary job", profile.PrimaryJob);
        table.AddRow("Pay Frequency", profile.PayFrequency.ToString());
        table.AddRow("Employment type", profile.EmploymentType.ToString());
        table.AddRow("Can earn Overtime", profile.CanEarnOvertime ? "Yes" : "No");
        table.AddRow("Take-home estimate", profile.TakeHomePayPerPayPeriod?.ToString("C") ?? "Manual entry each pay period.");
        table.AddRow("Primary goal", profile.PrimaryGoal.ToString());
        table.AddRow("Regular income sources", profile.RegularIncomeSources.Count.ToString());
        table.AddRow("Credit Cards", profile.CreditCards.Count.ToString());
        table.AddRow("Debt accounts", profile.DebtAccounts.Count.ToString());

        AnsiConsole.Write(table);
        WriteFinancialAccounts(profile);
        WriteRegularIncome(profile);
        WriteCreditCards(profile);
        WriteDebtAccounts(profile);
    }

    private static void WriteFinancialAccounts(UserProfile profile)
    {
        if(profile.FinancialAccounts.Count == 0)
        {
            return;
        }

        var table = new Table()
            .Title("Financical Accounts")
            .AddColumn("Name")
            .AddColumn("Type")
            .AddColumn("Balance");
        
        foreach(var account in profile.FinancialAccounts)
        {
            table.AddRow(account.Name, account.AccountType.ToString(), account.CurrentBalance.ToString("C"));
        }

        AnsiConsole.Write(table);
    }

    private static void WriteRegularIncome(UserProfile profile)
    {
        if(profile.RegularIncomeSources.Count == 0)
        {
            return;   
        }

        var table = new Table()
            .Title("Regular Income")
            .AddColumn("Name")
            .AddColumn("Amount")
            .AddColumn("Frequency");
        
        foreach(var income in profile.RegularIncomeSources)
        {
            table.AddRow(income.Name, income.Amount.ToString("C"), income.Frequency.ToString());
        }

        AnsiConsole.Write(table);
    }

    private static void WriteCreditCards(UserProfile profile)
    {
        if(profile.CreditCards.Count == 0)
        {
            return;
        }

        var table = new Table()
            .Title("Credit Cards")
            .AddColumn("Name")
            .AddColumn("Balance")
            .AddColumn("Limit")
            .AddColumn("Min payment")
            .AddColumn("APR");

        foreach(var card in profile.CreditCards)
        {
            table.AddRow(
                card.Name,
                card.CurrentBalance.ToString("C"),
                card.CreditLimit.ToString("C"),
                card.MinimumPayment.ToString("C"),
                $"{card.InterestRateApr:N2}%");
        }

        AnsiConsole.Write(table);
    }

    private static void WriteDebtAccounts(UserProfile profile)
    {
        if(profile.DebtAccounts.Count == 0)
        {
            return;
        }

        var table = new Table()
            .Title("Debt Accounts")
            .AddColumn("Name")
            .AddColumn("Type")
            .AddColumn("Balance")
            .AddColumn("Payment")
            .AddColumn("APR");

        foreach(var debt in profile.DebtAccounts)
        {
            table.AddRow(
                debt.Name,
                debt.DebtType.ToString(),
                debt.CurrentBalance.ToString("C"),
                debt.MonthlyPayment.ToString("C"),
                $"{debt.InterestRateApr:N2}%");
        }

        AnsiConsole.Write(table);
    }
}