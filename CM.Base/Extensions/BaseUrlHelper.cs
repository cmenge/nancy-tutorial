using System;
using System.Configuration;
using CM.Base.Logging;

namespace CM.Base.Extensions
{
    public static class BaseUrlHelper
    {
        private static readonly ILogger _log = LogManager.GetLogger();
        private static string _urlPattern = ConfigurationManager.AppSettings["UrlPattern"];
        private static Uri _rootUri;

        private static void VerifyBaseUrl()
        {
            if (_rootUri == null)
            {
                if (string.IsNullOrWhiteSpace(_urlPattern))
                    throw new ConfigurationErrorsException("UrlPattern not configured! Make sure 'UrlPattern' is set to a valid value in the application setttings file!");

                _log.Debug("UrlPattern: {0}", _urlPattern);

                if (_urlPattern.StartsWith("http://") == false && _urlPattern.StartsWith("https://") == false)
                    throw new ConfigurationErrorsException("UrlPattern does not seem to contain a protocol prefix ('http' or 'https')!");

                var rootUrl = string.Format(_urlPattern, "root");
                var success = Uri.TryCreate(rootUrl, UriKind.Absolute, out _rootUri);

                if (success == false)
                {
                    throw new ConfigurationErrorsException(string.Format("Failed to convert root url {0} to a valid absolute URI", rootUrl));
                }

                _log.Debug("BaseUrl configured successfully");
            }
        }

        public static string ToAbsolute(string href)
        {
            _urlPattern = ConfigurationManager.AppSettings["UrlPattern"];
            string baseUrl = string.Format(_urlPattern, "www");

            Uri result;
            bool combineSuccess = Uri.TryCreate(new Uri(baseUrl), href, out result);
            if (combineSuccess)
            {
                return result.AbsoluteUri;
            }
            throw new ArgumentException("Failed to combine the given reference with the application base path", "href");
        }

        public static string StaticBase()
        {
            string baseUrl = string.Format(_urlPattern, "static");
            return baseUrl;
        }

        //internal static string InviteEmailUrl(Guid guid)
        //{
        //    // Messy: URL mapping should be done using the routing system, but that 
        //    // is architecturally flawed because it assumes backend jobs don't need 
        //    // to know the URL structure, which is hard to overcome.
        //    return ToAbsolute("/Invitation/AcceptInvite/" + guid.ToString());
        //}

        //internal static string ConfirmEmailUrl(string validationToken)
        //{
        //    return ToAbsolute("/Account/Confirm?token=" + HttpUtility.UrlEncode(validationToken));
        //}
    }
}
