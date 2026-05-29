using TM.Core.Models;

namespace TM.Core.Records;

public class BalanceRecord
{
    public int Id {get;set;}
    public int UserProfileId {get;set;}
    public UserProfile? UserProfile {get;set;}
    public RecordTargetKind TargetKind {get;set;}
    public int TargetId {get;set;}
    public string TargetName {get;set;} = string.Empty;
    public string CategoryName {get;set;} = string.Empty;
    public decimal PreviousBalance {get;set;}
    public decimal NewBalance {get;set;}
    public decimal ChangeAmount {get;set;}
    public DateTime RecordedAt {get;set;} = DateTime.Now;
    public DateTime CreatedAt {get;set;} = DateTime.Now;
    public string? Note {get;set;}
    
}