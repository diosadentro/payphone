using BCD.Payphone.Lib.Entities;

namespace BCD.Payphone.Lib.Data
{
	public class Global : BaseEntity
	{
		public List<string>? AllowedNumbers { get; set; }
		public List<string>? SurpriseNumbers { get; set; }
		public bool SortSongsByPopularity { get; set; }
		public Global()
		{
		}
	}
}

