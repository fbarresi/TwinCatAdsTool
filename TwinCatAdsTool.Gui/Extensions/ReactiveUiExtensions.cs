using System;
using System.Linq;
using System.Reactive.Disposables;
using log4net;
using ReactiveUI;

namespace TwinCatAdsTool.Gui.Extensions
{
	public static class ReactiveUiExtensions
	{
		public static T SetupErrorHandling<T>(this T obj, ILog logger, CompositeDisposable disposables, string message = "Error in ReactiveCommand") where T : IDisposable, IHandleObservableErrors
		{
			disposables.Add(obj.ThrownExceptions.Subscribe<Exception>((Action<Exception>) (ex => logger.Error(message, ex))));
			disposables.Add((IDisposable) obj);
			return obj;
		}
	}
}