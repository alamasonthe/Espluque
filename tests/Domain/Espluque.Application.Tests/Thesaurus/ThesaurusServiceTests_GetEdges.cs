using Espluque.Contracts.Thesaurus;
using Moq;
using Util;

namespace Espluque.Application.Tests.Thesaurus
{
    public class ThesaurusServiceTests_GetEdges
    {
        [Fact]
        public async Task ReturnsAncestorAndDescendantEdges_WithRelations()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            source
                .Setup(x => x.GetAncestorLinks(10))
                .ReturnsAsync(
                    Result<List<(int ParentConceptId, int ChildConceptId)>>.Success(
                        [(1, 5), (5, 10)]));

            source
                .Setup(x => x.GetDescendantLinks(10))
                .ReturnsAsync(
                    Result<List<(int ParentConceptId, int ChildConceptId)>>.Success(
                        [(10, 20), (20, 30)]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            var result = await service.GetEdges(10);

            Assert.NotNull(result);
            Assert.Equal(4, result.Count);

            Assert.Contains(
                result,
                edge =>
                    edge.ParentConceptId == 1 &&
                    edge.ChildConceptId == 5 &&
                    edge.Relation == "Ancestor");

            Assert.Contains(
                result,
                edge =>
                    edge.ParentConceptId == 5 &&
                    edge.ChildConceptId == 10 &&
                    edge.Relation == "Ancestor");

            Assert.Contains(
                result,
                edge =>
                    edge.ParentConceptId == 10 &&
                    edge.ChildConceptId == 20 &&
                    edge.Relation == "Descendant");

            Assert.Contains(
                result,
                edge =>
                    edge.ParentConceptId == 20 &&
                    edge.ChildConceptId == 30 &&
                    edge.Relation == "Descendant");
        }

        [Fact]
        public async Task ReturnsNull_WhenSourceLookupFails()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            source
                .Setup(x => x.GetAncestorLinks(10))
                .ReturnsAsync(
                    Result<List<(int ParentConceptId, int ChildConceptId)>>.Failure(
                        "ERROR",
                        "Test error"));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            var result = await service.GetEdges(10);

            Assert.Null(result);
        }
    }
}