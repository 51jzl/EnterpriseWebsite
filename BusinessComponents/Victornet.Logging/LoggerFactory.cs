using System;
namespace Victornet.Logging
{
	public static class LoggerFactory
	{
		public static ILogger GetLogger(string loggerName)
		{
			ILoggerFactoryAdapter loggerFactoryAdapter = DIContainer.Resolve<ILoggerFactoryAdapter>();
			return loggerFactoryAdapter.GetLogger(loggerName);
		}
		public static ILogger GetLogger()
		{
			return LoggerFactory.GetLogger("Victornet.Logging");
		}
	}
}
