namespace TM.Core.Enums;

public enum PayFrequency
{
    Weekly = 1,
    BiWeekly = 2,
    SemiMonthly = 3,
    Monthly = 4
}

public enum EmploymentType
{
    Salary = 1,
    Hourly = 2
}

public enum HourlyPayEstimationMode
{
    EnterExpectedTakeHomeNow = 1,
    EstimateFromPreviousPaychecks = 2,
    EnterManuallyEachPayPeriod = 3    
}

public enum GoalType
{
    SaveMoney = 1,
    PayOffDebt = 2,
    TrackInformation = 3
}

public enum AccountType
{
    Checking = 1,
    Savings = 2,
    Brokerage = 3,
    Retirement = 4,
    Cash = 5,
    other = 6
}

public enum DebtType
{
    PersonalLoan = 1,
    AutoLoan = 2,
    StudentLoad = 3,
    Mortage = 4,
    MedicalDebpt = 5,
    Other = 6
}

public enum RegularIncomeFrequency
{
    Weekly = 1,
    BiWeekly = 2,
    SemiMonthly = 3,
    Monthly = 4,
    Quarterly = 5,
    Yearly = 6
}