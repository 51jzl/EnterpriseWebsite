using System;
namespace Victornet.Logging
{
	public interface ILoggerFactoryAdapter
	{
		ILogger GetLogger(string loggerName);
	}
}
