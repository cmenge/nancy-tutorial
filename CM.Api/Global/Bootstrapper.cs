using CM.Base;
using CM.Base.Logging;
using Nancy;
using Nancy.Conventions;
using ServiceStack.Text;

namespace CM.Api
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        private static readonly ILogger _log = LogManager.GetLogger();
        private static MongoContext _db;

        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);
            conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("ui", @"Content"));
            conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("angular", "angular"));
            conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("scripts", @"scripts", ".js"));

            JsConfig.AlwaysUseUtc = true;
            JsConfig.ThrowOnDeserializationError = true;
            JsConfig.TreatEnumAsInteger = false;
            JsConfig.DateHandler = JsonDateHandler.ISO8601;
            //JsConfig.EmitCamelCaseNames = true;
        }

        protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            _db = new MongoContext("cm-api");
            
            container.Register<MongoContext>(_db).WithStrongReference();
            var rootPathProvider = container.Resolve<IRootPathProvider>();            
            base.ApplicationStartup(container, pipelines);

            pipelines.OnError.AddItemToStartOfPipeline((context, exception) => {
                _log.Error(exception, "Global Error");
                // context.Trace.TraceLog.WriteLog(p => p.Append(""));
                return context.Response;
            });
        }
    }
}

