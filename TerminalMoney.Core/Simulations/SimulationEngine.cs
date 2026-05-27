using TM.Core.Enums;
using TM.Core.Models;

namespace TM.Core.Simulations;

public static class SimulationEngine
{
    public static decimal GetPeriodsPerMonth(PayFrequency payFrequency)
    {
        return payFrequency switch
        {
            PayFrequency.Weekly => 52m / 12m,
            PayFrequency.BiWeekly => 26m / 12m,
            PayFrequency.SemiMonthly => 2m,
            PayFrequency.Monthly => 1m,
            _ => 1m
        };
    }

    public static decimal GetMonthlyInce(UserProfile profile, decimal takeHomePayPerPayPeriod)
    {
        var payIncome = takeHomePayPerPayPeriod * GetPeriodsPerMonth(profile.PayFrequency);
        var additionalIncome = profile.RegularIncomeSources.Sum(x => ToMonthlyAmount(x.Amount, x.Frequency));

        return RoundMoney(payIncome + additionalIncome);
    }

    public static List<SimulatedDebt> BuildDebts(UserProfile profile)
    {
        var debts = new List<SimulatedDebt>();

        debts.AddRange(profile.CreditCards.Select(card => new SimulatedDebt
        {
            Key = $"credit-card:{card.Id}",
            Name = card.Name,
            Category = "Credit Card",
            Balance = card.CurrentBalance,
            MinimumMonthlyPayment = card.MinimumPayment,
            InterestRateApr = card.InterestRateApr,
            Priority = 1
        }));

        debts.AddRange(profile.DebtAccounts.Select(debt => new SimulatedDebt
        {
            Key = $"debt:{debt.Id}",
            Name = debt.Name,
            Category = GetDebtCategoryName(debt.DebtType),
            Balance = debt.CurrentBalance,
            MinimumMonthlyPayment = debt.MonthlyPayment,
            InterestRateApr = debt.InterestRateApr,
            Priority = GetDebtPriority(debt.DebtType)
        }));

        return debts
            .Where(x => x.Balance > 0)
            .OrderBy(x => x.Priority)
            .ThenByDescending(x => x.Balance)
            .ThenBy(x => x.Name)
            .ToList();
    }
    public static SavingsSimulationResult RunSavingsSimulation(SavingsSimulationInput input)
    {
        var result = new SavingsSimulationResult
        {
            MonthlyAvailableForSavings = RoundMoney(input.MonthlyIncome - input.MonthlyLivingExpenses)
        };

        if (input.TargetSavings <= input.StartingSavings)
        {
            result.CanReachGoal = true;
            result.Message = "You have already reached this savings goal.";
            return result;
        }

        if (result.MonthlyAvailableForSavings <= 0)
        {
            result.CanReachGoal = false;
            result.Message = "This goal cannot be reached with the current monthly income and living expenses.";
            return result;
        }

        var currentSavings = input.StartingSavings;

        for (var month = 1; month <= input.MaxMonths && currentSavings < input.TargetSavings; month++)
        {
            var startingBalance = currentSavings;
            var contribution = Math.Min(result.MonthlyAvailableForSavings, input.TargetSavings - currentSavings);
            currentSavings = RoundMoney(currentSavings + contribution);

            result.Months.Add(new SavingsMonthPlan
            {
                MonthNumber = month,
                StartingBalance = startingBalance,
                Contribution = contribution,
                EndingBalance = currentSavings
            });
        }

        result.CanReachGoal = currentSavings >= input.TargetSavings;
        result.MonthsToGoal = result.Months.Count;
        result.Message = result.CanReachGoal
            ? $"You can reach this savings goal in {result.MonthsToGoal} month(s)."
            : $"This goal was not reached within {input.MaxMonths} months.";

        return result;
    }

    public static DebtSnowballSimulationResult RunDebtSnowballSimulation(DebtSnowballSimulationInput input)
    {
        var debts = input.Debts
            .Where(x => x.Balance > 0)
            .Select(CloneDebt)
            .OrderBy(x => x.Priority)
            .ThenByDescending(x => x.Balance)
            .ThenBy(x => x.Name)
            .ToList();

        var result = new DebtSnowballSimulationResult
        {
            MonthlyAvailableForDebt = RoundMoney(input.MonthlyIncome - input.MonthlyLivingExpenses),
            StartingDebtTotal = RoundMoney(debts.Sum(x => x.Balance))
        };

        if (debts.Count == 0)
        {
            result.CanReachGoal = true;
            result.Message = "There is no debt to simulate.";
            return result;
        }

        if (result.MonthlyAvailableForDebt <= 0)
        {
            result.CanReachGoal = false;
            result.Message = "There is no monthly money available for debt after living expenses.";
            result.EndingDebtTotal = result.StartingDebtTotal;
            return result;
        }

        if (input.SpecificDebtKey is not null && debts.All(x => x.Key != input.SpecificDebtKey))
        {
            result.CanReachGoal = false;
            result.Message = "The selected debt could not be found.";
            result.EndingDebtTotal = result.StartingDebtTotal;
            return result;
        }

        for (var month = 1; month <= input.MaxMonths && !HasReachedDebtGoal(debts, input.SpecificDebtKey); month++)
        {
            var activeDebts = GetActiveDebts(debts);
            var availableForDebt = result.MonthlyAvailableForDebt;
            var monthPlan = new DebtSnowballMonthPlan
            {
                MonthNumber = month,
                MonthlyIncome = input.MonthlyIncome,
                LivingExpenses = input.MonthlyLivingExpenses,
                AvailableForDebt = availableForDebt,
                FocusDebtName = activeDebts.FirstOrDefault()?.Name ?? string.Empty
            };

            foreach (var debt in activeDebts)
            {
                var startingBalance = debt.Balance;
                var interestCharged = RoundMoney(debt.Balance * (debt.InterestRateApr / 100m) / 12m);
                debt.Balance = RoundMoney(debt.Balance + interestCharged);

                monthPlan.Payments.Add(new DebtPaymentPlan
                {
                    DebtKey = debt.Key,
                    DebtName = debt.Name,
                    Category = debt.Category,
                    StartingBalance = startingBalance,
                    InterestCharged = interestCharged,
                    EndingBalance = debt.Balance
                });
            }

            PayMinimums(activeDebts, monthPlan, ref availableForDebt);
            PaySnowballExtra(debts, monthPlan, ref availableForDebt);

            monthPlan.TotalInterestCharged = RoundMoney(monthPlan.Payments.Sum(x => x.InterestCharged));
            monthPlan.TotalDebtPaid = RoundMoney(monthPlan.Payments.Sum(x => x.MinimumPayment + x.ExtraPayment));
            monthPlan.RemainingDebt = RoundMoney(debts.Sum(x => x.Balance));

            result.Months.Add(monthPlan);

            if (monthPlan.TotalDebtPaid <= 0)
            {
                break;
            }
        }

        result.EndingDebtTotal = RoundMoney(debts.Sum(x => x.Balance));
        result.MonthsToGoal = result.Months.Count;
        result.CanReachGoal = HasReachedDebtGoal(debts, input.SpecificDebtKey);
        result.Message = result.CanReachGoal
            ? $"The debt goal can be reached in {result.MonthsToGoal} month(s)."
            : $"The debt goal was not reached within {input.MaxMonths} months.";

        return result;
    }

    private static void PayMinimums(List<SimulatedDebt> activeDebts, DebtSnowballMonthPlan monthPlan, ref decimal availableForDebt)
    {
        foreach (var debt in activeDebts)
        {
            if (availableForDebt <= 0)
            {
                return;
            }

            var payment = Math.Min(debt.MinimumMonthlyPayment, debt.Balance);
            payment = Math.Min(payment, availableForDebt);

            ApplyPayment(debt, payment);
            availableForDebt = RoundMoney(availableForDebt - payment);

            var paymentPlan = monthPlan.Payments.Single(x => x.DebtKey == debt.Key);
            paymentPlan.MinimumPayment = RoundMoney(paymentPlan.MinimumPayment + payment);
            paymentPlan.EndingBalance = debt.Balance;
        }
    }

    private static void PaySnowballExtra(List<SimulatedDebt> debts, DebtSnowballMonthPlan monthPlan, ref decimal availableForDebt)
    {
        while (availableForDebt > 0)
        {
            var focusDebt = GetActiveDebts(debts).FirstOrDefault();

            if (focusDebt is null)
            {
                return;
            }

            var payment = Math.Min(availableForDebt, focusDebt.Balance);
            ApplyPayment(focusDebt, payment);
            availableForDebt = RoundMoney(availableForDebt - payment);

            var paymentPlan = monthPlan.Payments.Single(x => x.DebtKey == focusDebt.Key);
            paymentPlan.ExtraPayment = RoundMoney(paymentPlan.ExtraPayment + payment);
            paymentPlan.EndingBalance = focusDebt.Balance;
        }
    }

    private static void ApplyPayment(SimulatedDebt debt, decimal payment)
    {
        debt.Balance = RoundMoney(Math.Max(0, debt.Balance - payment));
    }

    private static bool HasReachedDebtGoal(List<SimulatedDebt> debts, string? specificDebtKey)
    {
        if (specificDebtKey is null)
        {
            return debts.All(x => x.Balance <= 0);
        }

        return debts.Single(x => x.Key == specificDebtKey).Balance <= 0;
    }

    private static List<SimulatedDebt> GetActiveDebts(IEnumerable<SimulatedDebt> debts)
    {
        return debts
            .Where(x => x.Balance > 0)
            .OrderBy(x => x.Priority)
            .ThenByDescending(x => x.Balance)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static SimulatedDebt CloneDebt(SimulatedDebt debt)
    {
        return new SimulatedDebt
        {
            Key = debt.Key,
            Name = debt.Name,
            Category = debt.Category,
            Balance = debt.Balance,
            MinimumMonthlyPayment = debt.MinimumMonthlyPayment,
            InterestRateApr = debt.InterestRateApr,
            Priority = debt.Priority
        };
    }

    private static decimal ToMonthlyAmount(decimal amount, RegularIncomeFrequency frequency)
    {
        return frequency switch
        {
            RegularIncomeFrequency.Weekly => amount * 52m / 12m,
            RegularIncomeFrequency.BiWeekly => amount * 26m / 12m,
            RegularIncomeFrequency.SemiMonthly => amount * 2m,
            RegularIncomeFrequency.Monthly => amount,
            RegularIncomeFrequency.Quarterly => amount / 3m,
            RegularIncomeFrequency.Yearly => amount / 12m,
            _ => amount
        };
    }

    private static int GetDebtPriority(DebtType debtType)
    {
        return debtType switch
        {
            DebtType.PersonalLoan => 2,
            DebtType.AutoLoan => 3,
            DebtType.MedicalDept => 4,
            DebtType.Other => 5,
            DebtType.Mortage => 6,
            DebtType.StudentLoan => 7,
            _ => 99
        };
    }

    private static string GetDebtCategoryName(DebtType debtType)
    {
        return debtType switch
        {
            DebtType.PersonalLoan => "Personal Loan",
            DebtType.AutoLoan => "Car Loan",
            DebtType.MedicalDept => "Medical Debt",
            DebtType.Other => "Other",
            DebtType.Mortage => "Mortgage",
            DebtType.StudentLoan => "Student Loan",
            _ => "Debt"
        };
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}