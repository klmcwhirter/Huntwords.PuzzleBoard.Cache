#pragma warning disable CS1572, CS1573, CS1591
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Huntwords.Common.Models;
using Huntwords.Common.Repositories;
using Huntwords.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Huntwords.PuzzleBoard.Cache.Services
{
    /// <summary>
    /// This class manages populating the PuzzleBoardCache by prefilling the cache via the FillQueues method
    /// and registering to the cache's events to refill the cache as it is depleted.
    /// </summary>
    public class PuzzleBoardCacheManager
    {
        /// <summary>
        /// PuzzleBoardCache instance that holds the generated PuzzleBoard instances
        /// </summary>
        /// <returns></returns>
        protected PuzzleBoardCache Cache { get; }
        /// <summary>
        /// Factory method to activate generators
        /// </summary>
        /// <remarks>
        /// See http://autofac.readthedocs.io/en/latest/resolve/relationships.html#dynamic-instantiation-func-b
        /// </remarks>
        /// <returns></returns>
        protected Func<IGenerator<Huntwords.Common.Models.PuzzleBoard>> GeneratorFactory { get; }
        /// <summary>
        /// ILogger instance for this class
        /// </summary>
        /// <returns></returns>
        protected ILogger<PuzzleBoardCacheManager> Logger { get; set; }
        /// <summary>
        /// Repository for Puzzle definitions
        /// </summary>
        /// <returns></returns>
        protected IPuzzlesRepository PuzzleRepository { get; }
        /// <summary>
        /// TaskScheduler instance to help with concurrency and affinity control
        /// </summary>
        /// <returns></returns>
        protected TaskScheduler TaskScheduler { get; }

        /// <summary>
        /// Provides CancellationToken for async methods
        /// </summary>
        protected CancellationTokenSource tokenSource;

        /// <summary>
        /// Construct a PuzzleBoardCacheManager
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="generatorFactory"></param>
        /// <param name="puzzleRepository"></param>
        /// <param name="taskScheduler"></param>
        /// <param name="logger"></param>
        public PuzzleBoardCacheManager(
            PuzzleBoardCache cache,
            Func<IGenerator<Huntwords.Common.Models.PuzzleBoard>> generatorFactory,
            IPuzzlesRepository puzzleRepository,
            TaskScheduler taskScheduler,
            ILogger<PuzzleBoardCacheManager> logger
        )
        {
            Cache = cache;
            GeneratorFactory = generatorFactory;
            PuzzleRepository = puzzleRepository;
            TaskScheduler = taskScheduler;
            Logger = logger;
        }

        /// <summary>
        /// Guard that tells whether we are in the process of filling or not
        /// </summary>
        protected bool isFilling = false;
        /// <summary>
        /// Lock around isFilling guard
        /// </summary>
        /// <returns></returns>
        protected object isFillingLock = new object();
        /// <summary>
        /// Property for isFilling that bounds usage with the isFillingLock
        /// </summary>
        /// <returns></returns>
        protected bool IsFilling
        {
            get
            {
                lock (isFillingLock)
                {
                    return isFilling;
                }
            }
            set
            {
                lock (isFillingLock)
                {
                    isFilling = value;
                }
            }
        }

        public void Initialize(bool verbose)
        {
            FillQueues(verbose);
            Cache.SubscribePopped((name) => FillQueueByName(name, false));
        }

        /// <summary>
        /// Generate a puzzle by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="verbose"></param>
        /// <returns></returns>
        public void FillQueueByName(string name, bool verbose)
        {
            Logger.LogInformation($"Received message for(name={name}, verbose={verbose}) starting");

            var puzzle = PuzzleRepository.Get(name);

            AddPuzzleBoard(puzzle, verbose);

            FillQueues(verbose);

            Cache.SubscribePopped((n) => FillQueueByName(n, false));

            Logger.LogInformation($"FillQueuesPriority({name}, {verbose}) exiting");
        }

        /// <summary>
        /// Loops over puzzles and if the cache is not full for that id generate a PuzzleBoard and place it in the cache
        /// </summary>
        /// <param name="verbose"></param>
        /// <returns></returns>
        public void FillQueues(bool verbose)
        {
            Logger.LogInformation("FillQueues starting");

            if (!IsFilling)
            {
                IsFilling = true;
                tokenSource = new CancellationTokenSource();

                do
                {
                    Logger.LogInformation($"Populating Cache...");

                    var actions = new List<Action>();

                    foreach (var puzzle in PuzzleRepository.GetAll().ToList())
                    {
                        if (!Cache.CacheFull(puzzle.Name))
                        {
                            Logger.LogInformation($"Cache is not full for puzzle.Name={puzzle.Name}");
                            actions.Add(() => AddPuzzleBoard(puzzle, verbose));
                        }
                    }

                    var pOpts = new ParallelOptions
                    {
                        CancellationToken = tokenSource.Token,
                        MaxDegreeOfParallelism = TaskScheduler.MaximumConcurrencyLevel,
                        TaskScheduler = TaskScheduler
                    };
                    Parallel.Invoke(pOpts, actions.ToArray());

                } while (PuzzleRepository.GetAll().Any((p) => !Cache.CacheFull(p.Name)));

                IsFilling = false;
            }
            else
            {
                Logger.LogInformation("FillQueues already filling");
            }

            Logger.LogInformation("FillQueues exiting");
        }

        /// <summary>
        /// Generate a PuzzleBoard and add it to the cache
        /// </summary>
        /// <param name="puzzle"></param>
        /// <param name="verbose"></param>
        protected void AddPuzzleBoard(Puzzle puzzle, bool verbose)
        {
            Logger.LogInformation($"Replenishing cache for puzzle.Name={puzzle.Name}");
            var generator = GeneratorFactory();
            var board = generator.Generate(puzzle, verbose);
            Cache.Enqueue(board);
            Logger.LogInformation($"Replenishing cache for puzzle.Name={puzzle.Name} - DONE.");
        }
    }
}
