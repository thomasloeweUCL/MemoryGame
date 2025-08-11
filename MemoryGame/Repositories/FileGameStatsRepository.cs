using MemoryGame.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MemoryGame.Repositories
{
    public class FileGameStatsRepository : IGameStatsRepository
    {
        private readonly string _filePath;
        private const string Header = "PlayerName,Moves,GameTime,CompletedAt";

        public FileGameStatsRepository(string filePath = "gamestats.csv")
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            EnsureFileExists();
        }

        private void EnsureFileExists()
        {
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, Header + Environment.NewLine);
            }
        }

        public void SaveStats(GameStats stats)
        {
            try
            {
                string csvLine = $"{stats.PlayerName},{stats.Moves},{stats.GameTime:c},{stats.CompletedAt:yyyy-MM-dd HH:mm:ss}";
                File.AppendAllText(_filePath, csvLine + Environment.NewLine);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error saving stats: {ex.Message}");
            }
        }

        private IEnumerable<GameStats> GetAllStats()
        {
            if (!File.Exists(_filePath))
            {
                yield break;
            }

            var lines = File.ReadAllLines(_filePath).Skip(1);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length == 4)
                {
                    yield return new GameStats
                    {
                        PlayerName = parts[0],
                        Moves = int.Parse(parts[1]),
                        GameTime = TimeSpan.Parse(parts[2], CultureInfo.InvariantCulture),
                        CompletedAt = DateTime.ParseExact(parts[3], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                    };
                }
            }
        }

        public IEnumerable<GameStats> GetTopTenScores()
        {
            return GetAllStats()
                .OrderBy(s => s.Moves)
                .ThenBy(s => s.GameTime)
                .Take(10);
        }

        public IEnumerable<GameStats> GetAllStatsForPlayer(string playerName)
        {
            return GetAllStats()
                .Where(s => s.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
        }
    }
}