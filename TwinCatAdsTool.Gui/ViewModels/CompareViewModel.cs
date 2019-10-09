

using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;
using TextBox = System.Web.UI.WebControls.TextBox;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class CompareViewModel : ViewModelBase
    {

        private readonly Subject<JObject> leftTextSubject = new Subject<JObject>();
        private readonly Subject<JObject> rightTextSubject = new Subject<JObject>();
        private string backupText;
        private IClientService clientService;
        private IPersistentVariableService persistentVariableService;
        private IProcessingService processingService;
        private SideBySideDiffBuilder comparisonBuilder = new SideBySideDiffBuilder(new Differ());
        private SideBySideDiffModel comparisonModel = new SideBySideDiffModel();
        private readonly FontInfo currentFont;
        private Grid leftGrid = new Grid();
        private Grid rightGrid = new Grid();
        private string leftBox;
        private string rightBox;

        public bool ShowVisualAids { private get; set; }
        public double? CharacterWidthOverride { private get; set; }
        public double? LeftOffsetOverride { private get; set; }
        public double? LinePaddingOverride { private get; set; }
        public double? TopOffsetOverride { private get; set; }

        public CompareViewModel(IClientService clientService, IPersistentVariableService persistentVariableService, IProcessingService processingService)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
            this.processingService = processingService;
        }


        public override void Init()
        {

            var x = Observable
                .CombineLatest(leftTextSubject, rightTextSubject, 
                (l, r) => comparisonModel = GenerateDiffModel(l, r));

            x.ObserveOnDispatcher()
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables);

            leftTextSubject.OnNext(new JObject());
            rightTextSubject.OnNext(new JObject());

    
            Read = ReactiveCommand.CreateFromTask(ReadVariables, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            Load = ReactiveCommand.CreateFromTask(LoadJson, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);
        }

        private SideBySideDiffModel GenerateDiffModel(JObject left, JObject right)
        {
            var diffModel = comparisonBuilder.BuildDiffModel(left.ToString(), right.ToString());


            LeftBox = left.ToString();
            RightBox = right.ToString();

            RenderDiffLines(LeftGrid, diffModel.OldText);
            RenderDiffLines(RightGrid, diffModel.NewText);
            return diffModel;
        }

        // https://github.com/SciGit/scigit-client/blob/master/DiffPlex/SilverlightDiffer/TextBoxDiffRenderer.cs
        private void RenderDiffLines(Grid grid, DiffPaneModel diffModel)
        {
            var lineNumber = 0;
            foreach (var line in diffModel.Lines)
            {
                var fillColor = new SolidColorBrush(Colors.Transparent);
                if (line.Type == ChangeType.Deleted)
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 200, 100));
                else if (line.Type == ChangeType.Inserted)
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                else if (line.Type == ChangeType.Unchanged)
                    fillColor = new SolidColorBrush(Colors.White);
                else if (line.Type == ChangeType.Modified)
                {
                    /*
                    if (currentFont.IsMonoSpaced)
                        RenderDiffWords(grid, textBox, line, lineNumber);
                        */
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 220, 220, 255));
                }
                else if (line.Type == ChangeType.Imaginary)
                {
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));

                    //AddImaginaryLine(textBox, lineNumber);
                }

                /*
                if (ShowVisualAids)
                {
                    if (lineNumber % 2 == 0)
                        fillColor = new SolidColorBrush(Colors.Cyan);
                    else
                    {
                        fillColor = new SolidColorBrush(Colors.Gray);
                    }
                }
                */

                PlaceRectangleInGrid(grid, lineNumber, fillColor, 0, null);
                lineNumber++;
            }
        }

        private void PlaceRectangleInGrid( Grid grid, int lineNumber, SolidColorBrush fillColor, double left, double? width)
        {
            var rectLineHeight = 20; //textBox.FontSize + (LinePaddingOverride ?? 0);//currentFont.LinePadding);
            double rectTopOffset = TopOffsetOverride ?? 3;

            var offset = rectLineHeight * lineNumber + rectTopOffset;
            var floor = Math.Floor(offset);
            var fraction = offset - floor;

            var rectangle = new Rectangle
            {
                Fill = fillColor,
                Width = width ?? Double.NaN,
                Height = rectLineHeight + fraction,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = width.HasValue ? HorizontalAlignment.Left : HorizontalAlignment.Stretch,
                Margin = new Thickness(left, floor, 0, 0)
            };

            grid.Children.Insert(0, rectangle);
        }

        public ReactiveCommand<System.Reactive.Unit, Unit> Read { get; set; }
        public ReactiveCommand<System.Reactive.Unit, Unit> Load { get; set; }

        public Grid LeftGrid
        {
            get => leftGrid;
            set
            {
                if (value == leftGrid) return;
                leftGrid = value;
                raisePropertyChanged();
            }
        }


        public Grid RightGrid
        {
            get => rightGrid;
            set
            {
                if (value == rightGrid) return;
                leftGrid = value;
                raisePropertyChanged();
            }
        }

        public string LeftBox
        {
            get => leftBox;
            set
            {
                if (value == leftBox) return;
                leftBox = value;
                raisePropertyChanged();
            }
        }

        public string RightBox
        {
            get => rightBox;
            set
            {
                if (value == rightBox) return;
                rightBox = value;
                raisePropertyChanged();
            }
        }

        private async Task<Unit> ReadVariables()
        {
            var persistentVariables = await persistentVariableService.ReadPersistentVariables(clientService.Client);
            leftTextSubject.OnNext(persistentVariables);
            return Unit.Empty;
        }

        private async Task<Unit> LoadJson()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|Jason files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                JObject json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
                rightTextSubject.OnNext(json);
                
            }

            return Unit.Empty;
        }

    }
}
