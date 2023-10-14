using System.ComponentModel.DataAnnotations;
using System.Reflection;
using BCD.Payphone.Lib.Exceptions;
using Microsoft.Extensions.Configuration;

namespace BCD.Payphone.Lib
{

    public abstract class ConfigurationSection
    {
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

        public void Validate()
        {
            var validationContext = new ValidationContext(this);
            var validationResults = new List<ValidationResult>();

            // Validate this object
            if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
            {
                throw new ConfigurationValidationException(validationResults);
            }

            // Find all sub objects that extend the ConfigurationSection and validate them as well
            Type myType = this.GetType();
            PropertyInfo[] properties = myType.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (typeof(ConfigurationSection).IsAssignableFrom(property.PropertyType))
                {
                    if (property != null)
                    {
                        object propertyValue = property.GetValue(this);

                        var prop = propertyValue as ConfigurationSection;

                        if (prop != null)
                        {
                            (prop as ConfigurationSection).Validate();
                        }
                    }
                }
            }
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
        public DatabaseConfiguration Database { get; set; }

        [Required]
        public TwilioConfiguration Twilio { get; set; }

        [Required]
        public SpotifyConfiguration Spotify { get; set; }

        [Required]
        public AwsConfiguration AWS { get; set; }

        [Required]
        public HubitatConfiguration Hubitat { get; set; }

        public bool DisableAuth { get; set; }

        [Required]
        public Guid GlobalSettingsGuid { get; set; }

        [Required]
        public List<string> Characters { get; set; }

        [Required]
        public List<string> SurpriseNumbers { get; set; }
    }

    public class HubitatConfiguration : ConfigurationSection
    {
        [Required(AllowEmptyStrings = false)]

        public string Host { get; set; }

        [Required(AllowEmptyStrings = false)]
        public List<string> DeviceIds { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string AuthToken { get; set; }

        public HubitatConfiguration()
        {
            Host = "";
            DeviceIds = new List<string>();
            AuthToken = "";
        }

    }

    public class SpotifyConfiguration : ConfigurationSection
    {
        [Required(AllowEmptyStrings = false)]
        public string ClientId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string ClientSecret { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string RedirectUrl { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string RefreshToken { get; set; }

        public SpotifyConfiguration()
        {
            ClientId = "";
            ClientSecret = "";
            RedirectUrl = "";
            RefreshToken = "";
        }
    }

    public class TwilioConfiguration : ConfigurationSection
    {
        [Required(AllowEmptyStrings = false)]
        public string AuthToken { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string AccountSid { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string AccessCode { get; set; }

        public TwilioConfiguration()
        {
            AuthToken = "";
            AccountSid = "";
            AccessCode = "";
        }
    }

    public class DatabaseConfiguration : ConfigurationSection
    {
        [Required]
        public DatabaseType DatabaseType { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Server { get; set; }

        [Required]
        public int Port { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Database { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Username { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Password { get; set; }

        public DatabaseConfiguration()
        {
            DatabaseType = DatabaseType.Mongo;
            Server = "";
            Port = 27017;
            Database = "";
            Username = "";
            Password = "";
        }
    }

    public class AwsConfiguration : ConfigurationSection
    {
        [Required(AllowEmptyStrings = false)]
        public string AccessKey { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string SecretKey { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string BucketName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Region { get; set; }

        public AwsConfiguration()
        {
            AccessKey = "";
            SecretKey = "";
            BucketName = "";
            Region = "";
        }
    }

    public enum DatabaseType
    {
        Mongo
    }
}

