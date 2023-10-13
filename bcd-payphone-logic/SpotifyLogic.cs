using System.Text;
using BCD.Payphone.Lib;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using SpotifyAPI.Web;

namespace BCD.Payphone.Logic
{
	public class SpotifyLogic: ISpotifyLogic
    {
		private BCDConfiguration Configuration { get; set; }
        private readonly IMemoryCache MemoryCache;

        public SpotifyLogic(BCDConfiguration configuration, IMemoryCache memoryCache)
		{
			Configuration = configuration;
            MemoryCache = memoryCache;
        }

		public async Task<bool> SetCredential(string code, bool? refresh = false)
		{
            try
            {
                var nvc = new List<KeyValuePair<string, string>>();

                if(refresh == true)
                {
                    nvc.Add(new KeyValuePair<string, string>("refresh_token", code));
                    nvc.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
                }
                else
                {
                    nvc.Add(new KeyValuePair<string, string>("redirect_uri", Configuration.Spotify!.RedirectUrl!));
                    nvc.Add(new KeyValuePair<string, string>("code", code));
                    nvc.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
                }

                using var client = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token") { Content = new FormUrlEncodedContent(nvc) };
                var bytes = Encoding.UTF8.GetBytes($"{Configuration.Spotify!.ClientId}:{Configuration.Spotify.ClientSecret}");
                var base64 = Convert.ToBase64String(bytes);
                req.Headers.Add("Authorization", $"Basic {base64}");
                using (var res = await client.SendAsync(req))
                {
                    if (res.IsSuccessStatusCode)
                    {
                        var content = await res.Content.ReadAsStringAsync();
                        var loginResult = JsonConvert.DeserializeObject<SpotifyLoginResult>(content);

                        if(loginResult == null)
                        {
                            throw new Exception("Failed to deserialize login result from spotify");
                        }

                        // Set expiration with 1 minute grace period
                        loginResult.ExpirationTime = DateTime.UtcNow.AddSeconds(loginResult.expires_in - 60);
                        MemoryCache.Set("spotify-cred", loginResult);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> RefreshCredential()
        {
            if(MemoryCache.TryGetValue<SpotifyLoginResult>("spotify-cred", out var token))
            {
                if(token != null && !string.IsNullOrWhiteSpace(token.refresh_token))
                    return await SetCredential(token.refresh_token, true);
            }
            return false;
        }

        public SpotifyLoginResult? GetToken()
        {
            if (MemoryCache.TryGetValue<SpotifyLoginResult>("spotify-cred", out var token))
            {
                if(token != null)
                    return token;
            }
            return null;
        }

		public async Task<List<Track>> GetTrackSearchResults(string song, bool popularitySort)
		{
			var client = await GetClient();
			var searchResults = await client.Search.Item(new SearchRequest(SearchRequest.Types.Track, song));

            if(searchResults?.Tracks?.Items == null)
            {
                return new List<Track>();
            }

            if (popularitySort)
            {
                var sortedResults = searchResults.Tracks.Items.OrderByDescending(x => x.Popularity);
                return ConvertSearchResultsToDictionary(sortedResults);
            }
            else
            {
                return ConvertSearchResultsToDictionary(searchResults.Tracks.Items);
            }
		}

        public async Task<Lib.QueueResponse> AddSongToQueue(Track track)
        {
            var client = await GetClient();
            var result = await client.Player.AddToQueue(new PlayerAddToQueueRequest($"spotify:track:{track.TrackId}"));
            if(result)
            {
                var queueList = await client.Player.GetQueue();

                // Reverse the queue since the latest position will be at the end
                queueList.Queue.Reverse();
                var position = queueList.Queue.Count;
                var queueResponse = new Lib.QueueResponse();
                foreach(var item in queueList.Queue)
                {
                    if (item.Type == ItemType.Track)
                    {
                        var queuedTrack = item as FullTrack;
                        if(queuedTrack != null && queuedTrack.Id == track.TrackId)
                        {
                            queueResponse.Position = position;
                            queueResponse.Success = true;
                            break;
                        }
                        position--;
                    }
                }

                if(queueResponse.Success)
                {
                    int msUntilPlay = 0;
                    var upperBound = queueList.Queue.Count - position + 1;
                    for (var i=queueList.Queue.Count-1; i >= upperBound; i--)
                    {
                        var item = queueList.Queue[i];
                        if (item.Type == ItemType.Track)
                        {
                            var queuedTrack = item as FullTrack;
                            if(queuedTrack != null)
                                msUntilPlay += queuedTrack.DurationMs;
                        }
                    }

                    if(msUntilPlay > 60000)
                    {
                        int minutes = (int)Math.Round((double)msUntilPlay / 60000);
                        queueResponse.MinutesUntilPlay = minutes;
                    }
                    else if(msUntilPlay > 0)
                    {
                        queueResponse.MinutesUntilPlay = 0;
                    }
                    return queueResponse;
                }
                return new Lib.QueueResponse(true, null);
            }
            else
            {
                return new Lib.QueueResponse(false, null);
            }
        }

        private async Task<SpotifyClient> GetClient()
        {
            var config = SpotifyClientConfig.CreateDefault();

            if(!string.IsNullOrWhiteSpace(Configuration.Spotify!.RefreshToken))
            {
                await SetCredential(Configuration.Spotify.RefreshToken, true);
            }

            var token = GetToken();
            if(token == null)
            {
                throw new Exception("Failed to obtain spotify token");
            }

            if (DateTime.UtcNow >= token.ExpirationTime)
            {
                await RefreshCredential();
            }

            var spotify = new SpotifyClient(config.WithToken(token.access_token!));
            return spotify;
        }

        private List<Track> ConvertSearchResultsToDictionary(IEnumerable<FullTrack> searchResults)
		{
			var results = new List<Track>();
			foreach(var result in searchResults)
			{
				var track = new Track();
				track.TrackName = result.Name;
				track.Artist = string.Join(",", result.Artists.Select(x => x.Name).ToList());
				track.TrackId = result.Id;
				track.DisplayName = $"{track.TrackName} by {track.Artist}";
                results.Add(track);
            }
			return results;
		}
	}
}

