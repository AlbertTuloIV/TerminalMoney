using TM.Core.Enums;

namespace TM.Core.Models;

public class UserProfile
{
    public int Id {get;set;}
    public string Name {get;set;} = string.Empty;
    public int Age {get;set;}
    public string PrimaryJob {get;set;} = string.Empty;
    public PayFrequency PayFrequency {get;set;}
    public EmploymentType EmploymentType {get;set;}
    public bool CanEarnOvertime {get;set;}
    public decimal? TakeHomePayPerPayPeriod {get;set;}
    public GoalType PrimaryGoal {get;set;}
    public DateTime CreatedAt {get;set;} = DateTime.Now; // using DateTime.Now instead of .UTC due to it being locally hosted sqlite database.
    public DateTime UpdatedAt {get;set;} = DateTime.Now;

    public ICollection<RegularIncomeSource> RegularIncomeSources {get;set;} = [];
    public ICollection<FinancialAccount> FinancialAccounts {get;set;} = [];
    public ICollection<CreditCard> CreditCards {get;set;} = [];
    public ICollection<DebtAccount> DebtAccounts {get;set;} = [];
    public ICollection<PaycheckSample> PaycheckSamples {get;set;} = [];
}