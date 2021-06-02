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
using TwinCatAdsTool.Gui.Properties;
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
        private string sourceLeft;
        private string sourceRight;

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
                if (Equals(value, leftBoxText))
                {
                    return;
                }

                leftBoxText = value;
                raisePropertyChanged();
            }
        }

        public string SourceLeft
        {
            get
            {
                if (sourceLeft == null)
                {
                    sourceLeft = "";
                }

                return sourceLeft;
            } set
            {
                if (value == sourceLeft)
                {
                    return;
                }

                sourceLeft = value;
                raisePropertyChanged();
            }
        }

        public string SourceRight
        {
            get
            {
                if (sourceRight == null)
                {
                    sourceRight = "";
                }

                return sourceRight;
            } set
            {
                if (value == sourceRight)
                {
                    return;
                }

                sourceRight = value;
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
                if (Equals(value, rightBoxText)) return;
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

            AssignCommands();
        }

        private void AssignCommands()
        {
            ReadLeft = ReactiveCommand.CreateFromTask(ReadVariablesLeft,
                    canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            LoadLeft = ReactiveCommand.CreateFromTask(LoadJsonLeft)
                .AddDisposableTo(Disposables);


            ReadRight = ReactiveCommand.CreateFromTask(ReadVariablesRight,
                    canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
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
                Height = 20,
                Padding = new System.Windows.Thickness(0.0)
            });
            RightBoxText = rightBox.Select(x => new ListBoxItem
            {
                Content = x.Text,
                Background = GetBGColor(x),
                Height = 20,
                Padding = new System.Windows.Thickness(0.0)
            });

            Logger.Debug("Generated Comparison Model");
            return diffModel;
        }

        //manually coloring the ListboxItems depending on their diff state
        //compare https://github.com/SciGit/scigit-client/blob/master/DiffPlex/SilverlightDiffer/TextBoxDiffRenderer.cs
        private SolidColorBrush GetBGColor(DiffPiece diffPiece)
        {
            var fillColor = new SolidColorBrush(Colors.Transparent);
            switch (diffPiece.Type)
            {
                case ChangeType.Deleted:
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 200, 100));
                    break;
                case ChangeType.Inserted:
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                    break;
                case ChangeType.Unchanged:
                    fillColor = new SolidColorBrush(Colors.White);
                    break;
                case ChangeType.Modified:
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 220, 220, 255));
                    break;
                case ChangeType.Imaginary:
                    fillColor = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                    break;
            }

            return fillColor;
        }


        private Task<(JObject, string)> LoadJson()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Json files (*.json)|*.json";
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == true)
                {
                    JObject json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
                    Logger.Debug(string.Format(Resources.LoadOfFile0Wasuccesful, openFileDialog.FileName));
                    return Task.FromResult((json, System.IO.Path.GetFileName(openFileDialog.FileName)));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(Resources.ErrorDuringLoadOfFile, ex);
            }

            return Task.FromResult<(JObject, string)>((null, ""));
        }


        private Task LoadJsonLeft()
        {
            var (json, fileName) = LoadJson().Result;
            if (json != null)
            {
                leftTextSubject.OnNext(json.ToString());
                SourceLeft = fileName;
                Logger.Debug(Resources.UpdatedLeftTextBox);
            }

            return Task.FromResult(Unit.Default);
        }

        private Task LoadJsonRight()
        {
            var (json, fileName) = LoadJson().Result;
            if (json != null)
            {
                rightTextSubject.OnNext(json.ToString());
                SourceRight = fileName;
                Logger.Debug(Resources.UpdatedRightTextBox);
            }


            return Task.FromResult(Unit.Default);
        }

        private async Task<JObject> ReadVariables()
        {
            var persistentVariables = await persistentVariableService.ReadGlobalPersistentVariables(clientService.Client, clientService.TreeViewSymbols);

            Logger.Debug(Resources.ReadPersistentVariables);
            return persistentVariables;
        }

        private async Task ReadVariablesLeft()
        {
            var json = await ReadVariables().ConfigureAwait(false);
            leftTextSubject.OnNext(json.ToString());
            SourceLeft = "PLC";

            Logger.Debug(Resources.UpdatedLeftTextBox);
        }

        private async Task ReadVariablesRight()
        {
            var json = await ReadVariables().ConfigureAwait(false);
            rightTextSubject.OnNext(json.ToString());
            SourceRight = "PLC";

            Logger.Debug(Resources.UpdatedRightTextBox);
        }
    }
}
