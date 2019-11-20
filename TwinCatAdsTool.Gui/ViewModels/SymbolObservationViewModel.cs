using System;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using TwinCAT.Ads.Reactive;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;


namespace TwinCatAdsTool.Gui.ViewModels
{
    public abstract class SymbolObservationViewModel : ViewModelBase
    {
        protected readonly IClientService ClientService;
        private ObservableAsPropertyHelper<object> helper;

        protected SymbolObservationViewModel(ISymbol model, IClientService clientService)
        {
            ClientService = clientService;
            Model = model;
        }

        public ReactiveCommand<Unit, Unit> CmdSubmit { get; private set; }
        public ISymbol Model { get; set; }
        public string Name { get; set; }

        public bool SuportsGraph => GetSupportsGraph();
        public bool SupportsSubmit => GetSupportsSubmit();

        public object Value => helper.Value;

        public override void Init()
        {
            Name = Model.InstanceName;
            var readSymbolInfo = ClientService.Client.ReadSymbolInfo(Model.InstancePath);
            var initialValue = ClientService.Client.ReadSymbol(readSymbolInfo);
            var observable = ((IValueSymbol) Model).WhenValueChanged().StartWith(initialValue);
            helper = observable.ToProperty(this, m => m.Value);

            CmdSubmit = ReactiveCommand.CreateFromTask(_ => SubmitSymbol())
                .AddDisposableTo(Disposables);
        }

        protected abstract bool GetSupportsGraph();
        protected abstract bool GetSupportsSubmit();
        protected abstract Task SubmitSymbol();
    }

    public class SymbolObservationDefaultViewModel : SymbolObservationViewModel
    {
        public SymbolObservationDefaultViewModel(ISymbol model, IClientService clientService) : base(model, clientService)
        {
        }

        protected override bool GetSupportsGraph()
        {
            return false;
        }

        protected override bool GetSupportsSubmit()
        {
            return false;
        }

        protected override Task SubmitSymbol()
        {
            throw new NotImplementedException();
        }
    }

    public class SymbolObservationViewModel<T> : SymbolObservationViewModel
    {
        private T newValue;

        public SymbolObservationViewModel(ISymbol model, IClientService clientService) : base(model, clientService)
        {
        }

        public T NewValue
        {
            get => newValue;
            set
            {
                newValue = value;
                raisePropertyChanged();
            }
        }

        public override void Init()
        {
            base.Init();
            NewValue = (T) Value;
        }

        protected override bool GetSupportsGraph()
        {
            return (typeof(T) == typeof(int))
                || (typeof(T) == typeof(short))
                || (typeof(T) == typeof(bool))
                || (typeof(T) == typeof(float))
                || (typeof(T) == typeof(double))
                || (typeof(T) == typeof(byte))
                || (typeof(T) == typeof(ushort))
                || (typeof(T) == typeof(uint))
                || (typeof(T) == typeof(sbyte));
        }

        protected override bool GetSupportsSubmit()
        {
            return true;
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
                MessageBox.Show(Resources.ThisValueIsReadOnly, Resources.ReadOnlyValue, MessageBoxButton.OK);
                return;
            }

            var variableHandle = ClientService.Client.CreateVariableHandle(Model.InstancePath);

            if (typeof(T) == typeof(string))
            {
                Logger.Debug(string.Format(Resources.TryingToWriteTo0WithValue1, Model?.InstancePath, (value as string)));
                ClientService.Client.WriteAnyString(variableHandle, value as string, (value as string).Length, Encoding.Default);
            }
            else
            {
                Logger.Debug(string.Format(Resources.TryingToWriteTo0WithValue1, Model?.InstancePath, value));
                ClientService.Client.WriteAny(variableHandle, value);
            }
        }
    }
}