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
    public class GameViewModel : INotifyPropertyChanged
    {
        private readonly IGameStatsRepository _statsRepository;
        private DispatcherTimer _timer;
        private DateTime _startTime;

        private string _playerName = "Player 1";
        private int _moveCount;
        private string _gameTime;
        private bool _isGameCompleted;
        private Card _firstSelectedCard;
        private Card _secondSelectedCard;
        private bool _isChecking;

        public ObservableCollection<Card> Cards { get; set; }
        public ObservableCollection<GameStats> HighScores { get; set; }

        public string PlayerName { get => _playerName; set { _playerName = value; OnPropertyChanged(); } }
        public int MoveCount { get => _moveCount; set { _moveCount = value; OnPropertyChanged(); } }
        public string GameTime { get => _gameTime; set { _gameTime = value; OnPropertyChanged(); } }
        public bool IsGameCompleted { get => _isGameCompleted; set { _isGameCompleted = value; OnPropertyChanged(); } }

        public ICommand FlipCardCommand { get; }
        public ICommand NewGameCommand { get; }
        public ICommand ShowHighScoresCommand { get; }

        public GameViewModel()
        {
            _statsRepository = new FileGameStatsRepository();
            Cards = new ObservableCollection<Card>();
            HighScores = new ObservableCollection<GameStats>();

            FlipCardCommand = new RelayCommand(FlipCard, CanFlipCard);
            NewGameCommand = new RelayCommand(_ => NewGame());
            ShowHighScoresCommand = new RelayCommand(_ => ShowHighScores());

            NewGame();
        }

        private void NewGame()
        {
            var symbols = new List<string> { "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼" };
            var cardSymbols = symbols.Concat(symbols).ToList();
            var random = new Random();
            cardSymbols = cardSymbols.OrderBy(s => random.Next()).ToList();

            Cards.Clear();
            for (int i = 0; i < 16; i++)
            {
                Cards.Add(new Card { Id = i, Symbol = cardSymbols[i] });
            }

            ResetSelection();
            MoveCount = 0;
            GameTime = "00:00";
            IsGameCompleted = false;

            _startTime = DateTime.Now;
            _timer?.Stop();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (sender, args) =>
            {
                GameTime = (DateTime.Now - _startTime).ToString(@"mm\:ss");
            };
            _timer.Start();
        }

        private bool CanFlipCard(object parameter)
        {
            return parameter is Card card && !card.IsFlipped && !card.IsMatched && !_isChecking;
        }

        private async void FlipCard(object parameter)
        {
            var card = parameter as Card;
            if (card == null) return;

            card.IsFlipped = true;

            if (_firstSelectedCard == null)
            {
                _firstSelectedCard = card;
            }
            else
            {
                _secondSelectedCard = card;
                _isChecking = true; // Forhindrer klik mens vi tjekker
                CommandManager.InvalidateRequerySuggested();

                MoveCount++;
                await CheckForMatchAsync();

                _isChecking = false; // Tillad klik igen
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task CheckForMatchAsync()
        {
            if (_firstSelectedCard.Symbol == _secondSelectedCard.Symbol)
            {
                _firstSelectedCard.IsMatched = true;
                _secondSelectedCard.IsMatched = true;
                ResetSelection();
                CheckForGameCompletion();
            }
            else
            {
                await Task.Delay(800);
                _firstSelectedCard.IsFlipped = false;
                _secondSelectedCard.IsFlipped = false;
                ResetSelection();
            }
        }

        private void ResetSelection()
        {
            _firstSelectedCard = null;
            _secondSelectedCard = null;
        }

        private void CheckForGameCompletion()
        {
            if (Cards.All(c => c.IsMatched))
            {
                IsGameCompleted = true;
                _timer.Stop();
                SaveStats();
            }
        }

        private void SaveStats()
        {
            var stats = new GameStats
            {
                PlayerName = string.IsNullOrWhiteSpace(PlayerName) ? "Unknown" : PlayerName,
                Moves = MoveCount,
                GameTime = DateTime.Now - _startTime,
                CompletedAt = DateTime.Now
            };
            _statsRepository.SaveStats(stats);
        }

        private void ShowHighScores()
        {
            var scores = _statsRepository.GetTopTenScores();
            HighScores.Clear();
            foreach (var score in scores)
            {
                HighScores.Add(score);
            }

            var highScoresView = new HighScoresWindow
            {
                DataContext = this
            };
            highScoresView.ShowDialog();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}