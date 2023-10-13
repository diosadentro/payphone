using System.Security.Claims;
using System.Text.Encodings.Web;
using BCD.Payphone.Lib;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Twilio.Security;

namespace BCD.Payphone.Api.Policies
{

    public interface ICustomAuthenticationManager
    {
        Task<bool> Authenticate(HttpContext httpContext, RequestValidator _requestValidator);
    }

    public class CustomAuthenticationManager : ICustomAuthenticationManager
    {
        public async Task<bool> Authenticate(HttpContext httpContext, RequestValidator _requestValidator)
        {
            var request = httpContext.Request;
            var scheme = request.Scheme;
            var forwardScheme = request.Headers["X-Forwarded-Proto"];

            if(!string.IsNullOrWhiteSpace(forwardScheme))
            {
                scheme = forwardScheme;
            }

            var requestUrl = $"{scheme}://{request.Host}{request.Path}{request.QueryString}";
            var parameters = new Dictionary<string, string>();

            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync(httpContext.RequestAborted).ConfigureAwait(false);
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }
            var signature = request.Headers["X-Twilio-Signature"].FirstOrDefault();
            var isValid = _requestValidator.Validate(requestUrl, parameters, signature);

            return isValid;
        }
    }

    public class TokenAuthenticationOptions : AuthenticationSchemeOptions
    {
    }

    public class TwilioAuthenticationHandler : AuthenticationHandler<TokenAuthenticationOptions>
    {
        IHttpContextAccessor _httpContextAccessor;
        private readonly RequestValidator _requestValidator;
        private BCDConfiguration Configuration { get; set; }
        private readonly ICustomAuthenticationManager CustomAuthenticationManager;

        public TwilioAuthenticationHandler(
            IOptionsMonitor<TokenAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ICustomAuthenticationManager customAuthenticationManager,
            IHttpContextAccessor httpContextAccessor,
            BCDConfiguration configuration)
            : base(options, logger, encoder, clock)
        {
            CustomAuthenticationManager = customAuthenticationManager;
            _httpContextAccessor = httpContextAccessor;
            var authToken = configuration.Twilio!.AuthToken ?? throw new Exception("'Twilio:AuthToken' not configured.");
            _requestValidator = new RequestValidator(authToken);
            Configuration = configuration;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(new List<Claim>(), Scheme.Name);
            var principal = new System.Security.Principal.GenericPrincipal(identity, null);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            if (Configuration.DisableAuth)
            {
                return AuthenticateResult.Success(ticket);
            }
            HttpContext? httpContext = _httpContextAccessor.HttpContext;

            if(httpContext == null)
            {
                return AuthenticateResult.Fail("Could not read request");
            }
            var isValid = await CustomAuthenticationManager.Authenticate(httpContext, _requestValidator);

            if (isValid)
            {
                return AuthenticateResult.Success(ticket);
            }
            return AuthenticateResult.Fail("Signature did not match");
        }
    }
}

