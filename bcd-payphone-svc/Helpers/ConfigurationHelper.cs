using BCD.Payphone.Lib;

namespace BCD.Payphone.Svc
{
    public static class ConfigurationHelper
    {
        public static BCDConfiguration BindDuallyNoteConfiguration(this WebApplicationBuilder builder)
        {
            var configuration = new BCDConfiguration();
            var config = builder.Configuration;
            config.AddJsonFile("env.json", true);
            config.Bind(configuration);
            configuration.BindConfigToObject(config);
            builder.Services.AddSingleton(configuration);
            configuration.Validate();
            return configuration;
        }
    }
}

