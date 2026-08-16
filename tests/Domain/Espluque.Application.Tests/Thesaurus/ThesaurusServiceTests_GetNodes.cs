using Espluque.Contracts.Thesaurus;
using Moq;
using Util;

namespace Espluque.Application.Tests.Thesaurus
{
    public class ThesaurusServiceTests_GetNodes
    {
        [Fact]
        public async Task ReturnsAncestorsSelectedAndDescendants()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            var preferredTerm = new Mock<IThesaurusTerm>();
            preferredTerm.SetupGet(x => x.IsPreferred).Returns(true);
            preferredTerm.SetupGet(x => x.Term).Returns("Selected");

            var selectedConcept = new Mock<IThesaurusConcept>();
            selectedConcept.SetupGet(x => x.Id).Returns(10);
            selectedConcept.SetupGet(x => x.Terms).Returns([preferredTerm.Object]);

            source
                .Setup(x => x.GetAncestorRefs(10))
                .ReturnsAsync(
                    Result<List<(int ConceptId, string MainTerm)>>.Success(
                        [(1, "Ancestor")]));

            source
                .Setup(x => x.GetConceptById(10))
                .ReturnsAsync(
                    Result<IThesaurusConcept>.Success(
                        selectedConcept.Object));

            source
                .Setup(x => x.GetDescendantRefs(10))
                .ReturnsAsync(
                    Result<List<(int ConceptId, string MainTerm)>>.Success(
                        [(20, "Descendant")]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            var result = await service.GetNodes(10);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            Assert.Contains(
                result,
                node =>
                    node.ConceptId == 1 &&
                    node.MainTerm == "Ancestor" &&
                    node.Relation == "Ancestor");

            Assert.Contains(
                result,
                node =>
                    node.ConceptId == 10 &&
                    node.MainTerm == "Selected" &&
                    node.Relation == "Selected");

            Assert.Contains(
                result,
                node =>
                    node.ConceptId == 20 &&
                    node.MainTerm == "Descendant" &&
                    node.Relation == "Descendant");
        }

        [Fact]
        public async Task RemovesDuplicateConcepts_WithSelectedPriority()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            var preferredTerm = new Mock<IThesaurusTerm>();
            preferredTerm.SetupGet(x => x.IsPreferred).Returns(true);
            preferredTerm.SetupGet(x => x.Term).Returns("Selected");

            var selectedConcept = new Mock<IThesaurusConcept>();
            selectedConcept.SetupGet(x => x.Id).Returns(10);
            selectedConcept.SetupGet(x => x.Terms).Returns([preferredTerm.Object]);

            source
                .Setup(x => x.GetAncestorRefs(10))
                .ReturnsAsync(
                    Result<List<(int ConceptId, string MainTerm)>>.Success(
                        [(10, "Ancestor version")]));

            source
                .Setup(x => x.GetConceptById(10))
                .ReturnsAsync(
                    Result<IThesaurusConcept>.Success(
                        selectedConcept.Object));

            source
                .Setup(x => x.GetDescendantRefs(10))
                .ReturnsAsync(
                    Result<List<(int ConceptId, string MainTerm)>>.Success(
                        [(10, "Descendant version")]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            var result = await service.GetNodes(10);

            Assert.NotNull(result);

            var node = Assert.Single(result);

            Assert.Equal(10, node.ConceptId);
            Assert.Equal("Selected", node.MainTerm);
            Assert.Equal("Selected", node.Relation);
        }

        [Fact]
        public async Task ReturnsNull_WhenSourceLookupFails()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            source
                .Setup(x => x.GetAncestorRefs(10))
                .ReturnsAsync(
                    Result<List<(int ConceptId, string MainTerm)>>.Failure(
                        "ERROR",
                        "Test error"));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            var result = await service.GetNodes(10);

            Assert.Null(result);
        }
    }
}