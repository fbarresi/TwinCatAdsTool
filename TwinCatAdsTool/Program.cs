using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using log4net;
using Ninject;
using TwinCatAdsTool.Gui;
using TwinCatAdsTool.Gui.ViewModels;
using TwinCatAdsTool.Gui.Views;
using TwinCatAdsTool.Interfaces;
using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Logging;
using TwinCatAdsTool.Logic;

namespace TwinCatAdsTool
{
	public class Program
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeConsole();

		[STAThread]
		public static void Main(string[] args)
		{
			FreeConsole();

			using (IKernel kernel = new StandardKernel())
			{
				try
				{
					CreateLogger();
					LoggerFactory.GetLogger().Info(string.Join("", Enumerable.Repeat("#",80)));
					LoggerFactory.GetLogger().Info("Application starts!");
					LoggerFactory.GetLogger().Info("Loading kernel modules... ");
					LoadModules(kernel);
					LoggerFactory.GetLogger().Info("Kernel modules loaded");

					var viewModelFactory = kernel.Get<ViewModelLocator>();
					var application = CreateApplication(viewModelFactory);

					var mainWindowViewModel = viewModelFactory.CreateViewModel<MainWindowViewModel>();

					var mainWindow = kernel.Get<MainWindow>();
					mainWindow.DataContext = mainWindowViewModel;

					LoggerFactory.GetLogger().Info(string.Join("", Enumerable.Repeat("#",80)));

					application.Run(mainWindow);
					application.Shutdown();
					LoggerFactory.GetLogger().Info("Application ended...");
					LoggerFactory.GetLogger().Info("\n\n\n\n");
				}
				catch (Exception e)
				{
					LoggerFactory.GetLogger().Error("Unhandled exception", e);

                    throw;
				}
			}
		}

		private static void CreateLogger()
		{
			log4net.Config.XmlConfigurator.Configure(new FileInfo("log.config"));
			LogManager.CreateRepository(Constants.LoggingRepositoryName);
			LogManager.CreateRepository(Constants.LoggingObservationRepositoryName);
		}

		private static void LoadModules(IKernel kernel)
		{
			kernel.Load<GuiModuleCatalog>();
			kernel.Load<LogicModuleCatalog>();
		}

		private static Application CreateApplication(IViewModelFactory viewModelLocator)
		{
			var application = new App() { ShutdownMode = ShutdownMode.OnLastWindowClose };

			application.InitializeComponent();
			application.ReplaceViewModelLocator(viewModelLocator);

			return application;
		}
	}

	public static class ApplicationExtensions
	{
		public static void ReplaceViewModelLocator(this Application application, IViewModelFactory viewModelLocator, string locatorKey = "Locator")
		{
            if (application.Resources.Contains(locatorKey))
            {
                application.Resources.Remove(locatorKey);
            }

            application.Resources.Add(locatorKey, viewModelLocator);
		}
	}
}
