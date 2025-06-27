using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;

namespace Government.Data
{
    public class AppDbContext : IdentityDbContext<AppUser,AppRole, string>
    {
    

        public AppDbContext(DbContextOptions<AppDbContext> options ) : base(options)
        {
            
        }

        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceImage> ServiceImages { get; set; }
        public DbSet<ServiceData> ServicesData { get; set; }
        public DbSet<Field> Fields { get; set; }
        public DbSet<ServiceField> ServicesField { get; set; }
        public DbSet<RequiredDocument> RequiredDocuments { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<AttachedDocument> AttachedDocuments { get; set; }
        public DbSet<AdminResponse> AdminsResponse { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Member> Members { get; set; }

        public DbSet<OtpEntry> OtpEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Ensure this line is included to call the base implementation


            var cascadedfks = modelBuilder.Model.GetEntityTypes()
                             .SelectMany(x => x.GetForeignKeys())
                             .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade && !fk.IsOwnership);
            foreach (var fk in cascadedfks)
                fk.DeleteBehavior = DeleteBehavior.Restrict;

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly()); // Ensure Identity setup is applied
        }

        //public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //{
        //    var entries = ChangeTracker.Entries<Request>();
        //    var UserId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);


        //    foreach (var entry in entries)
        //    {

        //        if (entry.State == EntityState.Added)
        //        {
        //            entry.Property(x => x.UserId).CurrentValue = UserId!;
        //           // entry.Property(x => x.RequestDate).CurrentValue = DateTime.UtcNow;


        //        }

        //    }


        //    return base.SaveChangesAsync(cancellationToken);
        //}
    }
}
