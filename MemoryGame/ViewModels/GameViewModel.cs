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
    // It implements INotifyPropertyChanged to update the UI when its properties change.
    public class GameViewModel : INotifyPropertyChanged
    {
        // The repository for saving/loading high scores. Using the interface type ensures flexibility.
        private readonly IGameStatsRepository _statsRepository;
        // A timer to track game duration. DispatcherTimer is used because it operates on the UI thread,
        // which avoids cross-thread issues when updating UI-bound properties like GameTime.
        private DispatcherTimer _timer;
        // The time when the current game started, used to calculate elapsed time.
        private DateTime _startTime;

        // --- Private backing fields for public properties ---
        // These fields hold the actual data.
        private string _playerName = "Player 1";
        private int _moveCount;
        private string _gameTime;
        private bool _isGameCompleted;
        private Card _firstSelectedCard;
        private Card _secondSelectedCard;
        // A flag to prevent the player from clicking other cards while a pair is being checked.
        private bool _isChecking;

        // --- Public Properties for UI Binding ---

        // A collection of cards for the game board.
        // ObservableCollection is crucial for MVVM. It automatically notifies the UI
        // when items are added, removed, or the whole collection is refreshed.
        public ObservableCollection<Card> Cards { get; set; }

        // A collection for the high scores list, displayed in the HighScoresWindow.
        public ObservableCollection<GameStats> HighScores { get; set; }

        // The player's name. Bound to a TextBox in the UI for two-way communication.
        public string PlayerName { get => _playerName; set { _playerName = value; OnPropertyChanged(); } }

        // The number of moves made. Bound to a TextBlock in the UI.
        public int MoveCount { get => _moveCount; set { _moveCount = value; OnPropertyChanged(); } }

        // The formatted game time string (e.g., "01:23"). Bound to a TextBlock.
        public string GameTime { get => _gameTime; set { _gameTime = value; OnPropertyChanged(); } }

        // A flag indicating if the game has been won. Used to show/hide the "Game Completed!" message.
        public bool IsGameCompleted { get => _isGameCompleted; set { _isGameCompleted = value; OnPropertyChanged(); } }

        // --- Commands for UI Binding ---
        // ICommand properties are bound to UI elements like Buttons.

        // Command to handle a card being flipped.
        public ICommand FlipCardCommand { get; }
        // Command to start a new game.
        public ICommand NewGameCommand { get; }
        // Command to open the high scores window.
        public ICommand ShowHighScoresCommand { get; }

        // --- Constructor ---
        // This is executed once when a GameViewModel object is created.
        public GameViewModel()
        {
            // Initialize the repository for data access.
            _statsRepository = new FileGameStatsRepository();
            // Initialize the collections that the UI will bind to.
            Cards = new ObservableCollection<Card>();
            HighScores = new ObservableCollection<GameStats>();

            // Instantiate the commands, linking them to their respective methods.
            // The first parameter is the action to execute.
            // The second (optional) parameter is a function that determines if the command can execute.
            FlipCardCommand = new RelayCommand(FlipCard, CanFlipCard);
            NewGameCommand = new RelayCommand(_ => NewGame());
            ShowHighScoresCommand = new RelayCommand(_ => ShowHighScores());

            // Start the first game automatically when the application launches.
            NewGame();
        }

        // --- Game Logic Methods ---

        // Sets up and starts a new game.
        private void NewGame()
        {
            // Define the set of symbols for the cards.
            var symbols = new List<string> { "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼" };
            // Create pairs by concatenating the list with itself, then shuffle the order randomly.
            var cardSymbols = symbols.Concat(symbols).OrderBy(s => Guid.NewGuid()).ToList();

            // Clear any cards from a previous game. The UI updates automatically.
            Cards.Clear();
            // Create 16 new card objects and add them to the collection.
            for (int i = 0; i < 16; i++)
            {
                Cards.Add(new Card { Id = i, Symbol = cardSymbols[i] });
            }

            // Reset the game's state variables.
            ResetSelection();
            MoveCount = 0;
            GameTime = "00:00";
            IsGameCompleted = false;

            // --- Timer Setup ---
            _startTime = DateTime.Now;
            _timer?.Stop(); // Stop any existing timer.
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (sender, args) =>
            {
                // This code runs every second. It updates the GameTime property.
                // Because of INotifyPropertyChanged, the UI's TextBlock for the time will update automatically.
                GameTime = (DateTime.Now - _startTime).ToString(@"mm\:ss");
            };
            _timer.Start();
        }

        // Determines if the FlipCardCommand can be executed.
        private bool CanFlipCard(object parameter)
        {
            // A card can be flipped only if:
            // 1. The clicked item is actually a Card object.
            // 2. The card is not already flipped.
            // 3. The card is not already matched.
            // 4. We are not currently in the middle of checking a pair (_isChecking is false).
            return parameter is Card card && !card.IsFlipped && !card.IsMatched && !_isChecking;
        }

        // The main action when a card is clicked. This method is asynchronous.
        private async void FlipCard(object parameter)
        {
            var card = parameter as Card;
            if (card == null) return; // Safety check.

            card.IsFlipped = true; // Flip the card. The UI updates via the Card's OnPropertyChanged.

            if (_firstSelectedCard == null)
            {
                // This is the first card of a pair being flipped. Store it.
                _firstSelectedCard = card;
            }
            else
            {
                // This is the second card.
                _secondSelectedCard = card;
                _isChecking = true; // Set flag to prevent more clicks.
                CommandManager.InvalidateRequerySuggested(); // Force commands to re-evaluate their CanExecute status.

                MoveCount++; // A move consists of flipping a pair.
                await CheckForMatchAsync(); // Asynchronously check if the two cards are a match.

                _isChecking = false; // Reset the flag to allow clicks again.
                CommandManager.InvalidateRequerySuggested(); // Re-evaluate commands again.
            }
        }

        // Checks if the two selected cards match. 'async Task' allows for the use of 'await'.
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
                // Not a match. Wait for 800ms so the player can see the second card.
                // 'await' pauses this method without freezing the UI.
                await Task.Delay(800);
                // Flip both cards back down.
                _firstSelectedCard.IsFlipped = false;
                _secondSelectedCard.IsFlipped = false;
                ResetSelection();
            }
        }

        // Clears the references to the selected cards.
        private void ResetSelection()
        {
            _firstSelectedCard = null;
            _secondSelectedCard = null;
        }

        // Checks if all cards have been matched to determine if the game is over.
        private void CheckForGameCompletion()
        {
            // LINQ's .All() method checks if every item in the collection satisfies the condition.
            if (Cards.All(c => c.IsMatched))
            {
                IsGameCompleted = true; // Set the flag that the UI is bound to.
                _timer.Stop(); // Stop the game timer.
                SaveStats(); // Save the game result.
            }
        }

        // Saves the current game's stats using the repository.
        private void SaveStats()
        {
            // Create a new GameStats object with the final data.
            var stats = new GameStats
            {
                // Use a default name if the player name is empty or just whitespace.
                PlayerName = string.IsNullOrWhiteSpace(PlayerName) ? "Unknown" : PlayerName,
                Moves = MoveCount,
                GameTime = DateTime.Now - _startTime,
                CompletedAt = DateTime.Now
            };
            // Pass the object to the repository to handle the actual saving.
            _statsRepository.SaveStats(stats);
        }

        // Prepares and shows the high scores window.
        private void ShowHighScores()
        {
            // Retrieve the top scores from the repository.
            var scores = _statsRepository.GetTopTenScores();
            // Clear the existing high scores and add the freshly retrieved ones.
            HighScores.Clear();
            foreach (var score in scores)
            {
                HighScores.Add(score);
            }

            // Create a new instance of the HighScoresWindow.
            var highScoresView = new HighScoresWindow
            {
                DataContext = this
            };
            // Show the window as a dialog, which blocks interaction with the main window until it's closed.
            highScoresView.ShowDialog();
        }

        // Standard implementation for the INotifyPropertyChanged interface.
        public event PropertyChangedEventHandler PropertyChanged;

        // Helper method to raise the PropertyChanged event.
        // The [CallerMemberName] attribute automatically passes the name of the calling property.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}