using Espluque.Contracts.Thesaurus;
using Moq;
using Util;

namespace Espluque.Application.Tests.Thesaurus
{
    public class ThesaurusServiceTests_GetAlternateTerms
    {
        [Fact]
        public async Task RequestsAlternateTerms()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            source
                .Setup(x => x.GetReferenceTerms("PRONOM", "Alternate"))
                .ReturnsAsync(
                    Result<List<IReferenceTerm>>.Success([]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            await service.GetAlternateTerms("PRONOM");

            source.Verify(
                x => x.GetReferenceTerms("PRONOM", "Alternate"),
                Times.Once);
        }
    }
}