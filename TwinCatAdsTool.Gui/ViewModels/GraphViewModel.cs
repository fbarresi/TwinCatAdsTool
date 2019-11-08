using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Interfaces.Extensions;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class GraphViewModel : ViewModelBase
    {


        public PlotModel PlotModel
        {
            get
            {
                return plotModel;
            }
            set
            {
                plotModel = value;
                raisePropertyChanged();
            }
        }

        private SourceCache<SymbolObservationViewModel, string> symbolCache = new SourceCache<SymbolObservationViewModel, string>(x => x.Name);
        private PlotModel plotModel;
        private IObservableCache<SymbolObservationViewModel, string> SymbolCache => symbolCache.AsObservableCache();
        private IObservableCollection<LineSeries> Series { get; set; } = new ObservableCollectionExtended<LineSeries>(new List<LineSeries>());

        public GraphViewModel()
        {

        }

        public override void Init()
        {
            PlotModel = new PlotModel();
            SymbolCache.Connect()
                .Transform(CreateSymbolLineSeries)
                .ObserveOnDispatcher()
                .Bind(Series)
                .Subscribe()
                .AddDisposableTo(Disposables);
            
            var axis = new DateTimeAxis {
                Position = AxisPosition.Bottom, Minimum = DateTimeAxis.ToDouble(
                    DateTime.Now.Subtract(TimeSpan.FromMinutes(15))), Maximum = DateTimeAxis.ToDouble(DateTime.Now.Add(TimeSpan.FromMinutes(15))), StringFormat = "hh:mm:ss" };

     
            PlotModel.Axes.Add(axis);
            PlotModel.Axes.Add(new LinearAxis() { Minimum = 0, Maximum = 5000 });
            Observable.Interval(TimeSpan.FromSeconds(1)).Do(x => {
                PlotModel.Axes.Replace(PlotModel.Axes.First(), new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    Minimum = DateTimeAxis.ToDouble(
                    DateTime.Now.Subtract(TimeSpan.FromMinutes(15))),
                    Maximum = DateTimeAxis.ToDouble(DateTime.Now.Add(TimeSpan.FromMinutes(15))),
                    StringFormat = "hh:mm:ss"
                });
                PlotModel.InvalidatePlot(true);
            }).ObserveOnDispatcher().Subscribe().AddDisposableTo(Disposables);
            
        }

        private LineSeries CreateSymbolLineSeries(SymbolObservationViewModel symbol)
        {
            var lineSeries = new LineSeries();
        

            Observable.Interval(TimeSpan.FromSeconds(1)).Do(x => {
                lineSeries.Points.Add(DateTimeAxis.CreateDataPoint(DateTime.Now, Convert.ToDouble(symbol.Value)));
                PlotModel.InvalidatePlot(true);
            }).ObserveOnDispatcher().Subscribe().AddDisposableTo(Disposables);


            PlotModel.Series.Add(lineSeries);

            raisePropertyChanged("PlotModel");
            return lineSeries;
        }

        public void AddSymbol(SymbolObservationViewModel symbol)
        {
            symbolCache.AddOrUpdate(symbol);
        }

        public void RemoveSymbol(SymbolObservationViewModel symbol)
        {
            symbolCache.Remove(symbol);
        }
    }
}
