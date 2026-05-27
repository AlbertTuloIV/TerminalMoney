using Microsoft.EntityFrameworkCore;

namespace TM.Data.Persistence;

public static class TMDbContextFactory
{
    public static TMDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TMDbContext>()
            .UseSqlite($"Data Source={LocalDatabasepath.GetDatabasePath()}")
            .Options;

        return new TMDbContext(options);
    }
}