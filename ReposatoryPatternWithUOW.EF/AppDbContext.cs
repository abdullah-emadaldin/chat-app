using Microsoft.EntityFrameworkCore;
using ReposatoryPatternWithUOW.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.EF
{
    public class AppDbContext:DbContext
    {
        
        public DbSet<User> Users { get; set; }
        public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<IdentityTokenVerification> IdentityTokenVerifications { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }
        public DbSet<Chat> Chat { get; set; }
        public DbSet<Message> Messages { get; set; }







        public AppDbContext(DbContextOptions options) : base(options) { }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<RefreshToken>(x =>{
                x.HasKey(x => new { x.UserId, x.Token });
                x.HasOne(x=>x.User).WithMany(x=>x.RefreshTokens).HasForeignKey(x=>x.UserId);
                x.Property(w => w.Token).HasColumnType("varchar").HasMaxLength(44);
            });
            modelBuilder.Entity<EmailVerificationCode>(x =>
            {
                x.HasKey(x => new { x.UserId, x.Code });
                x.Property(w => w.Code).HasMaxLength(10).HasColumnType("varchar");
            });

            modelBuilder.Entity<User>(x =>
            {
                x.HasMany(e => e.UserConnections).WithOne(e => e.User).HasForeignKey(f => f.UserId);
                x.HasMany(e => e.RefreshTokens).WithOne(e => e.User).HasForeignKey(f => f.UserId);
                x.HasMany(e => e.Chat).WithMany(e => e.Users).UsingEntity<UserChat>();
                x.Property(p => p.FirstName).HasMaxLength(100);
                x.Property(p => p.LastName).HasMaxLength(100);
                x.Property(p => p.Email).HasMaxLength(100).IsUnicode(false);
                x.Property(p => p.Password).HasMaxLength(100);
                x.HasIndex(p => p.Email);
                x.HasMany(x => x.SentRequests).WithOne(x => x.Sender).HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.ClientCascade);
                x.HasMany(x => x.RecievedRequests).WithOne(x => x.Recipient).HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.ClientCascade);
                x.HasMany(x => x.SentMessages).WithOne(x => x.Sender).HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.ClientCascade);
                x.HasMany(x => x.ReceivedMessages).WithOne(x => x.Receiver).HasForeignKey(x => x.ReceiverId).OnDelete(DeleteBehavior.ClientCascade);


            });

            modelBuilder.Entity<FriendRequest>(x =>
            {
                x.HasKey(x => new { x.SenderId, x.RecipientId });
            });

            modelBuilder.Entity<UserConnection>(x =>
            {
                x.Property(p => p.ConnectionId).HasMaxLength(255);
                x.HasKey(k => new { k.UserId, k.ConnectionId });
            });


            modelBuilder.Entity<Chat>(x =>
            {
                //x.Property(x => x.Id).HasMaxLength(255);
                x.HasMany(x => x.Messages).WithOne(x => x.Chat).HasForeignKey(x => x.ChatId);

            });

            modelBuilder.Entity<IdentityTokenVerification>(x =>
            {

                x.HasOne(p => p.User).WithOne(p => p.IdentityTokenVerification).HasForeignKey<IdentityTokenVerification>(p => p.UserId);
                x.Property(p => p.Token).HasMaxLength(44).IsUnicode(false);
                x.HasKey(k => new { k.UserId, k.Token });
            });

            modelBuilder.Entity<Message>(x =>
            {
                x.Property(x => x.IsRead).HasDefaultValue(false);
                x.Property(x => x.MessageText).HasMaxLength(2000);
            });
        }
    }
}
