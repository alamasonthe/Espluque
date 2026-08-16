using Espluque.Contracts.Thesaurus;
using Moq;
using Util;

namespace Espluque.Application.Tests.Thesaurus
{
    public class ThesaurusServiceTests_GetReferenceTerms
    {
        [Fact]
        public async Task RequestsReferenceTerms()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            source
                .Setup(x => x.GetReferenceTerms("PRONOM", "Reference"))
                .ReturnsAsync(
                    Result<List<IReferenceTerm>>.Success([]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            await service.GetReferenceTerms("PRONOM");

            source.Verify(
                x => x.GetReferenceTerms("PRONOM", "Reference"),
                Times.Once);
        }
    }
}