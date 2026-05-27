using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TM.Core.Models;
using TM.Core.Simulations;
using TM.Data.Persistence;
using TM.Cli.Components;

namespace TM.Cli.Simulations;

public class SimulationMenu(TMDbContext dbContext)
{
    public async Task ShowAsync()
    {
        var profile = await LoadProfileAsync();

        if(profile is null)
        {
            AnsiConsole.MarkupLine("[yellow]Complete setup before running simulations.[/]");
            return;
        }
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Simulation")
                    .AddChoices([
                        "Savings simulation",
                        "Debt snowball simulation",
                        "Back"
                    ]));

            switch (choice)
            {
                case "Savings simulation":
                    RunSavingsSimulation(profile);
                    break;
                case "Debt snowball simulation":
                    RunDebtSnowballSimulation(profile);
                    break;
                case "Back":
                    return;
            }
        }
    }

    private async Task<UserProfile?> LoadProfileAsync()
    {
        return await dbContext.UserProfiles
            .Include(x => x.RegularIncomeSources)
            .Include(x => x.FinancialAccounts)
            .Include(x => x.CreditCards)
            .Include(x => x.DebtAccounts)
            .Include(x => x.PaycheckSamples)
            .FirstOrDefaultAsync();
    }

    private static void RunSavingsSimulation(UserProfile profile)
    {
        var takeHomePay = Prompts.PromptTakeHomePayPerPayPeriod(profile);
        var monthlyIncome = SimulationEngine.GetMonthlyInce(profile, takeHomePay);

        AnsiConsole.MarkupLine($"Estimated monthly income: [green]{monthlyIncome:C}[/]");

        var monthlyLivingExpenses = Prompts.PromptMoney("What is your normal living cost, not including current financial obligations?");
        var currentSavings = Prompts.PromptMoney("How much do you currently have saved?");
        var targetSavings = Prompts.PromptMoney("What savings amount do you want to reach?");

        var result = SimulationEngine.RunSavingsSimulation(new SavingsSimulationInput
        {
            MonthlyIncome = monthlyIncome,
            MonthlyLivingExpenses = monthlyLivingExpenses,
            StartingSavings = currentSavings,
            TargetSavings = targetSavings
        });

        RenderSavingsResult(result, monthlyIncome, monthlyLivingExpenses, profile);
    }

    private static void RunDebtSnowballSimulation(UserProfile profile)
    {
        var debts = SimulationEngine.BuildDebts(profile);
        
        if(debts.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No debt found in your setup.[/]");
            return;
        }

        var takeHomePay = Prompts.PromptTakeHomePayPerPayPeriod(profile);
        var monthlyIncome = SimulationEngine.GetMonthlyInce(profile, takeHomePay);

        AnsiConsole.MarkupLine($"Estimated monthly income: [green]{monthlyIncome:C}[/]");

        var monthlyLivingExpenses = Prompts.PromptMoney("What are your normal monthly living expenses? Do not include debt payments; TerminalMoney will add saved minimum payments automatically.");
        var specificDebtKey = Prompts.PromptDebtGoal(debts);

        var result = SimulationEngine.RunDebtSnowballSimulation(new DebtSnowballSimulationInput
        {
            MonthlyIncome = monthlyIncome,
            MonthlyLivingExpenses = monthlyLivingExpenses,
            SpecificDebtKey = specificDebtKey,
            Debts = debts
        });

        RenderDebtSnowballResult(result, monthlyIncome, monthlyLivingExpenses, profile);
    }

    private static void RenderSavingsResult(SavingsSimulationResult result, decimal monthlyIncome, decimal monthlyLivingExpenses, UserProfile profile)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(result.CanReachGoal ? "[green]Savings simulation complete.[/]" : "[yellow]Saving Simulation Warning.[/]");
        AnsiConsole.MarkupLine(result.Message);

        var availableForSavings = Math.Max(0, monthlyIncome - monthlyLivingExpenses);
        var remainder = Math.Max(0, monthlyIncome - monthlyLivingExpenses - availableForSavings);

        AnsiConsole.Write(new BreakdownChart()
            .Width(80)
            .AddItem("Living Expenses", (double)monthlyLivingExpenses, Color.Blue)
            .AddItem("Savings", (double)availableForSavings, Color.Green)
            .AddItem("Remaining", (double)remainder, Color.Grey));

        var summary = new Table()
            .Title("Savings Summary")
            .AddColumn("Field")
            .AddColumn("Value");

        summary.AddRow("Monthly Income", monthlyIncome.ToString("C"));
        summary.AddRow("Monthly Living Expenses", monthlyLivingExpenses.ToString("C"));
        summary.AddRow("Available For Savings", result.MonthlyAvailableForSavings.ToString("C"));
        summary.AddRow("Months to goal", result.CanReachGoal ? result.MonthsToGoal.ToString() : "Not reachable.");

        AnsiConsole.Write(summary);
        WritePaycheckOutline("Savings paycheck outline", profile, ("Living Expenses", monthlyLivingExpenses), ("Savings contribution", Math.Max(0, result.MonthlyAvailableForSavings)));

        if(result.Months.Count == 0)
        {
            return;
        }

        var chart = new BarChart()
            .Width(80)
            .Label("Savings progress")
            .CenterLabel();
        
        foreach(var month in SampleSavingsMonths(result.Months))
        {
            chart.AddItem($"Month {month.MonthNumber}", (double)month.EndingBalance, Color.Green);
        }

        AnsiConsole.Write(chart);
        WriteSavingsPlanTable(result.Months);
    }

    private static void RenderDebtSnowballResult(DebtSnowballSimulationResult result, decimal monthlyIncome, decimal monthlyLivingExpenses, UserProfile profile)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(result.CanReachGoal ? "[green]Debt snowball simulation complete.[/]" : "[yellow]Debt snowball simulation warning.[/]");
        AnsiConsole.MarkupLine(result.Message);

        AnsiConsole.Write(new BreakdownChart()
            .Width(80)
            .AddItem("Living Expenses", (double)monthlyLivingExpenses, Color.Blue)
            .AddItem("Minimum Debt Payments", (double)result.InitialMonthlyMinimumDebtPayments, Color.Red)
            .AddItem("Snowball Extra", (double)result.InitialMonthlySnowballExtra, Color.Green)
            .AddItem("Remaining", 0, Color.Grey));
        
        var summary = new Table()
        .Title("Debt Snowball Summary")
        .AddColumn("Field")
        .AddColumn("Value");

        summary.AddRow("Starting Debt", result.StartingDebtTotal.ToString("C"));
        summary.AddRow("Ending Debt", result.EndingDebtTotal.ToString("C"));
        summary.AddRow("Monthly Income", monthlyIncome.ToString("C"));
        summary.AddRow("Monthly Living Expenses", monthlyLivingExpenses.ToString("C"));
        summary.AddRow("Minimum Debt Payments", result.InitialMonthlyMinimumDebtPayments.ToString("C"));
        summary.AddRow("Snowball Extra", result.InitialMonthlySnowballExtra.ToString("C"));
        summary.AddRow("Total Debt Payment Budget", result.MonthlyDebtPaymentBudget.ToString("C"));
        summary.AddRow("Months to goal", result.CanReachGoal ? result.MonthsToGoal.ToString() : "Not Reached");

        AnsiConsole.Write(summary);
        WritePaycheckOutline(
            "Debt payoff paycheck outline",
            profile,
            ("Living Expenses", monthlyLivingExpenses),
            ("Minimum Debt Payments", result.InitialMonthlyMinimumDebtPayments),
            ("Snowball Extra", result.InitialMonthlySnowballExtra));

        if(result.Months.Count == 0)
        {
            return;
        }

        var chart = new BarChart()
            .Width(80)
            .Label("Remaining Debt")
            .CenterLabel();
        
        foreach(var month in SampleDebtMonths(result.Months))
        {
            chart.AddItem($"Month {month.MonthNumber}", (double)month.RemainingDebt, Color.Red);
        }
        AnsiConsole.Write(chart);
        WriteDebtPlanTable(result.Months);
    }

    private static void WriteSavingsPlanTable(List<SavingsMonthPlan> months)
    {
        var table = new Table()
            .Title("Savings plan")
            .AddColumn("Month")
            .AddColumn("Starting")
            .AddColumn("Contribution")
            .AddColumn("Ending");

        foreach (var month in TakePreviewMonths(months))
        {
            table.AddRow(
                month.MonthNumber.ToString(),
                month.StartingBalance.ToString("C"),
                month.Contribution.ToString("C"),
                month.EndingBalance.ToString("C"));
        }

        AnsiConsole.Write(table);
    }

    private static void WriteDebtPlanTable(List<DebtSnowballMonthPlan> months)
    {
        var table = new Table()
            .Title("Debt snowball plan")
            .AddColumn("Month")
            .AddColumn("Focus debt")
            .AddColumn("Interest")
            .AddColumn("Minimums")
            .AddColumn("Extra")
            .AddColumn("Debt paid")
            .AddColumn("Remaining debt");

        foreach (var month in TakePreviewMonths(months))
        {
            table.AddRow(
                month.MonthNumber.ToString(),
                month.FocusDebtName,
                month.TotalInterestCharged.ToString("C"),
                month.MinimumDebtPayments.ToString("C"),
                month.SnowballExtraPayment.ToString("C"),
                month.TotalDebtPaid.ToString("C"),
                month.RemainingDebt.ToString("C"));
        }

        AnsiConsole.Write(table);

        var firstMonth = months[0];
        var paymentTable = new Table()
            .Title("First month debt payment outline")
            .AddColumn("Debt")
            .AddColumn("Category")
            .AddColumn("Minimum")
            .AddColumn("Extra")
            .AddColumn("Ending balance");

        foreach (var payment in firstMonth.Payments.Where(x => x.MinimumPayment + x.ExtraPayment > 0))
        {
            paymentTable.AddRow(
                payment.DebtName,
                payment.Category,
                payment.MinimumPayment.ToString("C"),
                payment.ExtraPayment.ToString("C"),
                payment.EndingBalance.ToString("C"));
        }

        AnsiConsole.Write(paymentTable);
    }

    private static void WritePaycheckOutline(
        string title,
        UserProfile profile,
        params (string Label, decimal MonthlyAmount)[] allocations)
    {
        var payPeriodsPerMonth = SimulationEngine.GetPeriodsPerMonth(profile.PayFrequency);
        var table = new Table()
            .Title(title)
            .AddColumn("Category")
            .AddColumn("Monthly amount")
            .AddColumn("Per paycheck estimate");

        foreach (var allocation in allocations)
        {
            table.AddRow(
                allocation.Label,
                allocation.MonthlyAmount.ToString("C"),
                Math.Round(allocation.MonthlyAmount / payPeriodsPerMonth, 2, MidpointRounding.AwayFromZero).ToString("C"));
        }

        AnsiConsole.Write(table);
    }

    private static List<SavingsMonthPlan> SampleSavingsMonths(List<SavingsMonthPlan> months)
    {
        var step = Math.Max(1, months.Count / 12);
        return months
            .Where((_, index) => index % step == 0)
            .Take(12)
            .ToList();
    }

    private static List<DebtSnowballMonthPlan> SampleDebtMonths(List<DebtSnowballMonthPlan> months)
    {
        var step = Math.Max(1, months.Count / 12);
        return months
            .Where((_, index) => index % step == 0)
            .Take(12)
            .ToList();
    }

    private static IEnumerable<T> TakePreviewMonths<T>(List<T> months)
    {
        if (months.Count <= 13)
        {
            return months;
        }

        return months.Take(12).Concat([months[^1]]);
    }

    private static decimal PromptMoney(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<decimal>(prompt)
                .Validate(value => value < 0
                    ? ValidationResult.Error("[red]Enter zero or a positive amount.[/]")
                    : ValidationResult.Success()));
    }
}