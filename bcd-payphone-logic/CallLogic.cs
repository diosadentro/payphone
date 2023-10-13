using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using BCD.Payphone.Data;
using BCD.Payphone.Lib;
using BCD.Payphone.Lib.Data;
using MongoDB.Driver;
using Twilio.AspNet.Core;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace BCD.Payphone.Logic
{
	public class CallLogic : ICallLogic
    {

        #region Private Properties
        private BCDConfiguration Configuration { get; set; }
        private ISpotifyLogic Spotify;
        private IHubitatLogic Hubitat;
        private AmazonS3Client S3Client { get; set; }
        #endregion

        #region Protected Properties
        private readonly IMongoCollection<CallState>? _stateCollection;
        private readonly IMongoCollection<LastCharacter>? _characterCollection;
        private readonly IMongoCollection<Recording>? _recordingCollection;
        private readonly IMongoCollection<Global>? _globalCollection;
        private readonly IDatabaseClientFactory _databaseClientFactory;
        #endregion

        #region Public Constructor
        public CallLogic(BCDConfiguration configuration, ISpotifyLogic spotify, IHubitatLogic hubitat, IDatabaseClientFactory clientFactory)
		{
            Configuration = configuration;
            Hubitat = hubitat;
            Spotify = spotify;

            _databaseClientFactory = clientFactory;

            if (_databaseClientFactory == null)
            {
                throw new Exception("Failed to obtain database client, was it set properly in startup?");
            }

            _stateCollection = _databaseClientFactory.GetClient<CallState>();
            _characterCollection = _databaseClientFactory.GetClient<LastCharacter>();
            _recordingCollection = _databaseClientFactory.GetClient<Recording>();
            _globalCollection = _databaseClientFactory.GetClient<Global>();

            if (_stateCollection == null || _characterCollection == null || _recordingCollection == null || _globalCollection == null)
            {
                throw new Exception($"Failed to obtain state, character, recording, or global collection. Please check your database settings");
            }

            S3Client = new AmazonS3Client(Configuration.AWS!.AccessKey, Configuration.AWS!.SecretKey, RegionEndpoint.GetBySystemName(Configuration.AWS!.Region));
        }
        #endregion

        #region Public Methods
        public async Task<TwiMLResult> InitializeCall(Dictionary<string, string> parameters)
        {

            if(!await VerifyCaller(parameters))
            {
                return new TwiMLResult(new Hangup());
            }

            var state = await GetOrSetCallState(parameters);

            var response = new VoiceResponse();

            var gather = new Gather(
                numDigits: 1,
                action: new Uri("/process-command"),
                finishOnKey: "",
                input: new List<Gather.InputEnum>() { Gather.InputEnum.Dtmf},
                method: Twilio.Http.HttpMethod.Post,
                timeout: 10
            );

            var url = GetPresignedUrl(state.Character!, "intro");
            gather.Play(new Uri(url));

            response.Append(gather).Append(gather);
            return new TwiMLResult(response);
        }

        public async Task<TwiMLResult> ProcessCommand(Dictionary<string, string> parameters)
        {
            var response = new VoiceResponse();
            var state = await GetOrSetCallState(parameters);
            if (!parameters.ContainsKey("Digits"))
            {
                response.Append(GetVoiceResponse(state, "sorry"));
                response.Append(GoBackCharacter(state));
            }
            else
            {
                var digit = parameters["Digits"];
                if (string.IsNullOrWhiteSpace(digit) || !new List<string>() { "1", "2", "3", "4", "5", "6", "*" }.Contains(digit))
                {
                    response.Append(GetVoiceResponse(state, "sorry"));
                    response.Append(GoBackCharacter(state));
                }
                else
                {
                    switch (digit)
                    {
                        case "1":
                            PromptForSong(response, state);
                            break;
                        case "2":
                            Hubitat.EnqueueColorChangeTask();
                            response.Append(GetVoiceResponse(state, "lights"));
                            break;
                        case "3":
                            response.Append(GetVoiceResponse(state, "joke1"));
                            break;
                        case "4":
                            await GatherRecording(state, response);
                            break;
                        case "5":
                            response.Append(new Say("Please wait while we connect you to something special."));
                            response.Append(await GetRandomSurprirseNumber());
                            break;
                        case "6":
                            return await InitializeCall(parameters);
                        case "*":
                            GatherAcessCode(response);
                            break;
                    }
                }
            }
            return new TwiMLResult(response);
        }

        public async Task<TwiMLResult> ProcessSongRequest(Dictionary<string, string> parameters)
        {
            var state = await GetOrSetCallState(parameters);
            var globalSetting = await GetGlobalSettings();
            var response = new VoiceResponse();
            if (parameters.TryGetValue("SpeechResult", out var speachResult))
            {
                var songSearch = await Spotify.GetTrackSearchResults(speachResult, globalSetting.SortSongsByPopularity);
                if(songSearch.Count == 0)
                {
                    response.Append(GetVoiceResponse(state, "different-song"));
                    PromptForSong(response, state);
                }

                var songSelectionMessage = "";
                for (var i=0; i < songSearch.Count && i < 10; i++)
                {
                    songSelectionMessage += $"{i + 1}: {songSearch[i].DisplayName}.\n";
                }
                songSelectionMessage += "Press star to go back.";
                var gather = new Gather(
                    numDigits: 1,
                    action: new Uri("/process-song-selection"),
                    finishOnKey: "",
                    input: new List<Gather.InputEnum>() { Gather.InputEnum.Dtmf },
                    method: Twilio.Http.HttpMethod.Post,
                    timeout: 5
                );
                response.Append(GetVoiceResponse(state, "select-song"));
                gather.Say(songSelectionMessage);
                response.Append(gather);
                state.SongRequestCache = songSearch;
                await UpdateCallState(state);
            }
            else
            {
                response.Append(GetVoiceResponse(state, "sorry"));
                PromptForSong(response, state);
            }
            return new TwiMLResult(response);
        }

        public async Task<TwiMLResult> ProcessSongSelection(Dictionary<string, string> parameters)
        {
            var response = new VoiceResponse();
            var state = await GetOrSetCallState(parameters);
            if (!parameters.ContainsKey("Digits"))
            {
                response.Append(GetVoiceResponse(state, "sorry"));
                response.Append(GoBackCharacter(state));
            }
            else
            {
                if(state.SongRequestCache == null)
                {
                    throw new Exception("Failed to find song request cache");
                }

                if (int.TryParse(parameters["Digits"], out var digit))
                {
                    if(digit < 1 || digit > state.SongRequestCache.Count)
                    {
                        response.Append(GetVoiceResponse(state, "sorry"));
                        response.Append(GoBackCharacter(state));
                    }
                    var selectedSong = state.SongRequestCache[digit - 1];
                    try
                    {
                        var queueResponse = await Spotify.AddSongToQueue(selectedSong);
                        if (queueResponse.Success)
                        {
                            var positionString = queueResponse.Position != null ? $"Your position in the queue is {queueResponse.Position}": "";
                            var minutesUntilPlayString = "Your song will play soon.";

                            if (queueResponse.MinutesUntilPlay != null)
                            {
                                if(queueResponse.MinutesUntilPlay > 1)
                                {
                                    minutesUntilPlayString = $"Your song will play in about {queueResponse.MinutesUntilPlay} minutes";
                                }
                                else
                                {
                                    minutesUntilPlayString = $"Your song will play in less than 1 minute";
                                }
                            }

                            var say = new Say($"I've added {selectedSong.DisplayName} to the queue. {positionString}. {minutesUntilPlayString}!");
                            response.Append(say);
                            state.SongRequestCache = null;
                            await UpdateCallState(state);
                        }
                        else
                        {
                            response.Append(GetVoiceResponse(state, "sorry"));
                            response.Append(GoBackCharacter(state));
                        }
                    }
                    catch(Exception)
                    {
                        response.Append(new Say("Sorry, there isn't a queue to add to"));
                        response.Append(GoBackCharacter(state));
                    }
                }
                else
                {
                    response.Append(GetVoiceResponse(state, "sorry"));
                    response.Append(GoBackCharacter(state));
                }
            }
            return new TwiMLResult(response);
        }

        public async Task<TwiMLResult> ProcessRecordingFinished(Dictionary<string, string> parameters)
        {
            var response = new VoiceResponse();
            var state = await GetOrSetCallState(parameters);
            if (parameters.TryGetValue("RecordingUrl", out var recordingUrl))
            {
                var recording = await SaveRecording(recordingUrl, state.CallSid!);

                var play = new Play(new Uri(recordingUrl));
                var gather = new Gather(
                    numDigits: 1,
                    action: new Uri($"/process-recording-command?recordingId={recording.Id}"),
                    finishOnKey: "#",
                    input: new List<Gather.InputEnum>() { Gather.InputEnum.Dtmf },
                    method: Twilio.Http.HttpMethod.Post,
                    timeout: 10
                );

                gather.Append(GetVoiceResponse(state, "save-or-record"));

                response.Append(play).Append(gather);
                return new TwiMLResult(response);
            }
            else
            {
                response.Append(GetVoiceResponse(state, "sorry"));
                response.Append(GoBackCharacter(state));
            }

            return new TwiMLResult(response);
        }

        public async Task<TwiMLResult> ProcessRecordingCommand(Dictionary<string, string> parameters, string recordingId)
        {
            var response = new VoiceResponse();
            var state = await GetOrSetCallState(parameters);

            if(parameters.TryGetValue("Digits", out var digit))
            {
                switch(digit)
                {
                    case "9":
                        await _recordingCollection.DeleteOneAsync(x => x.Id == Guid.Parse(recordingId));
                        response.Append(GetVoiceResponse(state, "after-beep"));
                        response.Append(PromptForRecord());
                        break;
                    case "1":
                        await UpdateRecording(Guid.Parse(recordingId));
                        response.Append(GetVoiceResponse(state, "recording-saved"));
                        break;
                }
            }
            else
            {
                response.Append(GetVoiceResponse(state, "sorry"));
                response.Append(GoBackCharacter(state));
            }

            return new TwiMLResult(response);
        }

        public async Task<TwiMLResult> ProcessAccessCode(Dictionary<string, string> parameters)
        {
            var response = new VoiceResponse();
            var state = await GetOrSetCallState(parameters);
            var accessCode = Configuration.Twilio!.AccessCode;

            if (parameters.TryGetValue("Digits", out var digits) && digits == accessCode)
            {
                response.Append(GatherPhoneNumber());
            }
            return new TwiMLResult(response);
        }

        public async Task<TwiMLResult> ProcessDialNumber(Dictionary<string, string> parameters)
        {
            var response = new VoiceResponse();
            var state = await GetOrSetCallState(parameters);

            if (parameters.TryGetValue("Digits", out var digit))
            {
                response.Append(new Say($"Dialing. Please wait"));
                response.Append(new Dial(digit));
            }
            else
            {
                response.Append(new Say("A problem occurred"));
            }
            return new TwiMLResult(response);
        }

        #endregion

        #region Private Methods

        private async System.Threading.Tasks.Task UpdateCallState(CallState state)
        {
            await _stateCollection.ReplaceOneAsync(x => x.Id == state.Id, state);
        }

        private async Task<Recording> SaveRecording(string recordingUrl, string callSid)
        {
            var recording = new Recording();
            recording.Url = recordingUrl;
            recording.CallSid = callSid;
            recording.IsPublished = false;
            await _recordingCollection!.InsertOneAsync(recording);
            return recording;
        }

        private async System.Threading.Tasks.Task UpdateRecording(Guid recordingId)
        {
            var recording = await _recordingCollection.Find(x => x.Id == recordingId).FirstOrDefaultAsync();

            if(recording == null)
            {
                throw new Exception("Could not find recording");
            }

            recording.IsPublished = true;
            await _recordingCollection.ReplaceOneAsync(x => x.Id == recording.Id, recording);
        }

        private async System.Threading.Tasks.Task GatherRecording(CallState state, VoiceResponse response)
        {
            try
            {
                var recordings = await _recordingCollection.Find(x => x.CreatedDate > DateTime.UtcNow.AddDays(-1) && x.IsPublished).ToListAsync();
                if (recordings != null && recordings.Count > 0)
                {
                    var rand = new Random();
                    var recording = recordings[rand.Next(0, recordings.Count)];

                    if(recording.Url == null)
                    {
                        throw new Exception("Failed to find recoridng url");
                    }

                    response.Append(new Pause(1));
                    response.Append(new Say("A friend wants to tell you something..."));
                    response.Append(new Play(new Uri(recording.Url)));
                    response.Append(new Say("Now, wasn't that nice?"));
                    response.Append(new Pause(1));
                }
            }
            catch(Exception)
            {
                // Ignore
            }
            response.Append(GetVoiceResponse(state, "after-beep"));
            response.Append(PromptForRecord());
        }

        private void GatherAcessCode(VoiceResponse response)
        {
            var gather = new Gather(
                    numDigits: 20,
                    action: new Uri("/process-access-code"),
                    finishOnKey: "#",
                    input: new List<Gather.InputEnum>() { Gather.InputEnum.Dtmf },
                    method: Twilio.Http.HttpMethod.Post,
                    profanityFilter: false,
                    enhanced: true,
                    timeout: 5
                );
            gather.Say("Enter access code, then press pound.");
            response.Append(gather);
        }

        private Gather GatherPhoneNumber()
        {
            var gather = new Gather(
                    numDigits: 20,
                    action: new Uri("/process-phone-number"),
                    finishOnKey: "#",
                    input: new List<Gather.InputEnum>() { Gather.InputEnum.Dtmf },
                    method: Twilio.Http.HttpMethod.Post,
                    timeout: 5
                );
            gather.Say("Enter phone number to connect and press pound");
            return gather;
        }

        private void PromptForSong(VoiceResponse response, CallState state)
        {
            var gather = new Gather(
                    numDigits: 1,
                    action: new Uri("/process-song-request"),
                    finishOnKey: "#",
                    speechTimeout: "3",
                    input: new List<Gather.InputEnum>() { Gather.InputEnum.Speech },
                    method: Twilio.Http.HttpMethod.Post,
                    profanityFilter: false,
                    speechModel: "experimental_conversations",
                    timeout: 5
                );
            gather.Play(new Uri(GetPresignedUrl("general", "beep")));

            response.Append(GetVoiceResponse(state, "song-input"));
            response.Append(gather);
        }

        private Record PromptForRecord()
        {
            var record = new Record(
                action: new Uri("/process-recording-finished"),
                method: Twilio.Http.HttpMethod.Post,
                timeout: 5,
                finishOnKey: "#",
                maxLength: 15,
                playBeep: true,
                trim: Record.TrimEnum.TrimSilence,
                transcribe: false);
            return record;
        }

        private Gather GoBackCharacter(CallState state)
        {
            var gather = new Gather(
                    numDigits: 1,
                    action: new Uri("/process-command"),
                    finishOnKey: "",
                    input: new List<Gather.InputEnum>() { Gather.InputEnum.Dtmf },
                    method: Twilio.Http.HttpMethod.Post,
                    timeout: 10
                );
            gather.Append(GetVoiceResponse(state, "intro"));
            return gather;
        }

        private async Task<Dial> GetRandomSurprirseNumber()
        {
            var rand = new Random();
            var number = "";

            var globalData = await _globalCollection.Find(x => x.Id == Configuration.GlobalSettingsGuid).FirstOrDefaultAsync();
            if (globalData != null)
            {
                if (globalData.SurpriseNumbers!.Count > 0)
                {
                    number = globalData.SurpriseNumbers[rand.Next(0, globalData.SurpriseNumbers.Count)];
                }
            }

            // CYA - if the global doesn't contain the surprise numbers
            if(string.IsNullOrWhiteSpace(number) && Configuration.SurpriseNumbers != null)
            {
                number = Configuration.SurpriseNumbers[rand.Next(0, Configuration.SurpriseNumbers.Count)];
            }

            return new Dial(number, ringTone: Dial.RingToneEnum.Us);
        }

        public string GetPresignedUrl(string character, string phrase)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = Configuration.AWS!.BucketName,
                Key = $"{character}/{phrase}.wav",
                Expires = DateTime.UtcNow.AddMinutes(2)
            };

            return S3Client.GetPreSignedURL(request);
        }

        public async Task<Play> GetVoiceResponse(Dictionary<string, string> parameters, string phrase)
        {
            var state = await GetOrSetCallState(parameters);
            var url = GetPresignedUrl(state.Character!, phrase);
            return new Play(new Uri(url));
        }

        public Play GetVoiceResponse(CallState state, string phrase)
        {
            var url = GetPresignedUrl(state.Character!, phrase);
            return new Play(new Uri(url));
        }

        public async System.Threading.Tasks.Task SetLastCharacter(int index)
        {
            var lastCaller = await _characterCollection.Find(x => x.DataType == "LastCharacter").FirstOrDefaultAsync();
            if(lastCaller == null)
            {
                lastCaller = new LastCharacter();
                lastCaller.Index = (int) index;
                await _characterCollection!.InsertOneAsync(lastCaller);
            }
            else
            {
                lastCaller.Index = index;
                await _characterCollection.ReplaceOneAsync(x => x.Id == lastCaller.Id, lastCaller);
            }
        }

        public async Task<int> GetLastCharacter()
        {
            var lastCaller = await _characterCollection.Find(x => x.DataType == "LastCharacter").FirstOrDefaultAsync();
            if (lastCaller == null)
            {
                return 0;
            }
            else
            {
                return lastCaller.Index;
            }
        }

        private async Task<string> SetCharacterAsync()
        {
            var index = await GetLastCharacter();
            index++;

            if (index >= Configuration.Characters!.Count)
            {
                index = 0;
            }
            await SetLastCharacter(index);

            return Configuration.Characters[index];
        }

        private async Task<CallState> GetOrSetCallState(Dictionary<string, string> parameters)
        {
            var callSid = parameters.GetValueOrDefault("CallSid");

            var existingState = await _stateCollection.Find(x => x.CallSid == callSid).FirstOrDefaultAsync();

            if (existingState != null)
            {
                return existingState;
            }
            else
            {
                var newCallState = new CallState();
                newCallState.CallSid = parameters.GetValueOrDefault("CallSid");
                newCallState.Character = await SetCharacterAsync();
                await _stateCollection!.InsertOneAsync(newCallState);
                return newCallState;
            }
        }

        private async Task<Global> GetGlobalSettings()
        {
            var globalData = await _globalCollection.Find(x => x.Id == Configuration.GlobalSettingsGuid).FirstOrDefaultAsync();

            if(globalData == null)
            {
                globalData = new Global()
                {
                    Id = Configuration.GlobalSettingsGuid,
                    AllowedNumbers = new List<string>() { "*" },
                    SurpriseNumbers = Configuration.SurpriseNumbers,
                    SortSongsByPopularity = false
                };
                await _globalCollection!.InsertOneAsync(globalData);
            }

            return globalData;
        }

        private async Task<bool> VerifyCaller(Dictionary<string, string> parameters)
        {
            var fromNumber = parameters.GetValueOrDefault("From");
            var globalData = await GetGlobalSettings();

            if (fromNumber == null || !globalData.AllowedNumbers!.Contains(fromNumber) && !globalData.AllowedNumbers.Contains("*"))
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}