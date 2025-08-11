using MemoryGame.ViewModels; // Vigtig using-statement
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MemoryGame.Tests
{
    [TestClass]
    public class GameViewModelTests
    {
        [TestMethod]
        public void NewGame_ShouldCreate16Cards()
        {
            // Arrange
            var vm = new GameViewModel();

            // Act: Kør NewGame-kommandoen
            vm.NewGameCommand.Execute(null);

            // Assert
            Assert.AreEqual(16, vm.Cards.Count, "Der skal være 16 kort efter et nyt spil.");
        }

        [TestMethod]
        public void FlipCard_FirstCard_ShouldFlipCard()
        {
            // Arrange
            var vm = new GameViewModel();
            var firstCard = vm.Cards.First();

            // Act: Kør FlipCard-kommandoen på det første kort
            vm.FlipCardCommand.Execute(firstCard);

            // Assert
            Assert.IsTrue(firstCard.IsFlipped, "Kortet burde være vendt.");
        }
    }
}