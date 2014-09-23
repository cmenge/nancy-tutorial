using System.Linq;
using CM.Base.Logging;
using CM.Base.Models;

namespace CM.Base.Services
{
    public class SystemSettingsService
    {
        private static readonly ILogger _log = LogManager.GetLogger();
        private readonly MongoContext _db;
        private Settings _settings;

        public SystemSettingsService(MongoContext db)
        {
            _db = db;
        }

        private Settings RetrieveSettings()
        {
            _log.Debug("Loading settings...");
            var settings = _db.GetCollection<Settings>().FindAll().SingleOrDefault();
            if (settings == null)
            {
                _log.Info("No settings found, creating default settings.");
                settings = new Settings();
                _db.Insert(settings);
            }
            return settings;
        }

        public Settings Settings
        {
            get
            {
                if (_settings == null)
                    _settings = RetrieveSettings();
                return _settings;
            }
        }

        public void Update(Settings settings)
        {
            _log.Info("Updating settings in DB");
            _db.Update(settings);
        }
    }
}
