using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Text_Editor.AuxiliaryClasses
{
    public class Tab : INotifyPropertyChanged
    {
        private string _header;
        public string Header
        {
            get => _header;
            set
            {
                if (value != _header)
                {
                    _header = value;
                    OnPropertyChanged(nameof(Header));
                }
            }
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set
            {
                if (value != _fileName)
                {
                    _fileName = value;
                    Header = Path.GetFileName(FileName);
                }
            }
        }

        private string? _text;
        public string? Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                }
            }
        }
        public bool HasTextChanged { get; set; }

        public Tab(string? text, string fileName)
        {
            Text = text;
            FileName = fileName;
            if (!string.IsNullOrEmpty(fileName))
                Header = Path.GetFileName(FileName);
            else
                Header = "New file.txt";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
