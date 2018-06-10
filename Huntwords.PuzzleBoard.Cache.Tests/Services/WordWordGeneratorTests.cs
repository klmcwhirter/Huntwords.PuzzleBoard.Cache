using Huntwords.Common.Services;
using Xunit;

namespace Huntwords.PuzzleBoard.Cache.Tests.Services
{
    public class WordWordGeneratorTests
    {
        [Fact]
        public void CanConstruct()
        {
            new WordWordGenerator();
        }

        [Fact]
        public void Ctor_SetsPuzzle()
        {
            var rc = new WordWordGenerator();
            
            Assert.NotNull(rc.Puzzle);
            Assert.Equal(WordGeneratorsNamesProvider.Word, rc.Puzzle.Name);
        }

        [Fact]
        public void Generate_ReturnsWord()
        {
            var g = new WordWordGenerator();

            var rc = g.Generate();

            Assert.NotNull(rc);
            Assert.Equal("word", rc);
        }
    }
}
