using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Gui.ViewModels;

namespace TwinCatAdsTool.Gui.Extensions
{
    public static class TreeViewModelExtension
    {
        public static ObservableCollection<TreeViewModel> Flatten(this IEnumerable<ISymbol> viewModels)
        {
            var results = new ObservableCollection<TreeViewModel>();
            results.AddRange(viewModels.Select(symbol => new TreeViewModel(symbol)));

            var childs = viewModels.SelectMany(vm => vm.SubSymbols);
            if (childs.Any())
            {
                results.AddRange(childs.Flatten());
            }

            return results;
        }

    }
}
