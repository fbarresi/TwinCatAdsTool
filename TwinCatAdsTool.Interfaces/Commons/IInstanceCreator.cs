using System;
using System.Linq;
using Ninject.Parameters;

namespace TwinCatAdsTool.Interfaces.Commons
{
	public interface IInstanceCreator
	{
		T CreateInstance<T>(ConstructorArgument[] arguments);
	}
}