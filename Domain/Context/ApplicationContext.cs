
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Domain.Context
{
    public class ApplicationContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Flag to suppress row-by-row audit logging during bulk operations (e.g., Excel imports).
        /// When true, individual entity audits are skipped and a single upload audit should be logged separately.
        /// </summary>
        public bool SuppressAuditForBulkOperation { get; set; } = false;

        public ApplicationContext(DbContextOptions<ApplicationContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationContext).Assembly);

            #region Table Relation          
           
            #endregion

        }

        #region DbSets

        #region MyRegion

        #endregion

        #endregion

        #region AuditLog

        #endregion
    }
}
