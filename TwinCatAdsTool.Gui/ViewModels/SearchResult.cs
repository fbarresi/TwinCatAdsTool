using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public struct SearchResult
    {
        public string SearchTerm { get; set; }
        public IEnumerable<TreeViewModel> Results { get; set; }
    }
}