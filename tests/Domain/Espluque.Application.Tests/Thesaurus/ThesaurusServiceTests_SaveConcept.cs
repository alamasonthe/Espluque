using Espluque.Contracts.Thesaurus;
using Moq;
using Util;

namespace Espluque.Application.Tests.Thesaurus
{
    public class ThesaurusServiceTests_SaveConcept
    {
        [Fact]
        public async Task NormalizesTerms_BeforeSaving()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            var term = new Mock<IThesaurusTerm>();
            term.SetupGet(x => x.Term).Returns("  éléphant / rouge  ");
            term.SetupGet(x => x.IsPreferred).Returns(true);
            term.SetupProperty(x => x.NormalizedTerm);

            var concept = new Mock<IThesaurusConcept>();
            concept.SetupGet(x => x.Terms).Returns([term.Object]);

            source
                .Setup(x => x.SaveConcept(concept.Object))
                .ReturnsAsync(Result<int>.Success(42));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            await service.SaveConcept(concept.Object);

            Assert.Equal("ElephantRouge", term.Object.NormalizedTerm);
        }

        [Fact]
        public async Task ReturnsId_WhenSaveSucceeds()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            var concept = CreateConcept();

            source
                .Setup(x => x.SaveConcept(concept.Object))
                .ReturnsAsync(Result<int>.Success(42));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            int? result = await service.SaveConcept(concept.Object);

            Assert.Equal(42, result);
        }

        [Fact]
        public async Task ReturnsNull_WhenSaveFails()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            var concept = CreateConcept();

            source
                .Setup(x => x.SaveConcept(concept.Object))
                .ReturnsAsync(Result<int>.Failure("ERROR", "Test error"));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            int? result = await service.SaveConcept(concept.Object);

            Assert.Null(result);
        }

        private static Mock<IThesaurusConcept> CreateConcept()
        {
            var term = new Mock<IThesaurusTerm>();
            term.SetupGet(x => x.Term).Returns("Test");
            term.SetupGet(x => x.IsPreferred).Returns(true);
            term.SetupProperty(x => x.NormalizedTerm);

            var concept = new Mock<IThesaurusConcept>();
            concept.SetupGet(x => x.Terms).Returns([term.Object]);

            return concept;
        }
    }
}