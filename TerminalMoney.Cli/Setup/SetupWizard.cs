using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TM.Core.Enums;
using TM.Core.Models;
using TM.Data.Persistence;
using TM.Cli.Components;

namespace TM.Cli.Setup;

public class SetupWizard(TMDbContext dbContext)
{
    public Task<bool> HasCompletedSetupAsync()
    {
        return dbContext.UserProfiles.AnyAsync();
    }

    public async Task RunInitialSetupAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold deepskyblue1]Let's set up TerminalMoney.[/]");
        AnsiConsole.MarkupLine("Everything you enter is saved locally on this computer.");
        AnsiConsole.WriteLine();

        var profile = new UserProfile
        {
            Name = Prompts.PromptRequiredText("What is your name?"),
            Age = Prompts.PromptInt("What is your age?", 1, 130),
            PrimaryJob = Prompts.PromptRequiredText("What is your primary job?"),
            PayFrequency = Prompts.PromptPayFrequency()
        };

        ConfigurePay(profile);
        ConfigureRegularIncome(profile);
        ConfigureFinancialAccounts(profile);
        ConfigureCreditCards(profile);
        ConfigureDebtAccounts(profile);

        profile.PrimaryGoal = Prompts.PromptGoal();

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        AnsiConsole.MarkupLine("[green]Setup complete![/]");
        AnsiConsole.WriteLine();
    }

    public async Task RerunSetupAsync()
    {
        var existingProfiles = await dbContext.UserProfiles.ToListAsync();
        dbContext.UserProfiles.RemoveRange(existingProfiles);
        await dbContext.SaveChangesAsync();

        await RunInitialSetupAsync();
    }

    private static void ConfigurePay(UserProfile profile)
    {
        profile.EmploymentType = Prompts.PromptEmploymentType();
        profile.CanEarnOvertime = AnsiConsole.Confirm("Are you able to earn overtime pay?", false);

        if(profile.EmploymentType == EmploymentType.Salary)
        {
            profile.TakeHomePayPerPayPeriod = Prompts.PromptMoney("What is your take-home pay every pay period?");
            return;
        }

        profile.HourlyPayEstimationMode = Prompts.PromptHourlyPayEstimationMode();

        if(profile.HourlyPayEstimationMode == HourlyPayEstimationMode.EnterExpectedTakeHomeNow)
        {
            profile.TakeHomePayPerPayPeriod = Prompts.PromptMoney("What is your expected take-home pay for a normal pay period?");
            return;
        }

        if(profile.HourlyPayEstimationMode == HourlyPayEstimationMode.EstimateFromPreviousPaychecks)
        {
            do
            {
                profile.PaycheckSamples.Add(new PaycheckSample
                {
                    PayDate = Prompts.PromptDate("What was the pay date?"),
                    TakeHomePay = Prompts.PromptMoney("What was the take-home pay on that check?")
                });
            }
            while(AnsiConsole.Confirm("Add another previous paycheck?", true));

            profile.TakeHomePayPerPayPeriod = profile.PaycheckSamples.Average(x => x.TakeHomePay);
            AnsiConsole.MarkupLine($"Estimated take-home pay per pay period: [green]{profile.TakeHomePayPerPayPeriod:C}[/]");
        }
    }

    private static void ConfigureRegularIncome(UserProfile profile)
    {
        while(AnsiConsole.Confirm("Do you have an additional regular source of income to add?", false))
        {
            profile.RegularIncomeSources.Add(new RegularIncomeSource
            {
                Name = Prompts.PromptRequiredText("Income source name:"),
                Amount = Prompts.PromptMoney("Amount:"),
                Frequency = Prompts.PromptRegularIncomeFrequency()
            });
        }
    }

    private static void ConfigureFinancialAccounts(UserProfile profile)
    {
        while(AnsiConsole.Confirm("Do you want to add a bank, savings, brokerage, retirement, cash or other ccount?", true))
        {
            profile.FinancialAccounts.Add(new FinancialAccount
            {
                Name = Prompts.PromptRequiredText("Account name:"),
                AccountType = Prompts.PromptAccountType(),
                CurrentBalance = Prompts.PromptMoney("Current balance:")
            });
        }
    }

    private static void ConfigureCreditCards(UserProfile profile)
    {
        while(AnsiConsole.Confirm("Do you want to add a credit card?", false))
        {
            profile.CreditCards.Add(new CreditCard
            {
                Name = Prompts.PromptRequiredText("Credit card name:"),
                CurrentBalance = Prompts.PromptMoney("Current balance:"),
                CreditLimit = Prompts.PromptMoney("Credit limit:"),
                MinimumPayment = Prompts.PromptMoney("Minimum payment:"),
                InterestRateApr = Prompts.PromptPercent("Interest rate APR Percentage:") 
            });
        }
    }

    private static void ConfigureDebtAccounts(UserProfile profile)
    {
        while(AnsiConsole.Confirm("Do you want to add a loan, mortgage, or other debt?", false))
        {
            profile.DebtAccounts.Add(new DebtAccount
            {
                Name = Prompts.PromptRequiredText("Debt name:"),
                DebtType = Prompts.PromptDebtType(),
                CurrentBalance = Prompts.PromptMoney("Current balance:"),
                MonthlyPayment = Prompts.PromptMoney("Monthly payment:"),
                InterestRateApr = Prompts.PromptPercent("Interest rate APR percentage:")
            });
        }
    }
}