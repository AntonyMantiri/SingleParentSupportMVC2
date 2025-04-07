using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SingleParentSupport2.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Add custom user properties here
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.Now;
        public bool IsVolunteer { get; set; } = false;
        public string VolunteerRole { get; set; }
        public string VolunteerBio { get; set; }
    }

    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<SupportRequest> SupportRequests { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<ChatLog> ChatLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints here
            modelBuilder.Entity<SupportRequest>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Volunteer)
                .WithMany()
                .HasForeignKey(a => a.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatLog>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatLog>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed roles
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "2", Name = "Volunteer", NormalizedName = "VOLUNTEER" },
                new IdentityRole { Id = "3", Name = "User", NormalizedName = "USER" }
            );
        }
    }
}
