namespace BCD.Payphone.Lib
{
	public class QueueResponse
	{
		public bool Success { get; set; }
		public int? Position { get; set; }
		public int? MinutesUntilPlay { get; set; }

		public QueueResponse(bool success, int? position)
		{
			Success = success;
			Position = position;
		}

        public QueueResponse(bool success, int? position, int? minutesUntilPlay)
        {
            Success = success;
            Position = position;
            MinutesUntilPlay = minutesUntilPlay;
        }

		public QueueResponse()
		{

		}
    }
}

