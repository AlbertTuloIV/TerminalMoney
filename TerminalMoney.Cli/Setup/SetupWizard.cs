using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TM.Core.Enums;
using TM.Core.Models;
using TM.Data.Persistence;

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
            Name = PromptRequiredText("What is your name?"),
            Age = PromptInt("What is your age?", 1, 130),
            PrimaryJob = PromptRequiredText("What is your primary job?"),
            PayFrequency = PromptPayFrequency()
        };

        ConfigurePay(profile);
        ConfigureRegularIncome(profile);
        ConfigureFinancialAccounts(profile);
        ConfigureCreditCards(profile);
        ConfigureDebtAccounts(profile);

        profile.PrimaryGoal = PromptGoal();

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
        profile.EmploymentType = PromptEmploymentType();
        profile.CanEarnOvertime = AnsiConsole.Confirm("Are you able to earn overtime pay?", false);

        if(profile.EmploymentType == EmploymentType.Salary)
        {
            profile.TakeHomePayPerPayPeriod = PromptMoney("What is your take-home pay every pay period?");
            return;
        }

        profile.HourlyPayEstimationMode = PromptHourlyPayEstimationMode();

        if(profile.HourlyPayEstimationMode == HourlyPayEstimationMode.EnterExpectedTakeHomeNow)
        {
            profile.TakeHomePayPerPayPeriod = PromptMoney("What is your expected take-home pay for a normal pay period?");
            return;
        }

        if(profile.HourlyPayEstimationMode == HourlyPayEstimationMode.EstimateFromPreviousPaychecks)
        {
            do
            {
                profile.PaycheckSamples.Add(new PaycheckSample
                {
                    PayDate = PromptDate("What was the pay date?"),
                    TakeHomePay = PromptMoney("What was the take-home pay on that check?")
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
                Name = PromptRequiredText("Income source name:"),
                Amount = PromptMoney("Amount:"),
                Frequency = PromptRegularIncomeFrequency()
            });
        }
    }

    private static void ConfigureFinancialAccounts(UserProfile profile)
    {
        while(AnsiConsole.Confirm("Do you want to add a bank, savings, brokerage, retirement, cash or other ccount?", true))
        {
            profile.FinancialAccounts.Add(new FinancialAccount
            {
                Name = PromptRequiredText("Account name:"),
                AccountType = PromptAccountType(),
                CurrentBalance = PromptMoney("Current balance:")
            });
        }
    }

    private static void ConfigureCreditCards(UserProfile profile)
    {
        while(AnsiConsole.Confirm("Do you want to add a credit card?", false))
        {
            profile.CreditCards.Add(new CreditCard
            {
                Name = PromptRequiredText("Credit card name:"),
                CurrentBalance = PromptMoney("Current balance:"),
                CreditLimit = PromptMoney("Credit limit:"),
                MinimumPayment = PromptMoney("Minimum payment:"),
                InterestRateApr = PromptPercent("Interest rate APR Percentage:") 
            });
        }
    }

    private static void ConfigureDebtAccounts(UserProfile profile)
    {
        while(AnsiConsole.Confirm("Do you want to add a loan, mortgage, or other debt?", false))
        {
            profile.DebtAccounts.Add(new DebtAccount
            {
                Name = PromptRequiredText("Debt name:"),
                DebtType = PromptDebtType(),
                CurrentBalance = PromptMoney("Current balance:"),
                MonthlyPayment = PromptMoney("Monthly payment:"),
                InterestRateApr = PromptPercent("Interest rate APR percentage:")
            });
        }
    }

    private static string PromptRequiredText(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .Validate(value => string.IsNullOrWhiteSpace(value)
                    ? ValidationResult.Error("[red]Please enter a value.[/]")
                    : ValidationResult.Success()));
    }

    private static int PromptInt(string prompt, int minimum, int maximum)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<int>(prompt)
                .Validate(value => value < minimum || value > maximum
                    ? ValidationResult.Error($"[red]Enter a value from {minimum} to {maximum}.[/]")
                    : ValidationResult.Success()));        
    }

    private static decimal PromptMoney(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<decimal>(prompt)
                .Validate(value => value < 0 
                    ? ValidationResult.Error("[red]Enter zero or a positive number.[/]")
                    : ValidationResult.Success()));
    }

    private static decimal PromptPercent(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<decimal>(prompt)
                .Validate(value => value < 0 || value > 100
                    ? ValidationResult.Error("[red]Enter a percentage from 0 to 100.[/]")
                    : ValidationResult.Success()));
    }

    private static DateTime PromptDate(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<DateTime>(prompt)
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Enter a valid date such as 2026-05-27.[/]"));
    }

    private static PayFrequency PromptPayFrequency()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How often are you paid?")
                .AddChoices([
                    "Weekly",
                    "Bi-weekly",
                    "Semi-monthly",
                    "Monthly"
                ]));
        return choice switch
        {
            "Weekly" => PayFrequency.Weekly,
            "Bi-weekly" => PayFrequency.BiWeekly,
            "Semi-monthly" => PayFrequency.SemiMonthly,
            _ => PayFrequency.Monthly
        };
    }

    private static EmploymentType PromptEmploymentType()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Are you salary or hourly?")
                .AddChoices(["Salary", "Hourly"]));
            
        return choice == "Salary" ? EmploymentType.Salary : EmploymentType.Hourly;
    }

    private static HourlyPayEstimationMode PromptHourlyPayEstimationMode()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How should TerminalMoney estimate your hourly take-home pay?")
                .AddChoices([
                    "Enter expected take-home pay now",
                    "Estimate from previous paychecks",
                    "Enter pay manually each pay period"
                ]));
        return choice switch
        {
            "Enter expected take-home pay now" => HourlyPayEstimationMode.EnterExpectedTakeHomeNow,
            "Estimate from previous paychecks" => HourlyPayEstimationMode.EstimateFromPreviousPaychecks,
            _ => HourlyPayEstimationMode.EnterManuallyEachPayPeriod
        };
    }

    private static RegularIncomeFrequency PromptRegularIncomeFrequency()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How often do you recieve this income?")
                .AddChoices([
                    "Weekly",
                    "Bi-weekly",
                    "Semi-monthly",
                    "Monthly",
                    "Quarterly",
                    "Yearly"
                ]));
        
        return choice switch
        {
            "Weekly" => RegularIncomeFrequency.Weekly,
            "Bi-weekly" => RegularIncomeFrequency.BiWeekly,
            "Semi-monthly" => RegularIncomeFrequency.SemiMonthly,
            "Monthly" => RegularIncomeFrequency.Monthly,
            "Quarterly" => RegularIncomeFrequency.Quarterly,
            _ => RegularIncomeFrequency.Yearly
        };
    }

    private static AccountType PromptAccountType()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What type of account is this?")
                .AddChoices([
                    "Checking",
                    "Savings",
                    "Brokerage",
                    "Retirement",
                    "Cash",
                    "Other"
                ]));
        return Enum.Parse<AccountType>(choice);
    }

    private static DebtType PromptDebtType()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What type of debt is this?")
                .AddChoices([
                    "Personal loan",
                    "Auto loan",
                    "Student loan",
                    "Mortage",
                    "Medical debt",
                    "Other"
                ]));
        return choice switch
        {
            "Personal loan" => DebtType.PersonalLoan,
            "Auto loan" => DebtType.AutoLoan,
            "Student loan" => DebtType.StudentLoan,
            "Mortage" => DebtType.Mortage,
            "Medical debt" => DebtType.MedicalDept,
            _ => DebtType.Other
        };
    }

    private static GoalType PromptGoal()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What is your primary goal?")
                .AddChoices([
                    "Save money",
                    "Pay off debt",
                    "Track my information"
                ]));

        return choice switch
        {
            "Save money" => GoalType.SaveMoney,
            "Pay off debt" => GoalType.PayOffDebt,
            _ => GoalType.TrackInformation
        };
    }
}