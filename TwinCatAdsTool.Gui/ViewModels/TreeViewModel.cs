using System;
using System.Linq;
using TwinCAT.TypeSystem;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class TreeViewModel
    {
        public TreeViewModel(ISymbol model)
        {
            Model = model;
            Name = model.InstanceName;
        }

        public ISymbol Model { get; }
        public string Name { get; set; }
    }
}