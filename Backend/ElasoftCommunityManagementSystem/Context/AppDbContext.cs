using ElasoftCommunityManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<UserModel> Users { get; set; }
    public DbSet<BudgetRequestModel> BudgetRequest { get; set; }
    public DbSet<ClubMembershipModel> ClubMembership { get; set; }
    public DbSet<ClubModel> Club { get; set; }
    public DbSet<DocumentModel> Documents { get; set; }
    public DbSet<EventModel> Event { get; set; }
    public DbSet<EventParticipantModel> EventParticipant { get; set; }
    public DbSet<ClubExpenceModel> ClubExpenses { get; set; }
    public DbSet<AnnouncementModel> Announcement { get; set; }
    public DbSet<RefreshTokenModel> RefreshTokens { get; set; }
    public DbSet<CategoryModel> Categories { get; set; }
    public DbSet<DepartmentModel> Departments { get; set; }
    public DbSet<NotificationModel> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClubModel>()
            .HasOne(c => c.Advisor)
            .WithMany()
            .HasForeignKey(c => c.AdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<RefreshTokenModel>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<BudgetRequestModel>()
            .Property(b => b.RequestedAmount)
            .HasPrecision(18, 2);

        // Seed data has been removed, departments will be created manually through the UI
    }
}
