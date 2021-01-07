using System;
using BalTelegramBot.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BalTelegramBot
{
    public partial class BalDbContext : DbContext
    {
        public BalDbContext()
        {
        }

        public BalDbContext(DbContextOptions<BalDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Classes> Classes { get; set; }
        public virtual DbSet<PasswordInfo> PasswordInfo { get; set; }
        public virtual DbSet<Pupils> Pupils { get; set; }
        public virtual DbSet<Teachers> Teachers { get; set; }
        public virtual DbSet<UserInfo> UserInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=volodich.database.windows.net;Database=BalDb;User Id=volodich;Password=Vovasnikitin12fc;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Classes>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.Property(e => e.Name)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .ValueGeneratedNever();
            });

            modelBuilder.Entity<PasswordInfo>(entity =>
            {
                entity.HasKey(e => e.Key);

                entity.Property(e => e.Key)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Pupils>(entity =>
            {
                entity.Property(e => e.Class)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.HasOne(d => d.Chat)
                    .WithMany(p => p.Pupils)
                    .HasForeignKey(d => d.ChatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Pupils__Classrom__4D94879B");
            });

            modelBuilder.Entity<Teachers>(entity =>
            {
                entity.Property(e => e.Class)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FullName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Subjects)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.Chat)
                    .WithMany(p => p.Teachers)
                    .HasForeignKey(d => d.ChatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Teachers__Class__5070F446");
            });

            modelBuilder.Entity<UserInfo>(entity =>
            {
                entity.HasKey(e => e.ChatId);

                entity.Property(e => e.ChatId).ValueGeneratedNever();

                entity.Property(e => e.NameTelegram)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.NameUser)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Phone)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.State)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TypeUser)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.SettingNotification)
                    .HasMaxLength(50)
                    .IsUnicode(false);

            });
        }
    }
}
