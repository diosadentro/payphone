using BCD.Payphone.Lib.Entities;

namespace BCD.Payphone.Lib
{
	public class CallState : BaseEntity
    {
		public string? Character { get; set; }
		public string? CallSid { get; set; }
		public List<Track>? SongRequestCache { get; set; }


		public CallState()
		{
		}
	}
}

