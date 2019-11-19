using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class CompareViewModel : ViewModelBase
    {
        private readonly Subject<string> leftTextSubject = new Subject<string>();
        private readonly Subject<string> rightTextSubject = new Subject<string>();
        private readonly IClientService clientService;
        private readonly SideBySideDiffBuilder comparisonBuilder = new SideBySideDiffBuilder(new Differ());
        private SideBySideDiffModel comparisonModel = new SideBySideDiffModel();
        private IEnumerable<ListBoxItem> leftBoxText;
        private readonly IPersistentVariableService persistentVariableService;
        private IEnumerable<ListBoxItem> rightBoxText;

        public CompareViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
        }

        public IEnumerable<ListBoxItem> LeftBoxText
        {
            get
            {
                if (leftBoxText == null)
                {
                    leftBoxText = new List<ListBoxItem>();
                }

                return leftBoxText;
            }
            set
            {
                if (value == leftBoxText)
                {
                    return;
                }

                leftBoxText = value;
                raisePropertyChanged();
            }
        }

        public ReactiveCommand<Unit, Unit> LoadLeft { get; set; }
        public ReactiveCommand<Unit, Unit> LoadRight { get; set; }

        public ReactiveCommand<Unit, Unit> ReadLeft { get; set; }

        public ReactiveCommand<Unit, Unit> ReadRight { get; set; }

        public IEnumerable<ListBoxItem> RightBoxText
        {
            get
            {
                if (rightBoxText == null)
                {
                    rightBoxText = new List<ListBoxItem>();
                }

                return rightBoxText;
            }
            set
            {
                if (value == rightBoxText) return;
                rightBoxText = value;
                raisePropertyChanged();
            }
        }


        public override void Init()
        {
            var x = leftTextSubject.StartWith("")
                .CombineLatest(rightTextSubject.StartWith(""),
                               (l, r) => comparisonModel = GenerateDiffModel(l, r));

            x.ObserveOnDispatcher()
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables);

            ReadLeft = ReactiveCommand.CreateFromTask(ReadVariablesLeft, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            LoadLeft = ReactiveCommand.CreateFromTask(LoadJsonLeft)
                .AddDisposableTo(Disposables);


            ReadRight = ReactiveCommand.CreateFromTask(ReadVariablesRight, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            LoadRight = ReactiveCommand.CreateFromTask(LoadJsonRight)
                .AddDisposableTo(Disposables);
        }

        private SideBySideDiffModel GenerateDiffModel(string left, string right)
        {
            var diffModel = comparisonBuilder.BuildDiffModel(left, right);


            var leftBox = diffModel.OldText.Lines;
            var rightBox = diffModel.NewText.Lines;

            // all items have the same fixed height. this makes synchronizing of the scrollbars easier
            LeftBoxText = leftBox.Select(x => new ListBoxItem
            {
                Content = x.Text,
                Background = GetBGColor(x),
                Height = 20
            });
            RightBoxText = rightBox.Select(x => new ListBoxItem
            {
                Content = x.Text,
                Background = GetBGColor(x),
                Height = 20
            });

            Logger.Debug("Generated Comparison Model");
            return diffModel;
        }

        //manually coloring the ListboxItems depending on their diff state
        //compare https://github.com/SciGit/scigit-client/blob/master/DiffPlex/SilverlightDiffer/TextBoxDiffRenderer.cs
        private SolidColorBrush GetBGColor(DiffPiece diffPiece)
        {
            var fillColor = new SolidColorBrush(Colors.Transparent);
            if (diffPiece.Type == ChangeType.Deleted)
            {
                fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 200, 100));
            }
            else if (diffPiece.Type == ChangeType.Inserted)
            {
                fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
            }
            else if (diffPiece.Type == ChangeType.Unchanged)
            {
                fillColor = new SolidColorBrush(Colors.White);
            }
            else if (diffPiece.Type == ChangeType.Modified)
            {
                fillColor = new SolidColorBrush(Color.FromArgb(255, 220, 220, 255));
            }
            else if (diffPiece.Type == ChangeType.Imaginary)
            {
                fillColor = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            }

            return fillColor;
        }


        private Task<JObject> LoadJson()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Json files (*.json)|*.json";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (openFileDialog.ShowDialog() == true)
                {
                    JObject json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
                    Logger.Debug($"Load of File {openFileDialog.FileName} was succesful");
                    return Task.FromResult(json);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error during load of file");
            }

            return Task.FromResult<JObject>(null);
        }


        private Task LoadJsonLeft()
        {
            var json = LoadJson().Result;
            if (json != null)
            {
                leftTextSubject.OnNext(json.ToString());
                Logger.Debug("Updated left TextBox");
            }

            return Task.FromResult(Unit.Default);
        }

        private Task LoadJsonRight()
        {
            var json = LoadJson()?.Result;
            if (json != null)
            {
                rightTextSubject.OnNext(json.ToString());
                Logger.Debug("Updated right TextBox");
            }


            return Task.FromResult(Unit.Default);
        }

        private async Task<JObject> ReadVariables()
        {
            var persistentVariables = await persistentVariableService.ReadPersistentVariables(clientService.Client, clientService.TreeViewSymbols);
            leftTextSubject.OnNext(persistentVariables.ToString());

            Logger.Debug("Read Persistent Variables");
            return persistentVariables;
        }

        private async Task ReadVariablesLeft()
        {
            var json = await ReadVariables().ConfigureAwait(false);
            leftTextSubject.OnNext(json.ToString());
            Logger.Debug("Updated left TextBox");
        }

        private async Task ReadVariablesRight()
        {
            var json = await ReadVariables();
            rightTextSubject.OnNext(json.ToString());
            Logger.Debug("Updated right TextBox");
        }
    }
}