// ViewModels/GameViewModel.cs
using MemoryGame.Models;
using MemoryGame.Repositories;
using MemoryGame.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace MemoryGame.ViewModels
{
    // The main ViewModel for the game. It holds all the game logic and state.
    // It implements INotifyPropertyChanged to update the UI.
    public class GameViewModel : INotifyPropertyChanged
    {
        // The repository for saving/loading high scores. Using the interface type for flexibility.
        private readonly IGameStatsRepository _statsRepository;
        // A timer to track game duration. DispatcherTimer runs on the UI thread.
        private DispatcherTimer _timer;
        // The time when the current game started.
        private DateTime _startTime;

        // Private backing fields for the public properties.
        private string _playerName = "Player 1";
        private int _moveCount;
        private string _gameTime;
        private bool _isGameCompleted;
        private Card _firstSelectedCard;
        private Card _secondSelectedCard;
        // A flag to prevent the player from clicking cards while a match is being checked.
        private bool _isChecking;

        // A collection of cards for the game board. ObservableCollection automatically notifies the UI of changes (add/remove).
        public ObservableCollection<Card> Cards { get; set; }
        // A collection for the high scores list.
        public ObservableCollection<GameStats> HighScores { get; set; }

        // --- Public Properties for UI Binding ---
        // These properties are bound to UI elements like TextBlocks and TextBoxes.
        public string PlayerName { get => _playerName; set { _playerName = value; OnPropertyChanged(); } }
        public int MoveCount { get => _moveCount; set { _moveCount = value; OnPropertyChanged(); } }
        public string GameTime { get => _gameTime; set { _gameTime = value; OnPropertyChanged(); } }
        public bool IsGameCompleted { get => _isGameCompleted; set { _isGameCompleted = value; OnPropertyChanged(); } }

        // --- Commands for UI Binding ---
        // These are bound to Buttons in the View.
        public ICommand FlipCardCommand { get; }
        public ICommand NewGameCommand { get; }
        public ICommand ShowHighScoresCommand { get; }

        // Constructor
        public GameViewModel()
        {
            // Initialize the repository.
            _statsRepository = new FileGameStatsRepository();
            // Initialize the collections.
            Cards = new ObservableCollection<Card>();
            HighScores = new ObservableCollection<GameStats>();

            // Initialize the commands, linking them to their respective methods.
            FlipCardCommand = new RelayCommand(FlipCard, CanFlipCard);
            NewGameCommand = new RelayCommand(_ => NewGame());
            ShowHighScoresCommand = new RelayCommand(_ => ShowHighScores());

            // Start the first game automatically.
            NewGame();
        }

        // --- Game Logic Methods ---
        private void NewGame()
        {
            // Define the set of symbols.
            var symbols = new List<string> { "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼" };
            // Duplicate the symbols to create pairs, and randomize their order using LINQ.
            var cardSymbols = symbols.Concat(symbols).OrderBy(s => Guid.NewGuid()).ToList();

            // Clear the old cards from the board.
            Cards.Clear();
            // Create and add 16 new cards to the collection.
            for (int i = 0; i < 16; i++)
            {
                Cards.Add(new Card { Id = i, Symbol = cardSymbols[i] });
            }

            // Reset game state variables.
            ResetSelection();
            MoveCount = 0;
            GameTime = "00:00";
            IsGameCompleted = false;

            // Setup and start the game timer.
            _startTime = DateTime.Now;
            _timer?.Stop();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (sender, args) =>
            {
                // Every second, update the GameTime property. The UI will update automatically.
                GameTime = (DateTime.Now - _startTime).ToString(@"mm\:ss");
            };
            _timer.Start();
        }

        // Determines if a card can be flipped. Used by FlipCardCommand.
        private bool CanFlipCard(object parameter)
        {
            // A card can be flipped only if it is a Card, not already flipped, not already matched,
            // and we are not currently in the middle of checking a pair.
            return parameter is Card card && !card.IsFlipped && !card.IsMatched && !_isChecking;
        }

        // The main action when a card is clicked. Called by FlipCardCommand.
        private async void FlipCard(object parameter)
        {
            var card = parameter as Card;
            if (card == null) return;

            // Flip the card. The UI updates because Card.IsFlipped calls OnPropertyChanged.
            card.IsFlipped = true;

            if (_firstSelectedCard == null)
            {
                // This is the first card of a pair being flipped.
                _firstSelectedCard = card;
            }
            else
            {
                // This is the second card.
                _secondSelectedCard = card;
                _isChecking = true; // Prevent more clicks.
                CommandManager.InvalidateRequerySuggested(); // Manually tell commands to re-evaluate CanExecute.

                MoveCount++; // Increment the move counter.
                await CheckForMatchAsync(); // Check if the two cards are a match.

                _isChecking = false; // Allow clicks again.
                CommandManager.InvalidateRequerySuggested(); // Re-evaluate commands again.
            }
        }

        // Checks if the two selected cards are a match.
        private async Task CheckForMatchAsync()
        {
            if (_firstSelectedCard.Symbol == _secondSelectedCard.Symbol)
            {
                // It's a match!
                _firstSelectedCard.IsMatched = true;
                _secondSelectedCard.IsMatched = true;
                ResetSelection();
                CheckForGameCompletion();
            }
            else
            {
                // Not a match. Wait for a moment so the player can see the second card.
                await Task.Delay(800);
                // Flip both cards back down.
                _firstSelectedCard.IsFlipped = false;
                _secondSelectedCard.IsFlipped = false;
                ResetSelection();
            }
        }

        // Resets the selected cards.
        private void ResetSelection()
        {
            _firstSelectedCard = null;
            _secondSelectedCard = null;
        }

        // Checks if all cards have been matched.
        private void CheckForGameCompletion()
        {
            // If all cards on the board are matched...
            if (Cards.All(c => c.IsMatched))
            {
                IsGameCompleted = true; // Set the completion flag.
                _timer.Stop(); // Stop the game timer.
                SaveStats(); // Save the game result.
            }
        }

        // Saves the current game's stats.
        private void SaveStats()
        {
            var stats = new GameStats
            {
                // Use a default name if the player name is empty.
                PlayerName = string.IsNullOrWhiteSpace(PlayerName) ? "Unknown" : PlayerName,
                Moves = MoveCount,
                GameTime = DateTime.Now - _startTime,
                CompletedAt = DateTime.Now
            };
            // Use the repository to save the stats.
            _statsRepository.SaveStats(stats);
        }

        // Shows the high scores window.
        private void ShowHighScores()
        {
            // Retrieve the top scores from the repository.
            var scores = _statsRepository.GetTopTenScores();
            // Clear the existing high scores and add the new ones.
            HighScores.Clear();
            foreach (var score in scores)
            {
                HighScores.Add(score);
            }

            // Create and show the high scores window.
            var highScoresView = new HighScoresWindow
            {
                // Set this GameViewModel instance as the DataContext for the new window,
                // so it can bind to the HighScores collection.
                DataContext = this
            };
            highScoresView.ShowDialog();
        }

        // Standard implementation for INotifyPropertyChanged.
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}