using TM.Core.Models;

namespace TM.Core.Enums;

public class CreditCard
{
    public int Id {get;set;}
    public int UserProfileId {get;set;}
    public UserProfile? UserProfile {get;set;}
    public string Name {get; set;} = string.Empty;
    public decimal CurrentBalance {get; set;}
    public decimal MinimumPayment {get;set;}
    public decimal InterestRateApr {get;set;}
}