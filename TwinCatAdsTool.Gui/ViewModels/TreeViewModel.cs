using System.Collections.ObjectModel;
using TwinCAT.TypeSystem;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class TreeViewModel
    {
        public ISymbol Model { get; }
        public string Name { get; set; }

        public TreeViewModel(ISymbol model)
        {
            Model = model;
            Name = model.InstanceName;
        }
    }
}
