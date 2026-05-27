using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TM.Data.Persistence;

public class TMDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TMDbContext>
{
    public TMDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TMDbContext>()
            .UseSqlite($"Data Source={LocalDatabasepath.GetDatabasePath()}")
            .Options;
        
        return new TMDbContext(options);
    }
}