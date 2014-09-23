using System;
using System.Configuration;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using CM.Base.BusinessModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CM.Base.Logging
{
    public class LogManager
    {
        public static ILogger GetLogger()
        {
            return new Logger();
        }
    }

    public class Log : DBObject
    {
        public Log(LogSeverity severity, string format, object[] args, Exception ex = null)
        {
            Severity = severity;
            Message = string.Format(format, args);
            if(ex != null)
            {
                Message += Environment.NewLine;
                Message += FormatException(ex);
            }

            MessageHash = MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(format));

            var stackTrace = new StackTrace(2, true).GetFrame(0);
            Method = stackTrace.GetMethod().Name;
            var file = stackTrace.GetFileName();
            var line = stackTrace.GetFileLineNumber();
            var col = stackTrace.GetFileColumnNumber();
            FileLocation = string.Format("{0}({1},{2})", file, line, col);
        }

        private string FormatException(Exception ex)
        {
            if (ex == null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Exception: {0}", ex.GetType().Name);
            sb.Append(Environment.NewLine);
            sb.Append(ex.Message);
            foreach (object key in ex.Data.Keys)
            {
                sb.Append("  " + key.ToString() + " : " + ex.Data[key].ToString() + Environment.NewLine);
            }
            sb.Append(Environment.NewLine);
            sb.Append(ex.StackTrace);

            if (ex.InnerException != null)
                sb.Append(FormatException(ex.InnerException));

            return sb.ToString();
        }

        [BsonElement("s")]
        public LogSeverity Severity { get; set; }

        [BsonElement("msg")]
        public string Message { get; set; }

        [BsonElement("hash")]
        public byte[] MessageHash { get; set; }

        [BsonElement("req")]
        [BsonIgnoreIfNull]
        public string RequestId { get; set; }

        [BsonElement("method")]
        [BsonIgnoreIfNull]
        public string Method { get; set; }

        [BsonElement("loc")]
        [BsonIgnoreIfNull]
        public string FileLocation { get; set; }
    }

    public enum LogSeverity
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4
    }

    public interface ILogger
    {
        void Debug(string format, params object[] args);
        void Info(string format, params object[] args);
        void Warn(string format, params object[] args);
        void Error(string format, params object[] args);

        void Info(Exception ex, string format, params object[] args);
        void Warn(Exception ex, string format, params object[] args);
        void Error(Exception ex, string format, params object[] args);
    }

    class Logger : ILogger
    {
        private static MongoDatabase _realContext;

        public void Debug(string format, params object[] args)
        {
            DbLog(new Log(LogSeverity.Debug, format, args));
        }

        public void Info(string format, params object[] args)
        {
            DbLog(new Log(LogSeverity.Info, format, args));
        }

        public void Warn(string format, params object[] args)
        {
            DbLog(new Log(LogSeverity.Warn, format, args));
        }

        public void Error(string format, params object[] args)
        {
            DbLog(new Log(LogSeverity.Error, format, args));
        }

        private MongoDatabase Context
        {
            get
            {
                if (_realContext == null)
                {
                    var databaseName = "cm-api";
                    var host = ConfigurationManager.AppSettings["MongoHost"] ?? "localhost";
                    MongoClient client = new MongoClient(String.Format("mongodb://{0}/{1}", host, databaseName));
                    var server = client.GetServer();
                    try
                    {
                        server.Connect();
                        _realContext = server.GetDatabase(databaseName, WriteConcern.Acknowledged);
                    }
                    catch { }
                }

                return _realContext;
            }
        }

        private void DbLog(Log log)
        {
            try
            {
                var ctx = Context;
                if (ctx != null)
                    ctx.GetCollection("Log").Insert(log);
            }
            catch (Exception)
            {
                throw;
                //_logger.Error(() => "\r\n================================================\r\nFailed to log to DB!", ex);
                //_logger.Error(() => string.Format("Original log message: {0} | {1} | {2} | {3} | {4}\r\n{5}\r\n================================================\r\n\r\n", le.SeverityLevel, le.Machine, le.Application, le.UserId, le.EntityId, le.Message));
            }
        }

        public void Info(Exception ex, string format, params object[] args)
        {
            DbLog(new Log(LogSeverity.Info, format, args, ex));
        }

        public void Warn(Exception ex, string format, params object[] args)
        {
            DbLog(new Log(LogSeverity.Warn, format, args, ex));
        }

        public void Error(Exception ex, string format, params object[] args)
        {
            DbLog(new Log(LogSeverity.Error, format, args, ex));
        }

        public void Fatal(Exception ex, string format, params object[] args)
        {
            DbLog(new Log(LogSeverity.Fatal, format, args, ex));
        }
    }
}
