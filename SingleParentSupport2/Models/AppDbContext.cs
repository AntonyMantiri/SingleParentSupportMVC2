using Microsoft.EntityFrameworkCore;

namespace SingleParentSupport2.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet properties for each table
        public DbSet<User> Users { get; set; }
        public DbSet<SupportRequest> SupportRequests { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<ChatLog> ChatLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships
            modelBuilder.Entity<SupportRequest>()
                .HasOne(sr => sr.User)
                .WithMany(u => u.SupportRequests)
                .HasForeignKey(sr => sr.UserId);

            /* modelBuilder.Entity<Appointment>()
                 .HasOne(a => a.User)
                 .WithMany(u => u.Appointments)
                 .HasForeignKey(a => a.UserId)
                 ;

             modelBuilder.Entity<Appointment>()
                 .HasOne(a => a.Volunteer)
                 .WithMany()
                 .HasForeignKey(a => a.VolunteerId);
            */

            modelBuilder.Entity<Appointment>()
               .HasOne(a => a.User)
               .WithMany(u => u.Appointments)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Cascade); // Keep cascade for UserId

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Volunteer)
                .WithMany()
                .HasForeignKey(a => a.VolunteerId)
                .OnDelete(DeleteBehavior.NoAction); // Disable cascade for VolunteerId

            modelBuilder.Entity<ChatLog>()
                .HasOne(cl => cl.User)
                .WithMany(u => u.ChatLogs)
                .HasForeignKey(cl => cl.UserId);
        }
    }
}