#pragma warning disable CS1572, CS1573, CS1591
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Huntwords.Common.Models;
using Huntwords.Common.Repositories;
using Huntwords.Common.Utils;

namespace Huntwords.Common.Services
{
    [DataContract]
    public class RandomWordGenerator : IGenerator<string>
    {
        public Puzzle Puzzle { get; }

        protected IWordsRepository WordRepository { get; }

        public RandomWordGenerator(IWordsRepository wordRepository)
        {
            WordRepository = wordRepository;
            Puzzle = new Puzzle
            {
                Name = WordGeneratorsNamesProvider.Random,
                Description = "Puzzle containing a randomly selected list of words",
                PuzzleWords = new List<string>()
            };
        }

        public string Generate(params object[] options)
        {
            var idx = WordRepository.WordCount.Random();
            var rc = WordRepository.Get(idx);

            Puzzle.PuzzleWords.Add(rc);

            return rc;
        }
    }
}
