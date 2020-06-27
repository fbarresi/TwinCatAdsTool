using System;
using System.Linq;
using log4net;

namespace TwinCatAdsTool.Interfaces.Logging
{
	public static class LoggerFactory
	{
		public static ILog GetLogger()
		{
			return LogManager.GetLogger(Constants.LoggingRepositoryName);
		}
		
		public static ILog GetObserverLogger()
		{
			return LogManager.GetLogger(Constants.LoggingObservationRepositoryName);
		}
	}
}