
using System.Linq.Expressions;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Domain.Context
{
    public class ApplicationContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        /// <summary>
        /// Flag to suppress row-by-row audit logging during bulk operations (e.g., Excel imports).
        /// When true, individual entity audits are skipped and a single upload audit should be logged separately.
        /// </summary>
        public bool SuppressAuditForBulkOperation { get; set; } = false;

        public ApplicationContext(
            DbContextOptions<ApplicationContext> options,
            IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().ToTable("ApplicationUsers");
            modelBuilder.Entity<ApplicationRole>().ToTable("ApplicationRoles");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("ApplicationUserClaims");
            modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("ApplicationUserLogins");
            modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("ApplicationUserTokens");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("ApplicationRoleClaims");
            modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("ApplicationUserRoles");

            modelBuilder.Entity<ApplicationUser>().HasQueryFilter(user => !user.IsDeleted);
            modelBuilder.Entity<ApplicationRole>().HasQueryFilter(role => !role.IsDeleted);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(entityType => typeof(Domain.Base.BaseEntity).IsAssignableFrom(entityType.ClrType)))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "entity");
                var isDeleted = Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    [typeof(bool)],
                    parameter,
                    Expression.Constant(nameof(Domain.Base.BaseEntity.IsDeleted)));
                var filter = Expression.Lambda(isDeleted, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationContext).Assembly);

            #region Table Relation          
           
            #endregion

        }

        #region DbSets

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ApplicationRole> ApplicationRoles { get; set; }
        public DbSet<IdentityUserClaim<Guid>> ApplicationUserClaims { get; set; }
        public DbSet<IdentityUserLogin<Guid>> ApplicationUserLogins { get; set; }
        public DbSet<IdentityUserToken<Guid>> ApplicationUserTokens { get; set; }
        public DbSet<IdentityRoleClaim<Guid>> ApplicationRoleClaims { get; set; }
        public DbSet<IdentityUserRole<Guid>> ApplicationUserRoles { get; set; }

        #endregion

        #region AuditLog

        #endregion

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyAuditValues();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override int SaveChanges()
        {
            return SaveChanges(acceptAllChangesOnSuccess: true);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            ApplyAuditValues();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
        }

        private void ApplyAuditValues()
        {
            var now = DateTime.UtcNow;
            var currentUserId = GetCurrentUserId();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                {
                    continue;
                }

                if (entry.Metadata.FindProperty("CreatedOn") is not null && entry.State == EntityState.Added)
                {
                    entry.Property("CreatedOn").CurrentValue = now;
                }

                if (entry.Metadata.FindProperty("CreatedBy") is not null &&
                    entry.State == EntityState.Added &&
                    currentUserId.HasValue)
                {
                    entry.Property("CreatedBy").CurrentValue = currentUserId.Value;
                }

                if (entry.Metadata.FindProperty("ModifiedOn") is not null && entry.State == EntityState.Modified)
                {
                    entry.Property("ModifiedOn").CurrentValue = now;
                }

                if (entry.Metadata.FindProperty("ModifiedBy") is not null &&
                    entry.State == EntityState.Modified &&
                    currentUserId.HasValue)
                {
                    entry.Property("ModifiedBy").CurrentValue = currentUserId.Value;
                }
            }
        }

        private Guid? GetCurrentUserId()
        {
            var value = _httpContextAccessor?.HttpContext?.User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
