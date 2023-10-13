using BCD.Payphone.Lib;
using Hangfire;

namespace BCD.Payphone.Logic
{
	public class HubitatLogic : IHubitatLogic
	{
		private BCDConfiguration Configuration { get; set; }
        public List<ColorCode> AllowedColors = new List<ColorCode>()
        {
            new ColorCode(92, 100, 94),
            new ColorCode(72, 100, 94),
            new ColorCode(41, 100, 94),
            new ColorCode(5, 100, 94),
            new ColorCode(56, 100, 94),
            new ColorCode(79, 100, 94),
            new ColorCode(14, 100, 94),
            new ColorCode(0, 100, 94),
            new ColorCode(44, 50, 83)
        };

        public HubitatLogic(BCDConfiguration configuration)
		{
			Configuration = configuration;
        }

        public string EnqueueColorChangeTask()
        {
            var jobId = BackgroundJob.Enqueue(() => ChangeColor());
            return jobId;
        }

		public async Task ChangeColor()
		{
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                Random rand = new Random();
                var color = AllowedColors[rand.Next(0, AllowedColors.Count)];
                if (Configuration.Hubitat!.DeviceIds != null)
                {
                    foreach (var deviceId in Configuration.Hubitat!.DeviceIds)
                    {
                        try
                        {
                            await client.GetAsync($"http://{Configuration.Hubitat.Host}/apps/api/12/devices/{deviceId}/setColor/{{\"hue\":{color.Hue},\"saturation\":{color.Saturation},\"level\":{color.Level}}}?access_token={Configuration.Hubitat!.AuthToken}");
                        }
                        catch (Exception)
                        {
                            try
                            {
                                // Retry once
                                await client.GetAsync($"http://{Configuration.Hubitat.Host}/apps/api/12/devices/{deviceId}/setColor/{{\"hue\":{color.Hue},\"saturation\":{color.Saturation},\"level\":{color.Level}}}?access_token={Configuration.Hubitat!.AuthToken}");
                            }
                            catch (Exception)
                            {
                                // Ignore further errors
                            }
                        }
                    }
                }
            };
        }
	}

    public class ColorCode
    {
        public int Hue { get; set; }
        public int Saturation { get; set; }
        public int Level { get; set; }

        public ColorCode(int hue, int saturation, int level)
        {
            Hue = hue;
            Saturation = saturation;
            Level = level;
        }
    }
}

