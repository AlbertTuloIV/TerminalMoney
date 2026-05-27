using TM.Core.Models;

namespace TM.Core.Models;

public class PaycheckSample
{
    public int Id {get;set;}
    public int UserProfileId {get;set;}
    public UserProfile? UserProfile {get;set;}
    public DateTime PayDate {get;set;}
    public decimal TakeHomePay {get;set;}
}