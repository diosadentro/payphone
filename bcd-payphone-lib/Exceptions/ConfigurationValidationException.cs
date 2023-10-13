using System.ComponentModel.DataAnnotations;

namespace BCD.Payphone.Lib.Exceptions
{
    public class ConfigurationValidationException : Exception
    {
        public List<ValidationResult> ValidationErrors { get; set; }

        public ConfigurationValidationException(List<ValidationResult> validationErrors) : base("Invalid Configuration")
        {
            ValidationErrors = validationErrors;
        }
    }
}

