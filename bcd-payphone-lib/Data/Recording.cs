using BCD.Payphone.Lib.Entities;

namespace BCD.Payphone.Lib
{
	public class Recording : BaseEntity
    {
		public Recording()
		{
		}

		public string? Url { get; set; }
		public string? CallSid { get; set; }
		public bool IsPublished { get; set; }
	}
}

