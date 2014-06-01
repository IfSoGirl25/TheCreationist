﻿using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using ProjectVoid.Core.Helpers;
using ProjectVoid.Core.Utilities;
using ProjectVoid.TheCreationist.Properties;
using ProjectVoid.TheCreationist.View;
using ProjectVoid.TheCreationist.ViewModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace ProjectVoid.TheCreationist.Managers
{
    public class CommandManager : IDisposable
    {
        public CommandManager(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;

            CreateProjectCommand = new RelayCommand(
                () => CreateProject(),
                () => CanCreateProject());

            OpenProjectCommand = new RelayCommand<MainViewModel>(
                (m) => OpenProject(m),
                (m) => CanOpenProject(m));

            SaveProjectCommand = new RelayCommand<ProjectViewModel>(
                (p) => SaveProject(p),
                (p) => CanSaveProject(p));

            CloseProjectCommand = new RelayCommand<ProjectViewModel>(
                (p) => CloseProject(p),
                (p) => CanCloseProject(p));

            CloseAllProjectsCommand = new RelayCommand(
                () => CloseAllProjects(),
                () => CanCloseAllProjects());

            CloseAllProjectsExceptCommand = new RelayCommand<ProjectViewModel>(
                (p) => CloseAllProjectsExcept(p),
                (p) => CanCloseAllProjectsExcept(p));

            ConvertProjectCommand = new RelayCommand<ProjectViewModel>(
                (p) => ConvertProject(p),
                (p) => CanConvertProject(p));

            CompileProjectCommand = new RelayCommand<ProjectViewModel>(
                (p) => CompileProject(p),
                (p) => CanCompileProject(p));

            CloseWindowCommand = new RelayCommand<Window>(
                (p) => CloseWindow(p),
                (p) => CanCloseWindow(p));

            SelectSwatchCommand = new RelayCommand<MouseButtonEventArgs>(
                (e) => SelectSwatch(e),
                (e) => CanSelectSwatch(e));

            OpenLogLocationCommand = new RelayCommand(
                () => OpenLogLocation(),
                () => CanOpenLogLocation());
        }

        public MainViewModel MainViewModel { get; private set; }

        public RelayCommand CreateProjectCommand { get; private set; }

        public RelayCommand<MainViewModel> OpenProjectCommand { get; private set; }

        public RelayCommand<ProjectViewModel> SaveProjectCommand { get; private set; }

        public RelayCommand<ProjectViewModel> CloseProjectCommand { get; private set; }

        public RelayCommand CloseAllProjectsCommand { get; private set; }

        public RelayCommand<ProjectViewModel> CloseAllProjectsExceptCommand { get; private set; }

        public RelayCommand<ProjectViewModel> ConvertProjectCommand { get; private set; }

        public RelayCommand<ProjectViewModel> CompileProjectCommand { get; private set; }

        public RelayCommand<Window> CloseWindowCommand { get; private set; }

        public RelayCommand<MouseButtonEventArgs> SelectSwatchCommand { get; private set; }

        public RelayCommand OpenLogLocationCommand { get; private set; }

        private void CreateProject()
        {
            Logger.Log.Debug("Creating");

            ProjectViewModel project = new ProjectViewModel(MainViewModel);

            MainViewModel.Projects.Add(project);

            MainViewModel.ActiveProject = project;

            Logger.Log.DebugFormat("Created ID[{0}] Name[{1}]", project.Id, project.Name);
        }

        private bool CanCreateProject()
        {
            return true;
        }

        private void OpenProject(MainViewModel mainViewModel)
        {
            Logger.Log.Debug("Opening");

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToString();
            openFileDialog.Multiselect = false;
            openFileDialog.DefaultExt = ".xml";
            openFileDialog.Filter = "XML|*.xml";

            var result = openFileDialog.ShowDialog();

            if (result == false || result == null)
            {
                Logger.Log.Debug("Aborted");
                return;
            }

            var file = openFileDialog.FileName;

            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                ProjectViewModel project = XamlReader.Load(fileStream) as ProjectViewModel;
                project.MainViewModel = mainViewModel;

                if (mainViewModel.Projects.Any(p => p.Project.Id.Equals(project.Project.Id)))
                {
                    var match = mainViewModel.Projects.First(p => p.Project.Id.Equals(project.Project.Id));

                    mainViewModel.ActiveProject = match;

                    if (match.State.IsDirty)
                    {
                        var canReload = MessageBox.Show(String.Format("{0} has unsaved changes, are you sure you want to reload it?", match.Name), String.Format("Reload {0}?", match.Name), MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        if (canReload == MessageBoxResult.No)
                        {
                            fileStream.Close();
                            Logger.Log.Debug("Aborted ProjectAlreadyExits");
                            return;
                        }

                        MainViewModel.Projects.Remove(match);
                    }
                    else
                    {
                        fileStream.Close();
                        Logger.Log.DebugFormat("Aborted ProjectAlreadyExits");
                        return;
                    }
                }

                project.State.IsSaved = true;
                project.State.IsDirty = false;

                mainViewModel.Projects.Add(project);
                mainViewModel.ActiveProject = project;

                fileStream.Close();

                Logger.Log.DebugFormat("Opened ID[{0}] Name[{1}]", project.Id, project.Name);
            }
        }

        private bool CanOpenProject(MainViewModel mainViewModel)
        {
            return true;
        }

        private void SaveProject(ProjectViewModel projectViewModel)
        {
            Logger.Log.Debug("Saving");

            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToString();
            saveFileDialog.DefaultExt = ".xml";
            saveFileDialog.Filter = "XML|*.xml";

            var result = saveFileDialog.ShowDialog();

            if (result == false || result == null)
            {
                Logger.Log.Debug("Aborted");
                return;
            }

            var file = saveFileDialog.FileName;

            projectViewModel.Name = Path.GetFileNameWithoutExtension(file);

            using (FileStream fileStream = new FileStream(file, FileMode.Create))
            {
                XamlWriter.Save(projectViewModel, fileStream);

                fileStream.Close();
                Logger.Log.DebugFormat("Saved Project ID[{0}] Name[{1}]", projectViewModel.Id, projectViewModel.Name);
            }

            projectViewModel.State.IsSaved = true;
            projectViewModel.State.IsDirty = false;
        }

        private bool CanSaveProject(ProjectViewModel projectViewModel)
        {
            if (projectViewModel == null)
            {
                return false;
            }

            return true;
        }

        private void CloseProject(ProjectViewModel projectViewModel)
        {
            Logger.Log.Debug("Closing");

            if (projectViewModel.State.IsDirty == true)
            {
                var result = MessageBox.Show(String.Format("{0} has unsaved changes, are you sure you want to close it?", projectViewModel.Name), String.Format("Close {0}?", projectViewModel.Name), MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    Logger.Log.Debug("Aborted UnsavedChangesDetected");
                    return;
                }
            }

            MainViewModel.Projects.Remove(projectViewModel);

            Logger.Log.DebugFormat("Closed ID[{0}] Name[{1}]", projectViewModel.Id, projectViewModel.Name);
        }

        private bool CanCloseProject(ProjectViewModel projectViewModel)
        {
            if (MainViewModel.Projects.Count > 0)
            {
                return true;
            }

            return false;
        }

        private void CloseAllProjects()
        {
            Logger.Log.Debug("Closing");

            for (int i = MainViewModel.Projects.Count - 1; i > -1; i--)
            {
                CloseProject(MainViewModel.Projects[i]);
            }

            Logger.Log.Debug("Closed");
        }

        private bool CanCloseAllProjects()
        {
            if (MainViewModel.Projects.Count > 0)
            {
                return true;
            }

            return false;
        }

        private void CloseAllProjectsExcept(ProjectViewModel projectViewModel)
        {
            Logger.Log.Debug("Closing");

            for (int i = MainViewModel.Projects.Count - 1; i > -1; i--)
            {
                if (MainViewModel.Projects[i].Project.Id.Equals(projectViewModel.Project.Id))
                {
                    continue;
                }

                CloseProject(MainViewModel.Projects[i]);
            }

            Logger.Log.Debug("Closed");
        }

        private bool CanCloseAllProjectsExcept(ProjectViewModel projectViewModel)
        {
            if (MainViewModel.Projects.Count > 0)
            {
                return true;
            }

            return false;
        }

        private void ConvertProject(ProjectViewModel projectViewModel)
        {
            try
            {
                Logger.Log.Debug("Converting");

                var project = projectViewModel;

                if (project.Document.Blocks.Count < 1)
                {
                    Logger.Log.Debug("Compiling Cancelled[InsufficientBlockCount]");
                    return;
                }

                Brush _DefaultForeground = ColorUtility.ConvertBrushFromString(Settings.Default.Foreground.ToString());
                Brush _DefaultBackground = ColorUtility.ConvertBrushFromString(Settings.Default.Background.ToString());

                Brush _LastForeground;
                Brush _LastBackground;

                _LastForeground = _DefaultForeground;
                _LastBackground = _DefaultBackground;

                string result = ProcessBlocks(project.Document.Blocks, _DefaultForeground, _DefaultBackground, _LastForeground, _LastBackground);

                project.Project.Document.Blocks.Clear();

                project.Project.Document.Blocks.Add(new Paragraph(new Run(result) { Foreground = _DefaultForeground, Background = _DefaultBackground }));

                Logger.Log.DebugFormat("Converted ID[{0}] Name[{1}]", projectViewModel.Id, projectViewModel.Name);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Convert Exception", ex);
            }
        }

        private string ProcessBlocks(BlockCollection blocks, Brush defaultForeground, Brush defaultBackground, Brush lastForeground, Brush lastBackground)
        {
            StringBuilder processedBlocks = new StringBuilder();

            if (blocks != null)
            {
                foreach (Block block in blocks)
                {
                    Paragraph paragraph = block as Paragraph;

                    processedBlocks.Append("%r");

                    processedBlocks.Append(ProcessInlines(paragraph.Inlines, defaultForeground, defaultBackground, lastForeground, lastBackground));
                }
            }

            if (processedBlocks.ToString().Length >= 2)
            {
                return processedBlocks.ToString().Substring(2);
            }

            return null;
        }

        private string ProcessInlines(InlineCollection inlines, Brush defaultForeground, Brush defaultBackground, Brush lastForeground, Brush lastBackground)
        {
            StringBuilder processedInlines = new StringBuilder();

            foreach (Inline inline in inlines)
            {
                Char[] chars = null;

                Brush foreground = null;

                Brush background = null;

                if (inline.GetType().Equals(typeof(Span)))
                {
                    Span span = ((Span)inline);

                    chars = new TextRange(span.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward), span.ContentEnd.GetNextInsertionPosition(LogicalDirection.Backward)).Text.ToCharArray();

                    foreground = span.Foreground;
                    background = span.Background;
                }
                else if (inline.GetType().Equals(typeof(Run)))
                {
                    Run run = ((Run)inline);

                    chars = run.Text.ToCharArray();

                    foreground = run.Foreground;
                    background = run.Background;
                }
                else
                {
                    MessageBox.Show(inline.GetType().ToString());
                }

                if (background == null)
                {
                    background = defaultBackground;
                }

                if (foreground.ToString() == lastForeground.ToString() && background.ToString() == lastBackground.ToString())
                {
                    //
                }

                if (foreground.ToString() == lastForeground.ToString() && background.ToString() != lastBackground.ToString())
                {
                    processedInlines.Append(String.Format("%X<#{0}>", background.ToString().ToUpper().Substring(3)));
                }

                if (foreground.ToString() != lastForeground.ToString() && background.ToString() == lastBackground.ToString())
                {
                    processedInlines.Append(String.Format("%x<#{0}>", foreground.ToString().ToUpper().Substring(3)));
                }

                if (foreground.ToString() != lastForeground.ToString() && background.ToString() != lastBackground.ToString())
                {
                    if (foreground.ToString() == defaultForeground.ToString() && background.ToString() == defaultBackground.ToString())
                    {
                        processedInlines.Append(String.Format("%xn"));
                    }
                    else
                    {
                        processedInlines.Append(String.Format("%x<#{0}>%X<#{1}>", foreground.ToString().ToUpper().Substring(3), background.ToString().ToUpper().Substring(3)));
                    }
                }

                lastForeground = foreground;
                lastBackground = background;

                for (int i = 0; i < chars.Length; i++)
                {
                    switch (chars[i].ToString())
                    {
                        case "\\":
                            processedInlines.Append("%\\");
                            break;

                        case "%":
                            processedInlines.Append("%%");
                            break;

                        case "{":
                            processedInlines.Append("%{");
                            break;

                        case "}":
                            processedInlines.Append("%}");
                            break;

                        case "[":
                            processedInlines.Append("%[");
                            break;

                        case "]":
                            processedInlines.Append("%]");
                            break;

                        case "\r":
                            processedInlines.Append("%r"); i++;
                            break;

                        case "\t":
                            processedInlines.Append("%t");
                            break;

                        case " ":
                            StringBuilder spaces = new StringBuilder();

                            spaces.Append(" ");

                            while ((i + 1) < chars.Length && chars[i + 1].Equals(' '))
                            {
                                spaces.Append(" ");
                                i++;
                            }

                            if (spaces.Length == 1) { processedInlines.Append(spaces.ToString()); }
                            else
                            {
                                processedInlines.Append(String.Format("[space({0})]", spaces.Length.ToString()));
                            }

                            break;

                        default:
                            processedInlines.Append(chars[i]);
                            break;
                    }
                }
            }

            return processedInlines.ToString();
        }

        private bool CanConvertProject(ProjectViewModel projectViewModel)
        {
            if (projectViewModel == null)
            {
                return false;
            }

            return true;
        }

        private void CompileProject(ProjectViewModel projectViewModel)
        {
            try
            {
                Logger.Log.Debug("Compiling");

                var project = projectViewModel;

                DocumentBuilder document = CreateDocument(project);

                project.Project.Document.Blocks.Clear();

                if (document == null)
                {
                    return;
                }

                if (document.Blocks.Count < 1)
                {
                    Logger.Log.Debug("Compiling Cancelled[InsufficientBlockCount]");
                    return;
                }

                project.Project.Document.Blocks.AddRange(document.Blocks);

                Logger.Log.DebugFormat("Compiled ID[{0}] Name[{1}]", projectViewModel.Id, projectViewModel.Name);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Compile Exception", ex);
            }
        }

        private DocumentBuilder CreateDocument(ProjectViewModel project)
        {
            string text = null;

            text = new TextRange(project.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward), project.Document.ContentEnd.GetNextInsertionPosition(LogicalDirection.Backward)).Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            Brush defaultForeground = ColorUtility.ConvertBrushFromString(Settings.Default.Foreground.ToString());
            Brush defaultBackground = ColorUtility.ConvertBrushFromString(Settings.Default.Background.ToString());

            bool highlight = false;

            DocumentBuilder documentBuilder = new DocumentBuilder(defaultForeground, defaultBackground);

            StringBuilder stringBuilder = new StringBuilder();

            RegexHelper regexHelper = new RegexHelper();

            Match match = null;

            string subString = String.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i].ToString())
                {
                    case "[":
                        subString = text.Substring(i);

                        match = regexHelper.WhiteSpace.Match(subString, 0);

                        if (match.Success == true)
                        {
                            StringBuilder whiteSpace = new StringBuilder();

                            for (int j = 0; j < int.Parse(match.Groups[1].Value); j++)
                            {
                                whiteSpace.Append(" ");
                            }

                            stringBuilder.Append(whiteSpace);

                            i = i + (match.Length - 1);
                        }

                        break;

                    case "%": subString = text.Substring(i);
                        switch (subString[1].ToString())
                        {
                            case "{":
                            case "}":
                            case "[":
                            case "]":
                            case "%":
                            case "\\":
                                stringBuilder.Append(subString[1].ToString());
                                break;

                            case "r": stringBuilder.Append("\r\n");
                                break;

                            case "t": stringBuilder.Append("\t");
                                break;

                            case "b": stringBuilder.Append(" ");
                                break;

                            case "x":
                            case "X":
                            case "c":
                            case "C":
                                switch (subString[2].ToString())
                                {
                                    case "x":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        if (highlight)
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#808080");

                                        }
                                        else
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#000000");
                                        }
                                        break;

                                    case "X":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        documentBuilder.ActiveBackground = ColorUtility.ConvertBrushFromString("#000000");
                                        break;

                                    case "r":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        if (highlight)
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#FF0000");
                                        }
                                        else
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#800000");
                                        }
                                        break;

                                    case "R":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        documentBuilder.ActiveBackground = ColorUtility.ConvertBrushFromString("#FF0000");
                                        break;

                                    case "g":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        if (highlight)
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#00FF00");
                                        }
                                        else
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#008000");
                                        }
                                        break;

                                    case "G":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        documentBuilder.ActiveBackground = ColorUtility.ConvertBrushFromString("#00FF00");
                                        break;

                                    case "y":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        if (highlight)
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#FFFF00");
                                        }
                                        else
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#808000");
                                        }
                                        break;

                                    case "Y":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        documentBuilder.ActiveBackground = ColorUtility.ConvertBrushFromString("#FFFF00");
                                        break;

                                    case "b":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        if (highlight)
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#0000FF");
                                        }
                                        else
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#000080");
                                        }
                                        break;

                                    case "B":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        documentBuilder.ActiveBackground = ColorUtility.ConvertBrushFromString("#0000FF");
                                        break;

                                    case "m":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        if (highlight)
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#FF00FF");
                                        }
                                        else
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#800080");
                                        }
                                        break;

                                    case "M":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        documentBuilder.ActiveBackground = ColorUtility.ConvertBrushFromString("#FF00FF");
                                        break;

                                    case "c":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        if (highlight)
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#00FFFF");
                                        }
                                        else
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#008080");
                                        }
                                        break;

                                    case "C":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        documentBuilder.ActiveBackground = ColorUtility.ConvertBrushFromString("#00FFFF");
                                        break;

                                    case "w":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        if (highlight)
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#FFFFFF");
                                        }
                                        else
                                        {
                                            documentBuilder.ActiveForeground = ColorUtility.ConvertBrushFromString("#C0C0C0");
                                        }
                                        break;

                                    case "W":
                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        documentBuilder.ActiveBackground = ColorUtility.ConvertBrushFromString("#FFFFFF");
                                        break;

                                    case "n":
                                        highlight = false;

                                        documentBuilder.AddRun(stringBuilder.ToString());
                                        stringBuilder.Clear();

                                        documentBuilder.ActiveForeground = defaultForeground;
                                        documentBuilder.ActiveBackground = defaultBackground;
                                        break;

                                    case "h":
                                        highlight = true;
                                        break;

                                    case "f":
                                        break;

                                    case "i":
                                        break;

                                    default:

                                        match = regexHelper.AnsiColor.Match(subString, 0, 11);

                                        if (match.Success == true)
                                        {
                                            documentBuilder.AddRun(stringBuilder.ToString());
                                            stringBuilder.Clear();

                                            if (match.Groups[1].Value.Equals("X"))
                                            {
                                                documentBuilder.ActiveBackground =
                                                    ColorUtility.ConvertBrushFromString(match.Groups[2].Value);
                                            }
                                            else
                                            {
                                                documentBuilder.ActiveForeground =
                                                ColorUtility.ConvertBrushFromString(match.Groups[2].Value);
                                            }

                                            i = i + (match.Length - 3);
                                        }
                                        break;
                                }

                                i++;
                                break;
                        }

                        i++;
                        break;

                    case "{":
                    case "}":
                    case "]":
                    case "\\":
                        break;

                    default:
                        stringBuilder.Append(text[i].ToString()); break;
                }
            }

            documentBuilder.AddRun(stringBuilder.ToString());

            return documentBuilder;
        }

        private bool CanCompileProject(ProjectViewModel projectViewModel)
        {
            if (projectViewModel == null)
            {
                return false;
            }

            return true;
        }

        private void CloseWindow(Window window)
        {
            Logger.Log.Debug("Closing");

            window.Close();

            Logger.Log.DebugFormat("Closed Title[{0}]", window.Title);
        }

        private bool CanCloseWindow(Window window)
        {
            return true;
        }

        private void SelectSwatch(MouseButtonEventArgs eventArgs)
        {
            try
            {
                SwatchViewModel swatch = ((SwatchView)eventArgs.Source).DataContext as SwatchViewModel;

                switch (eventArgs.ChangedButton)
                {
                    case MouseButton.Left:
                        MainViewModel.ActiveProject.Foreground = swatch.Color;

                        if (MainViewModel.ActiveProject.Selection != null && !MainViewModel.ActiveProject.Selection.IsEmpty)
                        {
                            MainViewModel.ActiveProject.Selection.ApplyPropertyValue(TextBlock.ForegroundProperty, new SolidColorBrush(swatch.Color));
                        }
                        break;

                    case MouseButton.Right:
                        MainViewModel.ActiveProject.Background = swatch.Color;

                        if (MainViewModel.ActiveProject.Selection != null && !MainViewModel.ActiveProject.Selection.IsEmpty)
                        {
                            MainViewModel.ActiveProject.Selection.ApplyPropertyValue(TextBlock.BackgroundProperty, new SolidColorBrush(swatch.Color));
                        }
                        break;

                    case MouseButton.Middle:
                        MainViewModel.ActiveProject.Backdrop = swatch.Color;
                        break;

                    default:
                        return;
                }

                Logger.Log.DebugFormat("Selected Color[{0}] Click[{1}]", swatch.Color, eventArgs.ChangedButton);

                MainViewModel.ActiveProject.State.IsDirty = true;

                eventArgs.Handled = true;
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Select Exception", ex);
            }
        }

        private bool CanSelectSwatch(MouseButtonEventArgs eventArgs)
        {
            return true;
        }

        private void OpenLogLocation()
        {
            Logger.Log.Debug("Opening");

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            Process.Start(System.IO.Path.GetDirectoryName(path));

            Logger.Log.DebugFormat("Opened Path[{0}]", path);
        }

        private bool CanOpenLogLocation()
        {
            return true;
        }

        public void Dispose()
        {
            Logger.Log.Debug("Disposing");

            MainViewModel = null;

            Logger.Log.Debug("Disposed");
        }
    }
}
