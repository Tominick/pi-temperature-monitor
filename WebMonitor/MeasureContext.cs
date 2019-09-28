
using MeasApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MeasApi
{
    public class MeasureContext : DbContext
    {
        private const string DatabaseName = "MyDatabase.db";
        //public static string contentRoot = string.Empty;
        
        public MeasureContext(DbContextOptions<MeasureContext> options, string contentRoot)
                : base(options)
        {
            if (!System.IO.File.Exists(System.IO.Path.Combine(contentRoot, DatabaseName))) 
            {
                Database.EnsureCreated();

                //Measures.Add(new Measure {SensorId=1, DateTime = System.DateTime.Now, Temperature = 25.0f});
                //SaveChanges();
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=" + DatabaseName);
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Measure>().HasKey(c => new { c.SensorId, c.DateTime });
            modelBuilder.Entity<Sensor>().HasKey(c => new { c.SensorId });
        }

        public DbSet<Measure> Measures { get; set; }

        public DbSet<Sensor> Sensors { get; set; }
    }
}