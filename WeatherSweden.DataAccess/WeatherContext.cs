using Microsoft.EntityFrameworkCore;
using WeatherSweden.Core;

namespace WeatherSweden.DataAccess
{
    public class WeatherContext : DbContext
    {
        public DbSet<WeatherData> WeatherDatas { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Här sätter vi namnet på databasen till WeatherSwedenDB
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=WeatherSwedenDB;Trusted_Connection=True;");
        }
    }
}