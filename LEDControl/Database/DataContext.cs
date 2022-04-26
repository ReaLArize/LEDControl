using LEDControl.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace LEDControl.Database;

public sealed class DataContext : DbContext
{
    public DbSet<Video> Videos { get; set; }
    public DbSet<Device> Devices { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
}