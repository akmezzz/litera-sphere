using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TutorPlatform.Models;

namespace TutorPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<StudentGroup> StudentGroups { get; set; }
        public DbSet<StudentGroupMember> StudentGroupMembers { get; set; }
        public DbSet<LearningTest> LearningTests { get; set; }
        public DbSet<LearningTestQuestion> LearningTestQuestions { get; set; }
        public DbSet<TestAssignment> TestAssignments { get; set; }
        public DbSet<StudentSubmission> StudentSubmissions { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        public DbSet<StudyTip> StudyTips { get; set; }
        public DbSet<DailyMeme> DailyMemes { get; set; }
        public DbSet<StudentMemeLike> StudentMemeLikes { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LessonAssignment> LessonAssignments { get; set; }
        public DbSet<StudentLessonProgress> StudentLessonProgresses { get; set; }
        public DbSet<StudentQuestion> StudentQuestions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<StudentGroupMember>()
                .HasKey(member => new { member.StudentGroupId, member.StudentId });

            builder.Entity<StudentGroupMember>()
                .HasOne(member => member.StudentGroup)
                .WithMany(group => group.Members)
                .HasForeignKey(member => member.StudentGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentGroupMember>()
                .HasOne(member => member.Student)
                .WithMany()
                .HasForeignKey(member => member.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TestAssignment>()
                .HasIndex(assignment => new { assignment.LearningTestId, assignment.StudentGroupId })
                .IsUnique();

            builder.Entity<StudentSubmission>()
                .HasIndex(submission => new { submission.LearningTestId, submission.StudentId })
                .IsUnique();

            builder.Entity<StudentMemeLike>()
                .HasIndex(item => new { item.DailyMemeId, item.StudentId })
                .IsUnique();

            builder.Entity<LessonAssignment>()
                .HasIndex(item => new { item.LessonId, item.StudentGroupId })
                .IsUnique();

            builder.Entity<StudentLessonProgress>()
                .HasIndex(item => new { item.LessonAssignmentId, item.StudentId })
                .IsUnique();

            builder.Entity<LearningTestQuestion>()
                .Property(question => question.MaxPoints)
                .HasColumnType("decimal(9,2)");

            builder.Entity<StudentSubmission>()
                .Property(submission => submission.AutoScore)
                .HasColumnType("decimal(9,2)");

            builder.Entity<StudentSubmission>()
                .Property(submission => submission.MaxScore)
                .HasColumnType("decimal(9,2)");

            builder.Entity<StudentSubmission>()
                .Property(submission => submission.TutorScore)
                .HasColumnType("decimal(9,2)");

            builder.Entity<StudentSubmission>()
                .Property(submission => submission.TutorFeedback)
                .HasMaxLength(2000);

            builder.Entity<StudentAnswer>()
                .Property(answer => answer.AwardedPoints)
                .HasColumnType("decimal(9,2)");

            builder.Entity<LearningTest>()
                .Property(test => test.ExamType)
                .HasMaxLength(32);

            builder.Entity<LearningTest>()
                .Property(test => test.MechanicType)
                .HasMaxLength(64);

            builder.Entity<LearningTestQuestion>()
                .Property(question => question.QuestionType)
                .HasMaxLength(32);

            builder.Entity<ApplicationUser>()
                .Property(user => user.PlatformRole)
                .HasMaxLength(24);
        }
    }
}
