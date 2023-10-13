using BCD.Payphone.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Core;
using Twilio.TwiML.Voice;

namespace BCD.Payphone.Api.Controllers
{
    [Authorize]
    [Route("")]
    public class CallController : TwilioController
    {
        private ICallLogic Logic { get; set; }
        private ISpotifyLogic SpotifyLogic { get; set; }
        private IHubitatLogic HubitatLogic { get; set; }

        public CallController(ICallLogic logic, ISpotifyLogic spotifyLogic, IHubitatLogic hubitatLogic)
        {
            Logic = logic;
            SpotifyLogic = spotifyLogic;
            HubitatLogic = hubitatLogic;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("health")]
        public bool GetHealth()
        {
            // Dumb, but good enough for this app
            return true;
        }

        [HttpGet]
        [Route("spotify")]
        public async Task<bool> SetSpotifyCredential(string code, string state)
        {
            return await SpotifyLogic.SetCredential(code);
        }

        [HttpPost]
        [Route("initialize")]
        public async Task<TwiMLResult> Initialize()
        {
            var parameters = new Dictionary<string, string>();
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }
            return await Logic.InitializeCall(parameters);
        }

        [HttpPost]
        [Route("process-command")]
        public async Task<TwiMLResult> ProcessCommand()
        {
            var request = Request;
            var parameters = new Dictionary<string, string>();
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }

            return await Logic.ProcessCommand(parameters);
        }

        [HttpPost]
        [Route("process-song-request")]
        public async Task<TwiMLResult> ProcessSongRequest()
        {
            var request = Request;
            var parameters = new Dictionary<string, string>();
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }
            return await Logic.ProcessSongRequest(parameters);
        }

        [HttpPost]
        [Route("process-song-selection")]
        public async Task<TwiMLResult> ProcessSongSelection()
        {
            var request = Request;
            var parameters = new Dictionary<string, string>();
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }
            return await Logic.ProcessSongSelection(parameters);
        }

        [HttpPost]
        [Route("process-recording-finished")]
        public async Task<TwiMLResult> ProcessRecordingFinished()
        {
            var request = Request;
            var parameters = new Dictionary<string, string>();
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }
            return await Logic.ProcessRecordingFinished(parameters);
        }

        [HttpPost]
        [Route("process-recording-command")]
        public async Task<TwiMLResult> ProcessRecordingCommand(string recordingId)
        {
            var request = Request;
            var parameters = new Dictionary<string, string>();
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }
            return await Logic.ProcessRecordingCommand(parameters, recordingId);
        }

        [HttpPost]
        [Route("process-access-code")]
        public async Task<TwiMLResult> ProcessAccessCode()
        {
            var request = Request;
            var parameters = new Dictionary<string, string>();
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }
            return await Logic.ProcessAccessCode(parameters);
        }

        [HttpPost]
        [Route("process-phone-number")]
        public async Task<TwiMLResult> ProcessPhoneNumber()
        {
            var request = Request;
            var parameters = new Dictionary<string, string>();
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }
            return await Logic.ProcessDialNumber(parameters);
        }

        [HttpPost]
        [Route("change-color")]
        public TwiMLResult ChangeLightColor()
        {
            HubitatLogic.EnqueueColorChangeTask();
            var say = new Say("I've changed the light color. Goodbye!");
            return new TwiMLResult(say);
        }
    }
}