﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CV_Filtation_System.Core.Entities
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Company> Companies { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        //public DbSet<CompanyJobPosting> CompanyJobPostings { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<JobApplication> JobApplication { get; set; }
        public DbSet<UserSkill> UserSkills { get; set; }
        public DbSet<UserCompany> UserCompanies { get; set; }
        public DbSet<UserFavoriteJob> UserFavoriteJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserSkill>()
                .HasKey(us => new { us.UserId, us.SkillId });

            modelBuilder.Entity<UserCompany>()
                .HasKey(uc => new { uc.UserId, uc.CompanyId });

            //modelBuilder.Entity<CompanyJobPosting>()
            //    .HasKey(cj => new { cj.CompanyId, cj.JobPostingId });

            modelBuilder.Entity<Company>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<JobPosting>()
                .HasOne(j => j.Company)
                .WithMany(c => c.JobPostings)
                .HasForeignKey(j => j.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFavoriteJob>()
                .HasKey(uf => new { uf.UserId, uf.JobPostingId });

            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.User)
                .WithMany(u => u.Applications)
                .HasForeignKey(ja => ja.UserId);

            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.JobPosting)
                .WithMany(jp => jp.Applications)
                .HasForeignKey(ja => ja.JobPostingId);
        }
    }
}
