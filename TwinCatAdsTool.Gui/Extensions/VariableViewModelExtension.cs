using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCatAdsTool.Gui.ViewModels;

namespace TwinCatAdsTool.Gui.Extensions
{
    public static class VariableViewModelExtension
    {
        // equal if viewModels have same properties, json values can be different
        public static bool HasEqualStructure(this IEnumerable<VariableViewModel> viewModels1, IEnumerable<VariableViewModel> viewModels2)
        {
            if (!viewModels1.Count().Equals(viewModels2.Count()))
            {
                return false;
            }

            foreach(var element in viewModels1)
            {
                if(viewModels2.Count(x => x.Name == element.Name) != 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
