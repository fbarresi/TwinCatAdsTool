using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
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

        public GraphViewModel()
        {

        }

        public override void Init()
        {
            PlotModel = new PlotModel();
            SymbolCache.Connect()
                .Transform(CreateSymbolLineSeries)
                .ObserveOnDispatcher()
                .DisposeMany()
                .Subscribe()
                .AddDisposableTo(Disposables);

            var axis = new DateTimeAxis {
                Position = AxisPosition.Bottom, Minimum = DateTimeAxis.ToDouble(
                    DateTime.Now.Subtract(TimeSpan.FromMinutes(15))), Maximum = DateTimeAxis.ToDouble(DateTime.Now.Add(TimeSpan.FromMinutes(15))), StringFormat = "hh:mm:ss", IsZoomEnabled = false};
            PlotModel.LegendPosition = LegendPosition.BottomRight;
     
            PlotModel.Axes.Add(axis);
            Observable.Interval(TimeSpan.FromMinutes(5))
                .ObserveOnDispatcher()
                .Subscribe(x =>
                {
                    PlotModel.Axes.Replace(PlotModel.Axes.First(), new DateTimeAxis
                    {
                        Position = AxisPosition.Bottom,
                        Minimum = DateTimeAxis.ToDouble(
                            DateTime.Now.Subtract(TimeSpan.FromMinutes(15))),
                        Maximum = DateTimeAxis.ToDouble(DateTime.Now.Add(TimeSpan.FromMinutes(15))),
                        StringFormat = "hh:mm:ss",
                        IsZoomEnabled = false
                    });
                    PlotModel.InvalidatePlot(true);
                })
                .AddDisposableTo(Disposables);

        }

        private IDisposable CreateSymbolLineSeries(SymbolObservationViewModel symbol)
        {
            var lineSeries = new LineSeries();
            lineSeries.Title = symbol.Name;

            var index = PlotModel.Axes.Count - 1;

            var axis = new LinearAxis()
            {
                AxislineThickness = 2,
                AxislineColor = PlotModel.DefaultColors[index],
                MinorTickSize = 4,
                MajorTickSize = 7,
                TicklineColor = PlotModel.DefaultColors[index],
                TextColor = PlotModel.DefaultColors[index],
                AxisDistance = PlotModel.Axes.OfType<LinearAxis>().Count() * 50,
                Position = AxisPosition.Left,
                IsZoomEnabled = false,
                Key = symbol.Name,
                Tag = symbol.Name
            };

            lineSeries.YAxisKey = symbol.Name;

            PlotModel.Axes.Add(axis);

            RescaleAxisDistances();

              var subscription = Observable.Interval(TimeSpan.FromSeconds(1))
                .ObserveOnDispatcher()
                .Subscribe(x =>
                {
                    lineSeries.Points.Add(DateTimeAxis.CreateDataPoint(DateTime.Now, Convert.ToDouble(symbol.Value)));
                    PlotModel.InvalidatePlot(true);
                    raisePropertyChanged("PlotModel");
                });


            PlotModel.Series.Add(lineSeries);

            raisePropertyChanged("PlotModel");
            return subscription;
        }

        public void RescaleAxisDistances()
        {
            for (var i = 0; i < PlotModel.Axes.OfType<LinearAxis>().Count(); i++)
            {
                PlotModel.Axes.OfType<LinearAxis>().Skip(i).First().AxisDistance = 0; //i * 50;
            }
        }

        public void AddSymbol(SymbolObservationViewModel symbol)
        {
            symbolCache.AddOrUpdate(symbol);
        }

        public void RemoveSymbol(SymbolObservationViewModel symbol)
        {
            symbolCache.Remove(symbol.Name);

            var seriesToRemove = PlotModel.Series.FirstOrDefault(series => series.Title == symbol.Name);
            if (seriesToRemove != null)
            {
                PlotModel.Series.Remove(seriesToRemove);
            }

            var axisToRemove = PlotModel.Axes.FirstOrDefault(axis => axis.Key == symbol.Name);
            if (axisToRemove != null)
            {
                PlotModel.Axes.Remove(axisToRemove);
            }

            RescaleAxisDistances();
        }
    }
}
