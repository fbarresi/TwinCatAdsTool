using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Logic.Services
{
    public class SymbolSelectionService : ISelectionService<ISymbol>
    {
        private readonly Subject<ISymbol> elements = new Subject<ISymbol>();
        public IObservable<ISymbol> Elements => elements.AsObservable();
        public void Select(ISymbol element)
        {
            elements.OnNext(element);
        }
    }
}