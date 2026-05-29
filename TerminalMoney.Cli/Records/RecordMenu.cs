using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TM.Cli.Components;
using TM.Core.Enums;
using TM.Core.Models;
using TM.Core.Records;
using TM.Data.Persistence;

namespace TM.Cli.Records;

public class RecordMenu(TMDbContext dbContext)
{
    public async Task ShowAsync()
    {
        var profile = await LoadProfileAsync();

        if(profile is null)
        {
            AnsiConsole.MarkupLine("[yellow]Complete setup before recording account changes.[/]");
            return;
        }

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices([
                        "Record Assets",
                        "Record Debts",
                        "Recent Records",
                        "Back"
                    ])
            );

            switch (choice)
            {
                case "Record Assets":
                    await RecordAssetAsync(profile);
                    break;
                case "Record Debts":
                    await RecordDebtAsync(profile);
                    break;
                case "Recent Records":
                    await ShowRecentRecordsAsync(profile.Id);
                    break;
                case "Back":
                    return;
            }
        }
    }

    private async Task<UserProfile> LoadProfileAsync()
    {
        return await dbContext.UserProfiles
            .Include(x => x.FinancialAccounts)
            .Include(x => x.CreditCards)
            .Include(x => x.DebtAccounts)
            .FirstOrDefaultAsync();
    }

    private async Task RecordAssetAsync(UserProfile profile)
    {
        if(profile.FinancialAccounts.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No asset accounts were found. Add accounts in Settings first.[/]");
            return;
        }

        var selectedType = PromptAssetType(profile.FinancialAccounts);
        var accounts = profile.FinancialAccounts
            .Where(x => x.AccountType == selectedType)
            .OrderBy(x => x.Name)
            .ToList();
        
        if(accounts.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No {GetAccountTypeDisplayName(selectedType)} accounts were found.[/]");
            return;
        }

        var selectedAccount = PromptFinancialAccount(accounts);
        var newBalance = Prompts.PromptMoney($"What is the current balance for {selectedAccount.Name}?");
        var recordedAt = PromptRecordDate();
        var note = PromptOptionalText("Optional note, or press Enter to skip:");

        AddBalanceRecord(
            profile,
            RecordTargetKind.FinancialAccount,
            selectedAccount.Id,
            selectedAccount.Name,
            GetAccountTypeDisplayName(selectedAccount.AccountType),
            selectedAccount.CurrentBalance,
            newBalance,
            recordedAt,
            note);

        selectedAccount.CurrentBalance = newBalance;
        profile.UpdatedAt = DateTime.Now;

        await dbContext.SaveChangesAsync();

        AnsiConsole.MarkupLine($"[green]Recorded {selectedAccount.Name}: {newBalance:C}[/]");
    }

    private async Task RecordDebtAsync(UserProfile profile)
    {
        var targets = BuildDebtTargets(profile);

        if(targets.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No Debts were found. Add credit cards, loans, or mortgages in Settings first.[/]");
            return;
        }

        var selectedCategory = PromptDebtCategory(targets);
        var categoryTargets = targets
            .Where(x => x.CategoryName == selectedCategory)
            .OrderBy(x => x.Name)
            .ToList();
        
        if(categoryTargets.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No {selectedCategory} accounts were found.[/]");
            return;
        }

        var selectedTarget = PromptDebtTarget(categoryTargets);
        var newBalance = Prompts.PromptMoney($"What is the current balance for {selectedTarget.Name}?");
        var recordedAt = PromptRecordDate();
        var note = PromptOptionalText("Optional note, or press Enter to skip:");

        AddBalanceRecord(
            profile,
            selectedTarget.TargetKind,
            selectedTarget.Id,
            selectedTarget.Name,
            selectedTarget.CategoryName,
            selectedTarget.CurrentBalance,
            newBalance,
            recordedAt,
            note);
        
        selectedTarget.SetBalance(newBalance);
        profile.UpdatedAt = DateTime.Now;

        await dbContext.SaveChangesAsync();

        AnsiConsole.MarkupLine($"[green]Recorded {selectedTarget.Name}: {newBalance:C}[/]");
    }
    private async Task ShowRecentRecordsAsync(int userProfileId)
    {
        var records = await dbContext.BalanceRecords
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.RecordedAt)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .ToListAsync();

        if(records.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No balance records have been saved yet.[/]");
            return;
        }

        var table = new Table()
            .Title("Recent Records")
            .AddColumn("Date")
            .AddColumn("Category")
            .AddColumn("Account")
            .AddColumn("Previous")
            .AddColumn("Current")
            .AddColumn("Change")
            .AddColumn("Note");

        foreach(var record in records)
        {
            table.AddRow(
                record.RecordedAt.ToShortDateString(),
                record.CategoryName,
                record.TargetName,
                record.PreviousBalance.ToString("C"),
                record.NewBalance.ToString("C"),
                FormatChange(record.ChangeAmount),
                string.IsNullOrWhiteSpace(record.Note) ? string.Empty : record.Note);
        }

        AnsiConsole.Write(table);
    }

    private static AccountType PromptAssetType(ICollection<FinancialAccount> accounts)
    {
        var displayToType = accounts
            .Select(x => x.AccountType)
            .Distinct()
            .OrderBy(x => GetAccountTypeDisplayName(x))
            .ToDictionary(GetAccountTypeDisplayName, x => x);
        
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which asset type?")
                .AddChoices(displayToType.Keys)
        );

        return displayToType[selected];
    }

    private static FinancialAccount PromptFinancialAccount(List<FinancialAccount> accounts)
    {
        var displayToAccount = accounts.ToDictionary(
            account => $"{account.Name} - {account.CurrentBalance:C} (ID {account.Id})",
            account => account
        );

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which account?")
                .AddChoices(displayToAccount.Keys)
        );

        return displayToAccount[selected];
    }

    private static string PromptDebtCategory(List<RecordTarget> targets)
    {
        var categories = targets
            .Select(x => x.CategoryName)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which debt type?")
                .AddChoices(categories));
    }

    private static RecordTarget PromptDebtTarget(List<RecordTarget> targets)
    {
        var displayToTarget = targets.ToDictionary(
            target => $"{target.Name} - {target.CurrentBalance:C} (ID {target.Id})",
            target => target);

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which debt account?")
                .AddChoices(displayToTarget.Keys));

        return displayToTarget[selected];
    }

    private static List<RecordTarget> BuildDebtTargets(UserProfile profile)
    {
        var targets = new List<RecordTarget>();

        targets.AddRange(profile.CreditCards.Select(card => new RecordTarget
        {
            Id = card.Id,
            Name = card.Name,
            CategoryName = "Credit Card",
            TargetKind = RecordTargetKind.CreditCard,
            CurrentBalance = card.CurrentBalance,
            SetBalance = value => card.CurrentBalance = value
        }));

        targets.AddRange(profile.DebtAccounts.Select(debt => new RecordTarget
        {
            Id = debt.Id,
            Name = debt.Name,
            CategoryName = GetDebtTypeDisplayName(debt.DebtType),
            TargetKind = RecordTargetKind.DebtAccount,
            CurrentBalance = debt.CurrentBalance,
            SetBalance = value => debt.CurrentBalance = value
        }));

        return targets;
    }

    private void AddBalanceRecord(UserProfile profile, RecordTargetKind targetKind, int targetId, string targetName, string categoryName,
        decimal previousBalance, decimal newBalance, DateTime recordedAt, string? note)
    {
        dbContext.BalanceRecords.Add(new BalanceRecord
        {
            UserProfileId = profile.Id,
            TargetKind = targetKind,
            TargetName = targetName,
            CategoryName = categoryName,
            PreviousBalance = previousBalance,
            NewBalance = newBalance,
            ChangeAmount = newBalance - previousBalance,
            RecordedAt = recordedAt,
            CreatedAt = DateTime.Now,
            Note = note
        });
    }

    private static DateTime PromptRecordDate()
    {
        if(AnsiConsole.Confirm("Use today's date for this record?", true))
        {
            return DateTime.Today;
        }

        return Prompts.PromptDate("What date should be used for this record?");
    }

    private static string? PromptOptionalText(string prompt)
    {
        var value = AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
            .AllowEmpty());
        
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FormatChange(decimal changeAmount)
    {
        if(changeAmount > 0)
        {
            return $"[green]+{changeAmount:C}[/]";
        }
        if(changeAmount < 0)
        {
            return $"[red]{changeAmount:C}[/]";
        }

        return changeAmount.ToString("C");
    }

    private static string GetAccountTypeDisplayName(AccountType accountType)
    {
        return accountType switch
        {
            AccountType.Checking => "Checking",
            AccountType.Savings => "Savings",
            AccountType.Brokerage => "Brokerage",
            AccountType.Retirement => "Retirement",
            AccountType.Cash => "Cash",
            AccountType.other => "Other",
            _ => accountType.ToString()
        };
    }

    private static string GetDebtTypeDisplayName(DebtType debtType)
    {
        return debtType switch
        {
            DebtType.PersonalLoan => "Personal Loan",
            DebtType.AutoLoan => "Car Loan",
            DebtType.StudentLoan => "Student Loan",
            DebtType.Mortage => "Mortgage",
            DebtType.MedicalDept => "Medical Debt",
            DebtType.Other => "Other",
            _ => debtType.ToString()
        };
    }

    private sealed class RecordTarget
    {
        public int Id {get;set;}
        public string Name {get;set;} = string.Empty;
        public string CategoryName {get; init;} = string.Empty;
        public RecordTargetKind TargetKind {get; init;}
        public decimal CurrentBalance {get; init; }
        public Action<decimal> SetBalance {get; init; } = _ => { };
    }
}