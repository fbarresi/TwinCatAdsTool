using System;
using System.Linq;
using Ninject.Modules;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Interfaces.Services;
using TwinCatAdsTool.Logic.Services;

namespace TwinCatAdsTool.Logic
{
	public class LogicModuleCatalog : NinjectModule
	{
		public override void Load()
        {
            Bind<IClientService>().To<ClientService>().InSingletonScope();
            Bind<IPersistentVariableService>().To<PersistentVariableService>().InSingletonScope();
            Bind<ISelectionService<ISymbol>>().To<SymbolSelectionService>().InSingletonScope();
        }
	}
}
