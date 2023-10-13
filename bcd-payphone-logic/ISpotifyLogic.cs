using BCD.Payphone.Lib;

namespace BCD.Payphone.Logic
{
	public interface ISpotifyLogic
	{
        Task<List<Track>> GetTrackSearchResults(string song, bool popularitySort);
        Task<bool> SetCredential(string code, bool? refresh = false);
        Task<bool> RefreshCredential();
        Task<Lib.QueueResponse> AddSongToQueue(Track track);
    }
}

