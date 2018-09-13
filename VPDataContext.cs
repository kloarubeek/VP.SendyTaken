using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VerwijderWKUsers.Models;

namespace VerwijderWKUsers
{
    public class VPDataContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public DbSet<User> Users { get; set; }
        public DbSet<EmailLoginToken> EmailLoginTokens { get; set; }

        public VPDataContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmailLoginToken>()
                .HasKey(e => new { Email = e.Emailadres, e.WebsiteId });
            modelBuilder.Entity<User>()
                .HasOne(x => x.EmailLoginToken)
                .WithMany()
                .HasForeignKey(x => new { x.Email, x.WebsiteId });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));
        }
    }
}
