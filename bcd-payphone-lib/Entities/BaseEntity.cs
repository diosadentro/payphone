namespace BCD.Payphone.Lib.Entities
{
	public abstract class BaseEntity
	{
        public Guid Id { get; set; }
        public string? DataType { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? OwnerId { get; set; }
        public Dictionary<string, string>? Tags { get; set; }

        public BaseEntity()
		{
            DataType = GetType().Name;
            Id = Guid.NewGuid();
            CreatedDate = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }
	}
}

