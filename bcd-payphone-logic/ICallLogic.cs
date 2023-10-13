using Twilio.AspNet.Core;

namespace BCD.Payphone.Logic
{
	public interface ICallLogic
	{
        Task<TwiMLResult> InitializeCall(Dictionary<string, string> parameters);
        Task<TwiMLResult> ProcessCommand(Dictionary<string, string> parameters);
        Task<TwiMLResult> ProcessSongRequest(Dictionary<string, string> parameters);
        Task<TwiMLResult> ProcessSongSelection(Dictionary<string, string> parameters);
        Task<TwiMLResult> ProcessRecordingFinished(Dictionary<string, string> parameters);
        Task<TwiMLResult> ProcessRecordingCommand(Dictionary<string, string> parameters, string recordingId);
        Task<TwiMLResult> ProcessAccessCode(Dictionary<string, string> parameters);
        Task<TwiMLResult> ProcessDialNumber(Dictionary<string, string> parameters);
    }
}

