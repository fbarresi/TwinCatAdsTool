using System;
using System.Collections.Generic;
using System.Linq;
using TwinCAT.TypeSystem;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public struct SearchResult
    {
        public string SearchTerm { get; set; }
        public IEnumerable<ISymbol> Results { get; set; }
    }
}