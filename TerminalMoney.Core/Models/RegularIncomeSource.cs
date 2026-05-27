using TM.Core.Enums;

namespace TM.Core.Models;

public class RegularIncomeSource
{
    public int Id {get;set;}
    public int UserProfileId {get;set;}
    public UserProfile? UserProfile {get;set;}
    public string Name {get;set;} = string.Empty;
    public decimal Amount {get;set;} 
    public RegularIncomeFrequency Frequency {get;set;}
}