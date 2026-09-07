using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AppAgent> AppAgents => Set<AppAgent>();
    public DbSet<AppAgentRole> AppAgentRoles => Set<AppAgentRole>();
    public DbSet<AppSystem> AppSystems => Set<AppSystem>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<FeedbackAttachment> FeedbackAttachments => Set<FeedbackAttachment>();
    public DbSet<FeedbackMessage> FeedbackMessages => Set<FeedbackMessage>();
    public DbSet<FeedbackStatus> FeedbackStatuses => Set<FeedbackStatus>();
    public DbSet<FeedbackStatusHistory> FeedbackStatusHistories => Set<FeedbackStatusHistory>();
    public DbSet<FeedbackTag> FeedbackTags => Set<FeedbackTag>();
    public DbSet<FeedbackType> FeedbackTypes => Set<FeedbackType>();
    public DbSet<FeedbackVote> FeedbackVotes => Set<FeedbackVote>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Delete-behavior convention used below: Restrict for FKs into lookup tables
        // (FeedbackType/FeedbackStatus/Role/Tag/AppSystem) and into User, since cascading
        // a User delete through Feedback/messages/votes/history would destroy content and
        // audit trails; Cascade only for genuine parent -> owned-child rows (a Feedback's
        // messages/votes/history/tags, a message's attachments, and app/agent/role links).

        modelBuilder.Entity<User>(user =>
        {
            // Username + PasswordHash live in refers.m_user.
            user.ToTable("m_user", "refers");
            user.Property(u => u.Username).HasColumnName("UserName");
            user.Property(u => u.PasswordHash).HasColumnName("PW");
            user.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Role>(role =>
        {
            role.HasIndex(r => r.Code).IsUnique();
        });

        modelBuilder.Entity<Tag>(tag =>
        {
            tag.HasIndex(t => t.Code).IsUnique();
        });

        modelBuilder.Entity<FeedbackType>(type =>
        {
            type.HasIndex(t => t.Code).IsUnique();
        });

        modelBuilder.Entity<FeedbackStatus>(status =>
        {
            status.HasIndex(s => s.Code).IsUnique();
        });

        // Table names below override the snake-cased DbSet name (plural by default)
        // where the dbml uses a different name (singular, or a rename entirely).
        modelBuilder.Entity<AppSystem>(system =>
        {
            system.ToTable("apps");
        });

        modelBuilder.Entity<Agent>(agent =>
        {
            agent.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppAgent>(appAgent =>
        {
            appAgent.HasOne(aa => aa.App)
                .WithMany()
                .HasForeignKey(aa => aa.AppId)
                .OnDelete(DeleteBehavior.Cascade);

            appAgent.HasOne(aa => aa.Agent)
                .WithMany()
                .HasForeignKey(aa => aa.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppAgentRole>(appAgentRole =>
        {
            appAgentRole.HasOne(r => r.AppAgent)
                .WithMany()
                .HasForeignKey(r => r.AppAgentId)
                .OnDelete(DeleteBehavior.Cascade);

            appAgentRole.HasOne(r => r.Role)
                .WithMany()
                .HasForeignKey(r => r.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Feedback>(feedback =>
        {
            // dbml table is "feedback" (singular), not the pluralized DbSet-derived name.
            feedback.ToTable("feedback");

            // Two distinct FKs to User on the same entity - convention alone can't tell
            // AssigneeId and UserId apart, so each needs an explicit HasForeignKey.
            feedback.HasOne(f => f.Assignee)
                .WithMany()
                .HasForeignKey(f => f.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            feedback.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            feedback.HasOne(f => f.Type)
                .WithMany()
                .HasForeignKey(f => f.TypeId)
                .OnDelete(DeleteBehavior.Restrict);

            feedback.HasOne(f => f.Status)
                .WithMany()
                .HasForeignKey(f => f.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            feedback.HasOne(f => f.System)
                .WithMany()
                .HasForeignKey(f => f.SystemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeedbackMessage>(message =>
        {
            message.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            message.HasOne(m => m.Feedback)
                .WithMany()
                .HasForeignKey(m => m.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FeedbackAttachment>(attachment =>
        {
            attachment.HasOne(a => a.Message)
                .WithMany()
                .HasForeignKey(a => a.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FeedbackVote>(vote =>
        {
            vote.HasOne(v => v.Feedback)
                .WithMany()
                .HasForeignKey(v => v.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            vote.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            vote.HasIndex(v => new { v.FeedbackId, v.UserId }).IsUnique();
        });

        modelBuilder.Entity<FeedbackStatusHistory>(history =>
        {
            // dbml table is "feedback_status_history" (singular), not the pluralized
            // DbSet-derived name; changer_id is named changed_by in the dbml.
            history.ToTable("feedback_status_history");
            history.Property(h => h.ChangerId).HasColumnName("changed_by");

            history.HasOne(h => h.Feedback)
                .WithMany()
                .HasForeignKey(h => h.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            history.HasOne(h => h.Status)
                .WithMany()
                .HasForeignKey(h => h.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            history.HasOne(h => h.Changer)
                .WithMany()
                .HasForeignKey(h => h.ChangerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeedbackTag>(feedbackTag =>
        {
            feedbackTag.HasOne(t => t.Feedback)
                .WithMany()
                .HasForeignKey(t => t.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            feedbackTag.HasOne(t => t.Tag)
                .WithMany()
                .HasForeignKey(t => t.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(token =>
        {
            // Bounded length so MySql can index the column the token lookup filters on.
            token.Property(t => t.Token).HasMaxLength(255);
            token.HasIndex(t => t.Token).IsUnique();

            token.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            token.HasOne(t => t.ReplacedByToken)
                .WithOne()
                .HasForeignKey<RefreshToken>(t => t.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
