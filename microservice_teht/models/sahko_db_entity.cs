namespace microservice_teht.models
{
	public class sahko_entity
	{
		public Guid Id { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime UpdatedAt { get; set; } = DateTime.Now;

		public DateTime EndDate { get; set; }
		public DateTime StartDate { get; set; }
		public decimal Price { get; set; }

		public sahko_entity()
		{
		Id = Guid.NewGuid();
		}
	}
}
