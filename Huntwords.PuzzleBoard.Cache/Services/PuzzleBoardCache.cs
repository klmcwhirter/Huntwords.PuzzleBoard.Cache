#pragma warning disable CS1572, CS1573, CS1591
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Huntwords.Common.Models;
using Huntwords.Common.Repositories;
using Huntwords.Common.Services;

namespace Huntwords.PuzzleBoard.Cache.Services
{
    public class PuzzleBoardCache
    {
        protected IPuzzleBoardRepository PuzzleBoardRepository { get; }
        public IRedisPuzzleBoardPubSubService PubSubService { get; }
        protected ILogger<PuzzleBoardCache> Logger { get; }
        protected PuzzleBoardGeneratorOptions Options { get; }

        public PuzzleBoardCache(
            IOptions<PuzzleBoardGeneratorOptions> options,
            IPuzzleBoardRepository puzzleBoardRepository,
            IRedisPuzzleBoardPubSubService pubSubService,
            ILogger<PuzzleBoardCache> logger
        )
        {
            Options = options.Value;
            PuzzleBoardRepository = puzzleBoardRepository;
            PubSubService = pubSubService;
            Logger = logger;
        }

        public bool CacheFull(string name)
        {
            var rc = PuzzleBoardRepository.Length(name) >= Options.CacheSize;
            return rc;
        }

        public void Enqueue(Huntwords.Common.Models.PuzzleBoard board)
        {
            var name = board.Puzzle.Name;

            if (PuzzleBoardRepository.Length(name) < Options.CacheSize)
            {
                PuzzleBoardRepository.Push(board);
            }
        }

        public Huntwords.Common.Models.PuzzleBoard Dequeue(string name)
        {
            Huntwords.Common.Models.PuzzleBoard rc = null;
            if (PuzzleBoardRepository.Length(name) > 0)
            {
                rc = PuzzleBoardRepository.Pop(name);
            }

            return rc;
        }

        public void SubscribePopped(Action<string> poppedHandler)
        {
            PubSubService.SubscribePopped(poppedHandler);
        }
    }
}