using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using DynamicData;
using System.Linq;
using System.Reactive.Disposables;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using TwinCatAdsTool.Interfaces.Extensions;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class GraphViewModel : ViewModelBase
    {
        readonly Dictionary<string, List<DataPoint>> dataPoints = new Dictionary<string, List<DataPoint>>();
        private PlotModel plotModel;

        private readonly SourceCache<SymbolObservationViewModel, string> symbolCache = new SourceCache<SymbolObservationViewModel, string>(x => x.Name);


        public PlotModel PlotModel
        {
            get { return plotModel; }
            set
            {
                plotModel = value;
                raisePropertyChanged();
            }
        }

        private IObservableCache<SymbolObservationViewModel, string> SymbolCache => symbolCache.AsObservableCache();

        public void AddSymbol(SymbolObservationViewModel symbol)
        {
            symbolCache.AddOrUpdate(symbol);
        }

        public override void Init()
        {
            PlotModel = new PlotModel
            {
                LegendBorder = OxyColor.FromRgb(0x80, 0x80, 0x80),
                LegendBorderThickness = 1,
                LegendBackground = OxyColor.FromRgb(0xFF, 0xFF, 0xFF),
                LegendPosition = LegendPosition.LeftBottom
            };

            SymbolCache.Connect()
                .Transform(CreateSymbolLineSeries)
                .ObserveOnDispatcher()
                .DisposeMany()
                .Subscribe()
                .AddDisposableTo(Disposables);

            var axis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "hh:mm:ss",
                IsZoomEnabled = false
            };
            PlotModel.LegendPosition = LegendPosition.BottomRight;

            PlotModel.Axes.Add(axis);
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

        private void RescaleAxisDistances()
        {
            for (var i = 0; i < PlotModel.Axes.OfType<LinearAxis>().Count(); i++)
            {
                PlotModel.Axes.OfType<LinearAxis>().Skip(i).First().AxisDistance = i * 50;
            }
        }

        private IDisposable CreateSymbolLineSeries(SymbolObservationViewModel symbol)
        {
            var lineSeries = new LineSeries();
            lineSeries.Title = symbol.Name;

            var index = PlotModel.Axes.Count - 1;

            var disposable = new CompositeDisposable();

            var axis = new LinearAxis
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
                Tag = symbol.Name,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1
            };

            lineSeries.YAxisKey = symbol.Name;

            PlotModel.Axes.Add(axis);

            RescaleAxisDistances();

            dataPoints[symbol.Name] = new List<DataPoint> {DateTimeAxis.CreateDataPoint(DateTime.Now, Convert.ToDouble(symbol.Value))};

            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    handler => handler.Invoke,
                    h => symbol.PropertyChanged += h,
                    h => symbol.PropertyChanged -= h)
                .Where(args => args.EventArgs.PropertyName == "Value")
                .ObserveOnDispatcher()
                .Subscribe(x =>
                {
                    var refreshTime = DateTime.Now;
                    dataPoints[symbol.Name].Add(DateTimeAxis.CreateDataPoint(refreshTime, Convert.ToDouble(symbol.Value)));
                }).AddDisposableTo(disposable);


            Observable.Interval(TimeSpan.FromSeconds(1))
                .ObserveOnDispatcher()
                .Subscribe(x =>
                {
                    var newPoints = dataPoints[symbol.Name].Where(point => !lineSeries.Points.Select(oldPoint => oldPoint.X).Contains(point.X));
                    if (!newPoints.Any() && dataPoints[symbol.Name].Any())
                    {
                        var lastPoint = dataPoints[symbol.Name].LastOrDefault();
                        newPoints = new[] {DateTimeAxis.CreateDataPoint(DateTime.Now, lastPoint.Y)};
                    }

                    lineSeries.Points.AddRange(newPoints);
                    PlotModel.InvalidatePlot(true);
                    raisePropertyChanged("PlotModel");
                })
                .AddDisposableTo(disposable);

            PlotModel.Series.Add(lineSeries);

            raisePropertyChanged("PlotModel");

            return disposable;
        }
    }
}