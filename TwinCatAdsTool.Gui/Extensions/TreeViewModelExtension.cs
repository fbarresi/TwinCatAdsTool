using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using TwinCatAdsTool.Gui.ViewModels;

namespace TwinCatAdsTool.Gui.Extensions
{
    public static class TreeViewModelExtension
    {
        public static ObservableCollection<TreeViewModel> Flatten(this IEnumerable<TreeViewModel> viewModels)
        {
            var results = new ObservableCollection<TreeViewModel>();
            results.AddRange(viewModels);

            var childs = viewModels.SelectMany(vm => vm.Children);
            if (childs.Any())
            {
                results.AddRange(childs.Flatten());
            }

            return results;
        }

    }
}
