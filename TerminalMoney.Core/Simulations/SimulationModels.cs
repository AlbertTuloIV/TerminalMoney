namespace TM.Core.Simulations;

public class SavingsSimulationInput
{
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyLivingExpenses { get; set; }
    public decimal StartingSavings { get; set; }
    public decimal TargetSavings { get; set; }
    public int MaxMonths { get; set; } = 600;
}

public class SavingsSimulationResult
{
    public bool CanReachGoal {get;set;}
    public string Message {get;set;} = string.Empty;
    public int MonthsToGoal {get;set;}
    public decimal MonthlyAvailableForSavings {get;set;}
    public List<SavingsMonthPlan> Months {get;set;} = [];
}

public class SavingsMonthPlan
{
    public int MonthNumber {get;set;}
    public decimal StartingBalance {get;set;}
    public decimal Contribution {get;set;}
    public decimal EndingBalance {get;set;}
}

public class SimulatedDebt
{
    public string Key {get;set;} = string.Empty;
    public string Name {get;set;} = string.Empty;
    public string Category {get;set;} = string.Empty;
    public decimal Balance {get;set;}
    public decimal MinimumMonthlyPayment {get;set;}
    public decimal InterestRateApr {get;set;}
    public int Priority {get;set;}
}

public class DebtSnowballSimulationInput
{
    public decimal MonthlyIncome {get;set;}
    public decimal MonthlyLivingExpenses {get;set;}
    public string? SpecificDebtKey {get;set;}
    public int MaxMonths {get;set;} = 600;
    public List<SimulatedDebt> Debts {get;set;} = [];
}

public class DebtSnowballSimulationResult
{
    public bool CanReachGoal{get;set;}
    public string Message {get;set;} = string.Empty;
    public int MonthsToGoal {get;set;}
    public decimal MonthlyAvailableForDebt {get;set;}
    public decimal MonthlyDebtPaymentBudget {get;set;}
    public decimal InitialMonthlyMinimumDebtPayments {get;set;}
    public decimal InitialMonthlySnowballExtra {get;set;}
    public decimal StartingDebtTotal {get;set;}
    public decimal EndingDebtTotal {get;set;}
    public List<DebtSnowballMonthPlan> Months {get;set;} = [];
}

public class DebtSnowballMonthPlan
{
    public int MonthNumber {get;set;}
    public decimal MonthlyIncome {get;set;}
    public decimal LivingExpenses {get;set;}
    public decimal AvailableForDebt {get;set;}
    public decimal MinimumDebtPayments {get;set;}
    public decimal SnowballExtraPayment {get;set;}
    public decimal TotalInterestCharged {get;set;}
    public decimal TotalDebtPaid {get;set;}
    public decimal RemainingDebt {get;set;}
    public string FocusDebtName {get;set;} = string.Empty;
    public List<DebtPaymentPlan> Payments {get; set;} = [];
}

public class DebtPaymentPlan
{
    public string DebtKey {get;set;} = string.Empty;
    public string DebtName {get;set;} = string.Empty;
    public string Category {get;set;} = string.Empty;
    public decimal StartingBalance {get;set;}
    public decimal InterestCharged {get;set;}
    public decimal MinimumPayment {get;set;}
    public decimal ExtraPayment {get;set;}
    public decimal EndingBalance {get;set;}
}