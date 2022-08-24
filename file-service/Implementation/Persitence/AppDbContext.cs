using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using AIQXFileService.Domain.Models;
using AIQXFileService.Implementation.Services;
using AIQXCommon.Models;

namespace AIQXFileService.Implementation.Persistence
{
    public class AppDbContext : DbContext
    {

        private readonly ConfigService _config;
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<RefIdEntity> RefIds { get; set; }
        public DbSet<TagEntity> Tags { get; set; }


        public AppDbContext(ConfigService config)
        {
            _config = config;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer(
                _config.SqlConnStr(),
                x => x.MigrationsHistoryTable(_config.TablePrefix() + "__EFMigrationsHistory")
            );

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(_config.TablePrefix() + entity.GetTableName().ToLower());
            }
        }

                public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return (await base.SaveChangesAsync(cancellationToken));
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            var utcNow = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.Entity is UpdatedAtModel trackable)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            trackable.UpdatedAt = utcNow;
                            entry.Property("UpdatedAt").IsModified = true;
                            break;

                        case EntityState.Added:
                            trackable.UpdatedAt = utcNow;
                            break;
                    }
                }
            }
        }
    }
}
