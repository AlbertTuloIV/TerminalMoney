using Microsoft.EntityFrameworkCore;
using TM.Core.Enums;
using TM.Core.Models;
using TM.Core.Records;

namespace TM.Data.Persistence;

public class TMDbContext(DbContextOptions<TMDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RegularIncomeSource> RegularIncomeSources => Set<RegularIncomeSource>();
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<DebtAccount> DebtAccounts => Set<DebtAccount>();
    public DbSet<PaycheckSample> PaycheckSamples => Set<PaycheckSample>();

    public DbSet<BalanceRecord> BalanceRecords => Set<BalanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.PrimaryJob).HasMaxLength(150);
            entity.Property(x => x.PayFrequency).HasConversion<string>().HasMaxLength(25);
            entity.Property(x => x.EmploymentType).HasConversion<string>().HasMaxLength(25);
            entity.Property(x => x.HourlyPayEstimationMode).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.PrimaryGoal).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.TakeHomePayPerPayPeriod).HasPrecision(18, 2);

            entity.HasMany(x => x.RegularIncomeSources)
                .WithOne(x => x.UserProfile)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.FinancialAccounts)
                .WithOne(x => x.UserProfile)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.CreditCards)
                .WithOne(x => x.UserProfile)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.DebtAccounts)
                .WithOne(x => x.UserProfile)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.PaycheckSamples)
                .WithOne(x => x.UserProfile)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.BalanceRecords)
                .WithOne(x => x.UserProfile)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RegularIncomeSource>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Amount).HasPrecision(18,2);
            entity.Property(x => x.Frequency).HasConversion<string>().HasMaxLength(25);
        });

        modelBuilder.Entity<FinancialAccount>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(25);
            entity.Property(x => x.CurrentBalance).HasPrecision(18,2);
        });

        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.CurrentBalance).HasPrecision(18,2);
            entity.Property(x => x.CreditLimit).HasPrecision(18, 2);
            entity.Property(x => x.MinimumPayment).HasPrecision(18,2);
            entity.Property(x => x.InterestRateApr).HasPrecision(8,4);
        });

        modelBuilder.Entity<DebtAccount>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.DebtType).HasConversion<string>().HasMaxLength(25); 
            entity.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            entity.Property(x => x.MonthlyPayment).HasPrecision(18, 2);
            entity.Property(x => x.InterestRateApr).HasPrecision(8,4);
        });

        modelBuilder.Entity<PaycheckSample>(entity =>
        {
            entity.Property(x => x.TakeHomePay).HasPrecision(18,2);
        });

        modelBuilder.Entity<BalanceRecord>(entity =>
        {
            entity.Property(x => x.TargetKind).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.TargetName).HasMaxLength(150);
            entity.Property(x => x.CategoryName).HasMaxLength(100);
            entity.Property(x => x.PreviousBalance).HasPrecision(18,2);
            entity.Property(x => x.NewBalance).HasPrecision(18,2);
            entity.Property(x => x.ChangeAmount).HasPrecision(18,2);
            entity.Property(x => x.Note).HasMaxLength(500);

            entity.HasIndex(x => new { x.UserProfileId, x.RecordedAt }) ;
            entity.HasIndex(x => new { x.TargetKind, x.TargetId, x.RecordedAt });
        });
    }
}