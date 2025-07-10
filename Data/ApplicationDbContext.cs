using MedServices.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MedServices.Data
{
    public class ApplicationDbContext : IdentityDbContext<MedServicesUser>
    {
        public DbSet<MedServicesUser> Users { get; set; }
        
        protected ApplicationDbContext()
        {
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer(Startup.Startup.AppConfiguration["ConnectionStrings:DefaultConnection"]);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
		public override int SaveChanges(bool acceptAllChangesOnSuccess)
		{
			EntityState[] arrEntityStates = [ EntityState.Added, EntityState.Modified ];
			ChangeTracker.DetectChanges();
			IEnumerable<EntityEntry<MedServicesUser>> userEntities = ChangeTracker.Entries<MedServicesUser>()
				?.Where(curEntity => curEntity?.Entity is MedServicesUser && arrEntityStates.Contains(curEntity.State)
				&& !string.IsNullOrWhiteSpace(curEntity.CurrentValues["PhoneNumber"] as string));
			foreach(EntityEntry<MedServicesUser> curEntity in userEntities)
			{
				//curEntity.CurrentValues["NormalizedPhoneNumber"] = AccountViewModel.GetNormilizedPhoneNumber(curEntity.CurrentValues["PhoneNumber"] as string);
			}
			return base.SaveChanges(acceptAllChangesOnSuccess);
		}
		public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			return base.SaveChangesAsync(cancellationToken);
		}
    }
}
