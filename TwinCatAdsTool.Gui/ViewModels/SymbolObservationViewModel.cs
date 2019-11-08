using System;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using TwinCAT.Ads;
using TwinCAT.Ads.Reactive;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;


namespace TwinCatAdsTool.Gui.ViewModels
{
    public abstract class SymbolObservationViewModel : ViewModelBase
    {
        protected readonly IClientService clientService;
        private ObservableAsPropertyHelper<object> helper;
        public ISymbol Model { get; set; }
        public object Value => helper.Value;
        public string Name { get; set; }

        public Color Color { get; set; }


        public SymbolObservationViewModel(ISymbol model, IClientService clientService)
        {
            this.clientService = clientService;
            Model = model;
        }
        public override void Init()
        {
            Name = Model.InstanceName;
            var readSymbolInfo = clientService.Client.ReadSymbolInfo(Model.InstancePath);
            var initialValue = clientService.Client.ReadSymbol(readSymbolInfo);
            var observable = ((IValueSymbol) Model).WhenValueChanged().StartWith(initialValue);
            helper = observable.ToProperty(this, m => m.Value);

            CmdSubmit = ReactiveCommand.CreateFromTask(_ => SubmitSymbol())
                .AddDisposableTo(Disposables);
        }

        protected abstract Task SubmitSymbol();

        public ReactiveCommand<Unit, Unit> CmdSubmit { get; private set; }
    }

    public class SymbolObservationDefaultViewModel : SymbolObservationViewModel
    {
        public SymbolObservationDefaultViewModel(ISymbol model, IClientService clientService) : base(model, clientService)
        {
        }

        protected override Task SubmitSymbol()
        {
            throw new NotImplementedException();
        }
    }

    public class SymbolObservationViewModel<T> : SymbolObservationViewModel
    {
        private T newValue;

        public T NewValue
        {
            get => newValue;
            set  {
                newValue = value;
            raisePropertyChanged();
            }
        }

        public SymbolObservationViewModel(ISymbol model, IClientService clientService) : base(model, clientService)
        {
        }

        public override void Init()
        {
            base.Init();
            NewValue = (T) Value;
        }

        protected override Task SubmitSymbol()
        {
            Write(NewValue);
            return Task.FromResult(Unit.Default);
        }

        private void Write(T value)
        {
            if (Model.IsReadOnly)
            {
                System.Windows.MessageBox.Show("This value is Read Only", "Read Only Value", System.Windows.MessageBoxButton.OK);
                return;
            }

            var variableHandle = clientService.Client.CreateVariableHandle(Model.InstancePath);

            if (typeof(T) == typeof(string))
            {
                Logger.Debug($"Trying to write to {Model?.InstancePath} with value {(value as string)}");
                clientService.Client.WriteAnyString(variableHandle, value as string, (value as string).Length, Encoding.Default);
                return;
            }
            else{
                Logger.Debug($"Trying to write to {Model?.InstancePath} with value {value}");
                clientService.Client.WriteAny(variableHandle, value);
            }
        }


    }
}