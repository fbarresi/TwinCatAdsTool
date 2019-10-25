using System;
using System.Linq;
using DynamicData.Binding;
using ReactiveUI;
using TwinCAT.Ads.Reactive;
using TwinCAT.TypeSystem;


namespace TwinCatAdsTool.Gui.ViewModels
{
    public class SymbolObservationViewModel : ViewModelBase
    {
        private ObservableAsPropertyHelper<object> helper;
        public ISymbol Model { get; set; }

        public object Value => helper.Value;
        public string Name { get; set; }

        public SymbolObservationViewModel(ISymbol model)
        {
            Model = model;
        }
        public override void Init()
        {
            Name = Model.InstanceName;
            var observable = ((IValueSymbol) Model).WhenValueChanged();
            helper = observable.ToProperty(this, m => m.Value);
        }
    }
}