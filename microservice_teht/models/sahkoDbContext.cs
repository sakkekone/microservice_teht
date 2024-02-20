using Microsoft.EntityFrameworkCore;

namespace microservice_teht.models
{
	public class ElectricityDbContext:DbContext
	{
		public DbSet<sahko_entity> ElectricityPriceDatas { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
		optionsBuilder.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=sahko_db;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False");
		}

		public override int SaveChanges()
		{
		AddTimestamps();
		return base.SaveChanges();
		}

		public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
		AddTimestamps();
		return await base.SaveChangesAsync(cancellationToken);
		}

		private void AddTimestamps()
		{
		var entities = ChangeTracker.Entries()
				.Where(x => x.Entity is sahko_entity
				&& (x.State == EntityState.Modified));

		var now = DateTime.Now;

		foreach(var entity in entities)
		{
		var baseEntity = (sahko_entity)entity.Entity;

		baseEntity.UpdatedAt = now;
		}
		}

	}
}
