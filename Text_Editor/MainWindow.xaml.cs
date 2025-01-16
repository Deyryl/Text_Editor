using Microsoft.Win32;
using System;
using System.Timers;
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

namespace Text_Editor
{
    public partial class MainWindow : Window
    {
        private bool _hasTextChanged = false;
        private string _fileName = "";
        private readonly string _dialogFileTypes = "Text file (*.txt)|*.txt|All files|*.*|C# file (*.cs)|*.cs|C++ file (*.cpp)|*.cpp||C file (*.c)|*.c|";
        //private bool isBold = false;
        //private bool isItalic = false;
        //private bool isUnderline = false;
        private System.Timers.Timer AutoSaveTimer;

        public MainWindow()
        {
            InitializeComponent();
            
            TxtBoxDoc.FontSize = 14;
            FillFontFamilyComboBox(FontFamilyComboBox);

            AutoSaveTimer = new System.Timers.Timer(2000);
            AutoSaveTimer.Elapsed += AutoSaveDocument;
            AutoSaveTimer.AutoReset = false;
        }

        #region FileHandlers
        private void SaveBeforeClosing_Prompt()
        {
            if (_hasTextChanged)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Do you want to save before closing?", "Closing", MessageBoxButton.YesNoCancel);

                switch (messageBoxResult)
                {
                    case MessageBoxResult.Yes:
                        SaveFile();
                        break;
                    case MessageBoxResult.No:
                        NewFile();
                        break;
                    default:
                        return;
                }
            }

            TxtBoxDoc.Clear();
            _hasTextChanged = false;
        }

        #region MenuFunc
        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            SaveBeforeClosing_Prompt();
            NewFile();
        }

        private void NewFile()
        {
            _fileName = "";
            _hasTextChanged = false;
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            SaveBeforeClosing_Prompt();
            OpenFile();
        }

        private void DetectSyntaxAndChange()
        {
            string fileType;
            byte indexfileType;

            switch (_fileName.Substring(_fileName.LastIndexOf('.') + 1))
            {
                case ("cs"):
                    fileType = "C#";
                    indexfileType = 1;
                    break;
                case ("cpp"):
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
            OpenFileDialog openDlg = new OpenFileDialog
            {
                Filter = _dialogFileTypes,

                InitialDirectory = File.Exists(_fileName) ?
                    _fileName.Remove(_fileName.LastIndexOf('\\')) :
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openDlg.ShowDialog() == true)
            {
                TxtBoxDoc.Text = File.ReadAllText(openDlg.FileName);
                _fileName = openDlg.FileName;
                DetectSyntaxAndChange();
                _hasTextChanged = false;
            }
        }

        public void OpenFile(string filePath)
        {
            TxtBoxDoc.Text = File.ReadAllText(filePath);
            _fileName = filePath;
            DetectSyntaxAndChange();
            _hasTextChanged = false;
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
            if (File.Exists(_fileName) && !saveAs)
            {
                File.WriteAllText(_fileName, TxtBoxDoc.Text);
                return;
            }

            SaveFileDialog saveDlg = ReturnSaveDialog();

            if (saveDlg.ShowDialog() == true)
            {
                File.WriteAllText(saveDlg.FileName, TxtBoxDoc.Text);
                _fileName = saveDlg.FileName;
                _hasTextChanged = false;
                DetectSyntaxAndChange();
            }
        }

        private SaveFileDialog ReturnSaveDialog()
        {
            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = _dialogFileTypes,

                InitialDirectory = File.Exists(_fileName) ?
                    _fileName.Remove(_fileName.LastIndexOf('\\')) :
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),

                DefaultExt = "txt",
                AddExtension = true,
                FileName = _fileName.LastIndexOf('\\') != -1 ? _fileName.Substring(_fileName.LastIndexOf('\\') + 1) : _fileName
            };
            return saveDlg;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SaveBeforeClosing_Prompt();

            if (_hasTextChanged)
                e.Cancel = true;

            Properties.Settings.Default.Save();
        }

        private void Bold_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Italic_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Underlining_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        private void TxtBoxDoc_TextChanged(object sender, EventArgs e)
        {
            _hasTextChanged = true;
            AutoSaveTimer.Stop();
            AutoSaveTimer.Start();
        }

        private void AutoSaveDocument(object sender, ElapsedEventArgs e)
        {
            if (!_fileName.Equals("") && File.Exists(_fileName))
            {
                try
                {
                    File.WriteAllText(_fileName, TxtBoxDoc.Text);
                }
                catch(Exception ex) { }
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
    }
}
