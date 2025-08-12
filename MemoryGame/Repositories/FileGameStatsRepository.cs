using MemoryGame.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MemoryGame.Repositories
{
    // An implementation of IGameStatsRepository that saves statistics to a local CSV file.
    public class FileGameStatsRepository : IGameStatsRepository
    {
        // The path to the file where stats are stored.
        private readonly string _filePath;
        // The header line for the CSV file to define the columns.
        private const string Header = "PlayerName,Moves,GameTime,CompletedAt";

        // Constructor. It takes an optional file path, defaulting to "gamestats.csv".
        public FileGameStatsRepository(string filePath = "gamestats.csv")
        {
            // Creates the full path to the file in the application's base directory.
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            // Ensures that the file and its header exist before we try to use it.
            EnsureFileExists();
        }

        // Private helper method to create the file and write the header if it doesn't exist.
        private void EnsureFileExists()
        {
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, Header + Environment.NewLine);
            }
        }

        // Implements the SaveStats method from the interface.
        public void SaveStats(GameStats stats)
        {
            try
            {
                // Formats the GameStats object into a single line of comma-separated values.
                // Using specific formats for TimeSpan ('c') and DateTime ensures consistency.
                string csvLine = $"{stats.PlayerName},{stats.Moves},{stats.GameTime:c},{stats.CompletedAt:yyyy-MM-dd HH:mm:ss}";
                // Appends the new line to the end of the file.
                File.AppendAllText(_filePath, csvLine + Environment.NewLine);
            }
            catch (IOException ex)
            {
                // Basic error handling in case the file is locked or cannot be accessed.
                Console.WriteLine($"Error saving stats: {ex.Message}");
            }
        }

        // Private helper method to read all stats from the file.
        private IEnumerable<GameStats> GetAllStats()
        {
            if (!File.Exists(_filePath))
            {
                yield break; // Return an empty collection if the file doesn't exist.
            }

            // Read all lines from the file, skipping the header row.
            var lines = File.ReadAllLines(_filePath).Skip(1);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length == 4)
                {
                    // 'yield return' creates a lazy-loaded collection. A GameStats object is only created
                    // when it's actually requested by the calling code (e.g., during the .OrderBy).
                    yield return new GameStats
                    {
                        PlayerName = parts[0],
                        Moves = int.Parse(parts[1]),
                        // CultureInfo.InvariantCulture is used to ensure parsing works regardless of system's regional settings.
                        GameTime = TimeSpan.Parse(parts[2], CultureInfo.InvariantCulture),
                        CompletedAt = DateTime.ParseExact(parts[3], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                    };
                }
            }
        }

        // Implements the GetTopTenScores method from the interface.
        public IEnumerable<GameStats> GetTopTenScores()
        {
            // First, get all stats from the file.
            return GetAllStats()
                // Then, order them by the number of moves (ascending).
                .OrderBy(s => s.Moves)
                // If moves are equal, order by game time (ascending).
                .ThenBy(s => s.GameTime)
                // Finally, take only the first 10 results.
                .Take(10);
        }

        // Implements the GetAllStatsForPlayer method from the interface.
        public IEnumerable<GameStats> GetAllStatsForPlayer(string playerName)
        {
            // Get all stats and filter them to return only those where the player name matches (case-insensitive).
            return GetAllStats()
                .Where(s => s.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
        }
    }
}