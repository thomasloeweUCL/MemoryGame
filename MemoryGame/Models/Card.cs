using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MemoryGame.Models
{
    // Represents a single card in the game.
    // Implements INotifyPropertyChanged to automatically update the UI when a property changes.
    public class Card : INotifyPropertyChanged
    {
        // Private backing fields for properties that will notify the UI.
        private bool _isFlipped;
        private bool _isMatched;

        // A unique identifier for the card. Not strictly used in the game logic but good practice.
        public int Id { get; set; }
        // The symbol displayed on the card's face (e.g., "🐶").
        public string Symbol { get; set; }

        // Property to check if the card is currently face up.
        public bool IsFlipped
        {
            get => _isFlipped;
            set
            {
                _isFlipped = value;
                OnPropertyChanged(); // Notifies the UI that this property has changed.
            }
        }

        // Property to check if the card has been successfully matched with its pair.
        public bool IsMatched
        {
            get => _isMatched;
            set
            {
                _isMatched = value;
                OnPropertyChanged(); // Notifies the UI that this property has changed.
            }
        }

        // The event that is raised when a property value changes.
        public event PropertyChangedEventHandler PropertyChanged;

        // Method that raises the PropertyChanged event.
        // The [CallerMemberName] attribute automatically gets the name of the property that called this method.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            // Invokes the event, sending 'this' (the Card instance) and the property name.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}