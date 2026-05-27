using Spectre.Console;
using TM.Core.Enums;
using TM.Core.Models;
using TM.Core.Simulations;

namespace TM.Cli.Components;

public class Prompts
{
    public static string PromptRequiredText(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
            .Validate(value => string.IsNullOrWhiteSpace(value)
                ? ValidationResult.Error("[red]Please enter a value.[/]")
                : ValidationResult.Success()));
    }

    public static int PromptInt(string prompt, int minimum, int maximum)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<int>(prompt)
                .Validate(value => value < minimum || value > maximum
                    ? ValidationResult.Error($"[red]Enter a value from {minimum} to {maximum}.[/]")
                    : ValidationResult.Success()));
    }

    public static decimal PromptMoney(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<decimal>(prompt)
                .Validate(value => value < 0
                    ? ValidationResult.Error("[red]Enter zero or a positive number.[/]")
                    : ValidationResult.Success()));
    }

    public static decimal PromptPercent(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<decimal>(prompt)
                .Validate(value => value < 0 || value > 100
                    ? ValidationResult.Error("[red]Enter a percentage from 0 to 100.[/]")
                    : ValidationResult.Success()));
    }

    public static DateTime PromptDate(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<DateTime>(prompt)
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Enter a valid date such as 2026-05-27.[/]"));
    }

    public static PayFrequency PromptPayFrequency()
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

    public static EmploymentType PromptEmploymentType()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Are you salary or hourly?")
                .AddChoices(["Salary", "Hourly"]));

        return choice == "Salary" ? EmploymentType.Salary : EmploymentType.Hourly;
    }

    public static HourlyPayEstimationMode PromptHourlyPayEstimationMode()
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

    public static RegularIncomeFrequency PromptRegularIncomeFrequency()
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

    public static AccountType PromptAccountType()
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

    public static DebtType PromptDebtType()
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

    public static GoalType PromptGoal()
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

    public static decimal PromptTakeHomePayPerPayPeriod(UserProfile profile)
    {
        if (profile.TakeHomePayPerPayPeriod is > 0)
        {
            var useSavedValue = AnsiConsole.Confirm(
                $"Use your saved take-home pay estimate of {profile.TakeHomePayPerPayPeriod:C} per pay period?",
                true);

            if (useSavedValue)
            {
                return profile.TakeHomePayPerPayPeriod.Value;
            }
        }

        return PromptMoney("What is your take-home pay for a normal pay period?");
    }

    public static string? PromptDebtGoal(List<SimulatedDebt> debts)
    {
        var goal = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What is your debt payoff goal?")
                .AddChoices([
                    "Pay off all debt",
                    "Pay off a specific debt"
                ]));

        if (goal == "Pay off all debt")
        {
            return null;
        }

        var displayToDebt = debts.ToDictionary(
            debt => $"{debt.Category}: {debt.Name} ({debt.Balance:C})",
            debt => debt);

        var selectedDebt = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which debt do you want to track as the goal?")
                .AddChoices(displayToDebt.Keys));

        return displayToDebt[selectedDebt].Key;
    }
}