using MemoryGame.Models;
using System.Collections.Generic;

namespace MemoryGame.Repositories
{
    public interface IGameStatsRepository
    {
        void SaveStats(GameStats stats);
        IEnumerable<GameStats> GetTopTenScores();
        IEnumerable<GameStats> GetAllStatsForPlayer(string playerName);
    }
}