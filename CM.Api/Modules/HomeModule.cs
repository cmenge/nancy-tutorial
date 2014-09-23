using CM.Base.Logging;
using Nancy;

namespace CM.Api.Modules
{
    public class HomeModule : Nancy.NancyModule
    {
        private static readonly ILogger _log = LogManager.GetLogger();

        public HomeModule()
        {
            Get["/"] = _ => View["index.html"].WithContentType("text/html; charset=utf-8");
        }
    }
}
