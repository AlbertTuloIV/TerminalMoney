using TM.Core.Enums;
using TM.Core.Models;

namespace TM.Core.Models;

public class DebtAccount
{
    public int Id {get;set;}
    public int UserProfileId {get;set;}
    public UserProfile? UserProfile {get;set;}
    public string Name {get;set;} = string.Empty;
    public DebtType DebtType {get;set;}
    public decimal CurrentBalance {get;set;}
    public decimal MonthlyPayment {get;set;}
    public decimal InterestRateApr {get;set;}
}