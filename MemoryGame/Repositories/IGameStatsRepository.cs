using MemoryGame.Models;
using System.Collections.Generic;

namespace MemoryGame.Repositories
{
    // Defines a contract for classes that will handle the storage and retrieval of game statistics.
    public interface IGameStatsRepository
    {
        // Contract for saving a game's statistics.
        void SaveStats(GameStats stats);

        // Contract for retrieving the top ten scores, ordered by best performance (fewest moves, then shortest time).
        IEnumerable<GameStats> GetTopTenScores();

        // Contract for retrieving all game statistics for a specific player.
        IEnumerable<GameStats> GetAllStatsForPlayer(string playerName);
    }
}