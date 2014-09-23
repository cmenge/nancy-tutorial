using System;
using System.Configuration;
using CM.Base.Logging;

namespace CM.Base.Services
{
    public class UrlService
    {
        private static readonly ILogger _log = LogManager.GetLogger();
        private readonly Uri _baseUrl;

        public UrlService(SystemSettingsService settingsService)
        {
            string configuredBaseUrl = settingsService.Settings.BaseUrl;

            if (string.IsNullOrWhiteSpace(configuredBaseUrl))
                throw new ConfigurationErrorsException("Missing setting for 'BaseUrl'!");

            if (!Uri.IsWellFormedUriString(configuredBaseUrl, UriKind.Absolute))
                throw new ConfigurationErrorsException(string.Format("BaseUrl '{0}' is not valid!", configuredBaseUrl));

            try
            {
                _baseUrl = new Uri(configuredBaseUrl);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to convert base url string '{0}' to a URI", configuredBaseUrl);
                throw;
            }
        }

        public string ToAbsolute(string href)
        {
            Uri result;
            bool combineSuccess = Uri.TryCreate(_baseUrl, href, out result);
            if (combineSuccess)
            {
                return result.AbsoluteUri;
            }
            throw new ArgumentException("Failed to combine the given reference with application base path", "href");
        }
    }
}
