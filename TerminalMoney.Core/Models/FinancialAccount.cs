using TM.Core.Enums;

namespace TM.Core.Models;

public class FinancialAccount
{
    public int Id {get;set;}
    public int UserProfileId {get;set;}
    public UserProfile? UserProfile {get;set;}
    public string Name {get;set;} = string.Empty;
    public AccountType AccountType {get;set;}
    public decimal CurrentBalance {get;set;}
}