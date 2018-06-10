#pragma warning disable CS1572, CS1573, CS1591
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Huntwords.Common.Models;
using Huntwords.Common.Repositories;
using Huntwords.Common.Services;
using Huntwords.Common.Utils;
using Microsoft.Extensions.Logging;

namespace Huntwords.PuzzleBoard.Cache.Services
{
    [DataContract]
    public class PuzzleWordGenerator : IGenerator<string>
    {
        protected ILogger<PuzzleWordGenerator> Logger { get; }
        protected IPuzzlesRepository Repository { get; }

        public PuzzleWordGenerator(IPuzzlesRepository repository, ILogger<PuzzleWordGenerator> logger)
        {
            Repository = repository;
            Logger = logger;
        }

        protected int CurrentIdx { get; set; } = 0;
        protected ISet<string> Seen { get; set; }

        public string Generate(params object[] options)
        {
            var puzzle = (Puzzle)options[0];

            string rc;
            do
            {
                // If the word list is exhausted, start over
                if (Seen == null || Seen.Count >= puzzle.PuzzleWords.Count)
                {
                    ResetSeen();
                }

                var idx = puzzle.PuzzleWords.Count.Random();
                rc = puzzle.PuzzleWords.Skip(idx).FirstOrDefault();
            } while (Seen.Contains(rc));

            Seen.Add(rc);

            return rc;
        }

        protected void ResetSeen()
        {
            Seen = new SortedSet<string>(Comparer<string>.Create(
                        (e1, e2) => e1.ToUpperInvariant().CompareTo(e2.ToUpperInvariant())
                    ));
        }
    }
}
