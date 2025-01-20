using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using Text_Editor.Properties;
using System.Windows.Media;
using System.Linq;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using Text_Editor.AuxiliaryClasses;
using System.Runtime.CompilerServices;
using System.Windows.Forms.Design;

namespace Text_Editor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int MAX_COUNT_OF_TABS = 12;
        private readonly string _dialogFileTypes = "All files|*.*|" +
                                                    "Text file (*.txt)|*.txt|" +
                                                    "C# file (*.cs)|*.cs|" +
                                                    "C++ file (*.cpp)|*.cpp|" +
                                                    "C file (*.c)|*.c|" +
                                                    "Header file (*.h)|*.h";
        private bool _isBold = false;
        private bool _isItalic = false;
        private DispatcherTimer _autoSaveTimer;
        private Tab _selectedTab;
        public Tab SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab != value)
                {
                    _selectedTab = value;
                    OnPropertyChanged(nameof(SelectedTab));
                    LoadTextToEditor();
                }
            }
        }
        public ObservableCollection<Tab> Tabs { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            //TextBox
            TxtBoxDoc.FontSize = 14;
            FillFontFamilyComboBox(FontFamilyComboBox);
            TxtBoxDoc.FontFamily = (FontFamily)FontFamilyComboBox.SelectedItem;

            //Инициализация вкладок
            Tabs = new ObservableCollection<Tab>();
            NewTab(null, "Default.txt");

            //Инициализация таймера
            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _autoSaveTimer.Tick += AutoSaveTimer_Tick!;
            _autoSaveTimer.Start();
        }

        private void NewTab(string? text, string fileName)
        {
            if (NewTabIsValid())
            {
                Tabs.Add(new Tab(text, fileName));
                SelectedTab = Tabs.Last();
                SelectedTab.HasTextChanged = false;
            }
        }

        private void CloseTab(Tab tab)
        {
            if (Tabs.Contains(tab))
            {
                if (Tabs.Count <= 1)
                {
                    NewFile();
                }
                Tabs.Remove(tab);
            }
        }

        private bool NewTabIsValid()
        {
            return Tabs.Count + 1 <= MAX_COUNT_OF_TABS;
        }

        private bool ClosingAllTabs()
        {
            while (Tabs.Count > 0)
            {
                if (Tabs.Last().HasTextChanged)
                {
                    if (!SaveBeforeClosing_Prompt(Tabs.Last()))
                        break;
                }
                else Tabs.Remove(Tabs.Last());
            }
            return Tabs.Count > 0;
        }

        private void LoadTextToEditor()
        {
            if (SelectedTab != null)
                TxtBoxDoc.Text = SelectedTab.Text;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTextToEditor();
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Tab tabToClose)
            {
                if (tabToClose.HasTextChanged)
                {
                    SaveBeforeClosing_Prompt(tabToClose);
                }
                else
                {
                    CloseTab(tabToClose);
                }

                if (SelectedTab == tabToClose)
                {
                    SelectedTab = Tabs.Last();
                }
            }
        }

        #region FileHandlers
        private bool SaveBeforeClosing_Prompt(Tab tabToClose)
        {
            MessageBoxResult result = MessageBox.Show(
                "Do you want to save changes to the " + tabToClose.Header + "?",
                "Save Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    SelectedTab = tabToClose;
                    SaveFile();
                    if (!tabToClose.HasTextChanged)
                        CloseTab(tabToClose);
                    break;
                case MessageBoxResult.No:
                    CloseTab(tabToClose);
                    break;
                case MessageBoxResult.Cancel:
                    return false;
            }
            return true;
        }

        #region MenuFunc
        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            NewFile("");
        }

        private void NewFile(string header="Default.txt")
        {
            if (!NewTabIsValid())
            {
                ActionIsNotValidMsg("Number of tabs exceeded");
                return;
            }
            NewTab("", header);
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        private void DetectSyntaxAndChange()
        {
            string fileType;
            byte indexfileType;

            switch (SelectedTab.FileName.
                    Substring(SelectedTab.FileName.
                    LastIndexOf('.') + 1))
            {
                case ("cs"):
                    fileType = "C#";
                    indexfileType = 1;
                    break;
                case ("cpp"):
                    fileType = "C++";
                    indexfileType = 2;
                    break;
                case ("h"):
                    fileType = "C++";
                    indexfileType = 2;
                    break;
                case ("c"):
                    fileType = "C++";
                    indexfileType = 2;
                    break;
                default:
                    fileType = "Text";
                    indexfileType = 0;
                    break;
            }

            ChangeSyntax(fileType);
            syntaxComboBox.SelectedIndex = indexfileType;
        }

        private void OpenFile()
        {
            if (!NewTabIsValid())
            {
                ActionIsNotValidMsg("Number of tabs exceeded");
                return;
            }

            OpenFileDialog openDlg = new OpenFileDialog
            {
                Filter = _dialogFileTypes,
                InitialDirectory = File.Exists(SelectedTab?.FileName) ?
                    Path.GetDirectoryName(SelectedTab.FileName) :
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openDlg.ShowDialog() == true)
            {
                string text = File.ReadAllText(openDlg.FileName);
                NewTab(text, openDlg.FileName);
                DetectSyntaxAndChange();
            }
        }

        private void ActionIsNotValidMsg(string msg)
        {
            MessageBoxResult result = MessageBox.Show(
                msg, "ERROR", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error
            );
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFile(true);
        }

        private void SaveFile(bool saveAs = false)
        {
            if (File.Exists(SelectedTab.FileName) && !saveAs)
            {
                File.WriteAllText(SelectedTab.FileName, TxtBoxDoc.Text);
                return;
            }

            SaveFileDialog saveDlg = ReturnSaveDialog();

            if (saveDlg.ShowDialog() == true)
            {
                File.WriteAllText(saveDlg.FileName, TxtBoxDoc.Text);
                SelectedTab.FileName = saveDlg.FileName;
                SelectedTab.HasTextChanged = false;
                DetectSyntaxAndChange();
            }
        }

        private SaveFileDialog ReturnSaveDialog()
        {
            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = _dialogFileTypes,

                InitialDirectory = File.Exists(SelectedTab.FileName) ?
                    SelectedTab.FileName.Remove(SelectedTab.FileName.LastIndexOf('\\')) :
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),

                DefaultExt = "txt",
                AddExtension = true,
                FileName = SelectedTab.FileName.LastIndexOf('\\') != -1 ? 
                    SelectedTab.FileName.Substring(SelectedTab.FileName.LastIndexOf('\\') + 1) : 
                    SelectedTab.FileName
            };
            return saveDlg;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (ClosingAllTabs())
                e.Cancel = true;

            Properties.Settings.Default.Save();
        }

        private void Bold_Click(object sender, RoutedEventArgs e)
        {
            if (_isBold) TxtBoxDoc.FontWeight = FontWeights.Normal;
            else TxtBoxDoc.FontWeight = FontWeights.Bold;
            _isBold = !_isBold;
        }

        private void Italic_Click(object sender, RoutedEventArgs e)
        {
            if (_isItalic) 
                TxtBoxDoc.FontStyle = FontStyles.Normal;
            else 
                TxtBoxDoc.FontStyle = FontStyles.Italic;
            _isItalic = !_isItalic;
        }
        #endregion

        private void TxtBoxDoc_TextChanged(object sender, EventArgs e)
        {
            SelectedTab.HasTextChanged = true;
            SelectedTab.Text = TxtBoxDoc.Text;
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            if (SelectedTab.HasTextChanged && 
                File.Exists(SelectedTab.FileName))
            {
                SaveFile();
                SelectedTab.HasTextChanged = false;
            }
        }
        #endregion

        #region ComboBoxes
        //FontSize
        private void ComboFontSize_DropDownClosed(object sender, EventArgs e)
        {
            ChangeFontSize();
        }

        private void ComboFontSize_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ChangeFontSize();
        }

        private void ComboFontSize_LostFocus(object sender, RoutedEventArgs e)
        {
            ChangeFontSize();
        }

        private void ChangeFontSize()
        {
            if (string.IsNullOrWhiteSpace(comboFontSize.Text))
            {
                ResetFontSize();
                return;
            }

            if (double.TryParse(comboFontSize.Text, out double fontSize) && 
                fontSize >= 8 && fontSize <= 72)
            {
                TxtBoxDoc.FontSize = fontSize;
            }
            else
            {
                ResetFontSize(); 
            }
        }

        private void ResetFontSize()
        {
            comboFontSize.Text = TxtBoxDoc.FontSize.ToString();
        }

        //Syntax
        private void SyntaxComboBox_OnDropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox)
                ChangeSyntax(comboBox.Text);
        }

        private void ChangeSyntax(string syntax)
        {
            var typeConverter = new HighlightingDefinitionTypeConverter();
            var syntaxHighlighter = (IHighlightingDefinition)typeConverter.ConvertFrom(syntax)!;
            TxtBoxDoc.SyntaxHighlighting = syntaxHighlighter;
        }

        //FontFamily
        private void FillFontFamilyComboBox(ComboBox comboBoxFonts)
        {
            FontFamilyComboBox.ItemsSource = Fonts.SystemFontFamilies;
            comboBoxFonts.SelectedIndex = 0;
        }

        private void FontFamilyComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                string selectedFont = comboBox.Text;
                if (Fonts.SystemFontFamilies.Any(f => f.Source.Equals(selectedFont, StringComparison.OrdinalIgnoreCase)))
                {
                    ChangeFontFamily(selectedFont);
                }
                else
                {
                    comboBox.Text = TxtBoxDoc.FontFamily.Source;
                }
            }
        }

        private void FontFamilyComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                string selectedFont = comboBox.Text;
                if (Fonts.SystemFontFamilies.Any(f => f.Source.Equals(selectedFont, StringComparison.OrdinalIgnoreCase)))
                {
                    ChangeFontFamily(selectedFont);
                }
                else
                {
                    comboBox.Text = TxtBoxDoc.FontFamily.Source;
                }
            }
        }

        private void FontFamilyComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is ComboBox comboBox)
                {
                    string selectedFont = comboBox.Text;
                    ChangeFontFamily(selectedFont);
                }
            }
        }

        private void ResetFontFamily()
        {
            FontFamilyComboBox.Text = TxtBoxDoc.FontFamily.Source;
        }

        private void ChangeFontFamily(string fontFamily)
        {
            if (string.IsNullOrWhiteSpace(fontFamily))
            {
                ResetFontFamily();
                return;
            }

            // Попробуем найти шрифт в ComboBox
            foreach (var item in FontFamilyComboBox.Items)
            {
                if (item is FontFamily ff && ff.Source.Equals(fontFamily, StringComparison.OrdinalIgnoreCase))
                {
                    TxtBoxDoc.FontFamily = ff;
                    FontFamilyComboBox.Text = ff.Source;
                    return;
                }
            }

            // Если ничего не найдено, сбросим шрифт
            ResetFontFamily();
        }

        //EncodingComboBox
        private void EncodingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        #endregion

        #region View
        private void MenuLineNumbers_OnClick(object sender, RoutedEventArgs e)
        {
            TxtBoxDoc.ShowLineNumbers = !TxtBoxDoc.ShowLineNumbers;
            menuLineNumbers.IsChecked = TxtBoxDoc.ShowLineNumbers;
            Properties.Settings.Default.LineNumbers = TxtBoxDoc.ShowLineNumbers;
        }

        private void MenuNightMode_OnClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.NightMode = !Properties.Settings.Default.NightMode;
        }
        #endregion

        #region FindReplace
        private void MenuFind_Click(object sender, RoutedEventArgs e)
        {
            SearchPanel.Install(TxtBoxDoc);
        }

        private void Replace_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FindReplaceDialog.ShowForReplace(TxtBoxDoc);
        }

        private void Replace_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Find_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FindReplaceDialog.ShowForFind(TxtBoxDoc);
        }

        private void Find_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        #endregion

        #region About
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            OpenAboutWindow();
        }

        private static void OpenAboutWindow()
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
