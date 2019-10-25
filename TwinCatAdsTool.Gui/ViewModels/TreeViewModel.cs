using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.TypeSystem;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class TreeViewModel
    {
        public ISymbol Model { get; }
        public string Name { get; set; }

        public ObservableCollection<TreeViewModel> Children { get; set; } = new ObservableCollection<TreeViewModel>();

        public string FullPath { get; set; }

        public TreeViewModel(ISymbol model)
        {
            Model = model;
            Name = model.InstanceName;
        }
    }
}
