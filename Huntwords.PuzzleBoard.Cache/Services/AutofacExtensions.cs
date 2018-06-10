#pragma warning disable CS1572, CS1573, CS1591
using Autofac;
using Huntwords.Common.Services;

namespace Huntwords.PuzzleBoard.Cache.Services
{
    public static class AutofacExtensions
    {
        public static ContainerBuilder RegisterServices(this ContainerBuilder builder)
        {
            builder.RegisterType<PuzzleBoardGenerator>().As<IGenerator<Huntwords.Common.Models.PuzzleBoard>>();

            builder.RegisterType<PuzzleWordGenerator>().Named<IGenerator<string>>(WordGeneratorsNamesProvider.Cached);
            builder.RegisterType<RandomWordGenerator>().Named<IGenerator<string>>(WordGeneratorsNamesProvider.Random);
            builder.RegisterType<WordWordGenerator>().Named<IGenerator<string>>(WordGeneratorsNamesProvider.Word);

            return builder;
        }
    }
}