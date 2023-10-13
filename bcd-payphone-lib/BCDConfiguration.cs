using System.ComponentModel.DataAnnotations;
using BCD.Payphone.Lib.Exceptions;
using Microsoft.Extensions.Configuration;

namespace BCD.Payphone.Lib
{
    public class BCDConfiguration
    {
        public BCDConfiguration()
        {
            Database = new DatabaseConfiguration();
            Twilio = new TwilioConfiguration();
            Spotify = new SpotifyConfiguration();
            AWS = new AwsConfiguration();
            Hubitat = new HubitatConfiguration();
            Characters = new List<string>();
            SurpriseNumbers = new List<string>();
        }

        public void BindConfigToObject(ConfigurationManager conf)
        {
            if (!string.IsNullOrWhiteSpace(conf["Characters"]))
            {
                var sanitized = conf["Characters"]!.Replace(" ", "");
                Characters = sanitized.Split(',').ToList();
            }

            if (!string.IsNullOrWhiteSpace(conf["SurpriseNumbers"]))
            {
                var sanitized = conf["SurpriseNumbers"]!.Replace(" ", "");
                SurpriseNumbers = sanitized.Split(',').ToList();
            }

            if (!string.IsNullOrWhiteSpace(conf["Hubitat:DeviceIds"]))
            {
                var sanitized = conf["Hubitat:DeviceIds"]!.Replace(" ", "");
                Hubitat.DeviceIds = sanitized.Split(',').ToList();
            }
        }

        [Required]
        public DatabaseConfiguration? Database { get; set; }

        [Required]
        public TwilioConfiguration? Twilio { get; set; }

        [Required]
        public SpotifyConfiguration? Spotify { get; set; }

        [Required]
        public AwsConfiguration? AWS { get; set; }

        [Required]
        public HubitatConfiguration Hubitat { get; set; }

        public bool DisableAuth { get; set; }

        [Required]
        public Guid GlobalSettingsGuid { get; set; }

        [Required]
        public List<string>? Characters { get; set; }

        [Required]
        public List<string>? SurpriseNumbers { get; set; }

        public void Validate()
        {
            var validationContext = new ValidationContext(this);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
            {
                throw new ConfigurationValidationException(validationResults);
            }
        }
    }

    public class HubitatConfiguration
    {
        [Required]
        public string? Host { get; set; }

        [Required]
        public List<string>? DeviceIds { get; set; }

        [Required]
        public string? AuthToken { get; set; }

    }

    public class SpotifyConfiguration
    {
        [Required]
        public string? ClientId { get; set; }

        [Required]
        public string? ClientSecret { get; set; }

        [Required]
        public string? RedirectUrl { get; set; }

        [Required]
        public string? RefreshToken { get; set; }
    }

    public class TwilioConfiguration
    {
        [Required]
        public string? AuthToken { get; set; }

        [Required]
        public string? AccountSid { get; set; }

        [Required]
        public string? AccessCode { get; set; }
    }

    public class DatabaseConfiguration
    {
        [Required]
        public DatabaseType DatabaseType { get; set; }

        [Required]
        public string? Server { get; set; }

        [Required]
        public int Port { get; set; }

        [Required]
        public string? Database { get; set; }

        [Required]
        public string? Username { get; set; }

        [Required]
        public string? Password { get; set; }
    }

    public class AwsConfiguration
    {
        [Required]
        public string? AccessKey { get; set; }

        [Required]
        public string? SecretKey { get; set; }

        [Required]
        public string? BucketName { get; set; }

        [Required]
        public string? Region { get; set; }
    }

    public enum DatabaseType
    {
        Mongo
    }
}

