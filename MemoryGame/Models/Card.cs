using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MemoryGame.Models
{
    public class Card : INotifyPropertyChanged
    {
        private bool _isFlipped;
        private bool _isMatched;

        public int Id { get; set; }
        public string Symbol { get; set; }

        public bool IsFlipped
        {
            get => _isFlipped;
            set { _isFlipped = value; OnPropertyChanged(); }
        }

        public bool IsMatched
        {
            get => _isMatched;
            set { _isMatched = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}