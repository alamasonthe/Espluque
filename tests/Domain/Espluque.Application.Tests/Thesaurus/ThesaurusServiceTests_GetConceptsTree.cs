using Espluque.Contracts.Thesaurus;
using Moq;
using Util;

namespace Espluque.Application.Tests.Thesaurus
{
    public class ThesaurusServiceTests_GetConceptsTree
    {
        [Fact]
        public async Task ReturnsTree_WhenConceptGraphIsValid()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            var rootTerm = CreatePreferredTerm("Root");
            var childTerm = CreatePreferredTerm("Child");

            var root = new Mock<IThesaurusConcept>();
            var child = new Mock<IThesaurusConcept>();

            root.SetupGet(x => x.Id).Returns(1);
            root.SetupGet(x => x.Parents).Returns([]);
            root.SetupGet(x => x.Children).Returns([child.Object]);
            root.SetupGet(x => x.Terms).Returns([rootTerm.Object]);

            child.SetupGet(x => x.Id).Returns(2);
            child.SetupGet(x => x.Parents).Returns([root.Object]);
            child.SetupGet(x => x.Children).Returns([]);
            child.SetupGet(x => x.Terms).Returns([childTerm.Object]);

            source
                .Setup(x => x.GetConcepts())
                .ReturnsAsync(Result<List<IThesaurusConcept>>.Success(
                    [root.Object, child.Object]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            TreeNode<IThesaurusConcept>? result =
                await service.GetConceptsTree();

            Assert.NotNull(result);
            Assert.Equal("Thesaurus", result.Name);

            var rootNode = Assert.Single(result.Children);
            Assert.Equal("Root", rootNode.Name);
            Assert.Same(root.Object, rootNode.Data);

            var childNode = Assert.Single(rootNode.Children);
            Assert.Equal("Child", childNode.Name);
            Assert.Same(child.Object, childNode.Data);
        }


        [Fact]
        public async Task ReturnsNull_WhenConceptGraphContainsLoop()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            var rootTerm = CreatePreferredTerm("Root");
            var childTerm = CreatePreferredTerm("Child");

            var root = new Mock<IThesaurusConcept>();
            var child = new Mock<IThesaurusConcept>();

            root.SetupGet(x => x.Id).Returns(1);
            root.SetupGet(x => x.Parents).Returns([]);
            root.SetupGet(x => x.Children).Returns([child.Object]);
            root.SetupGet(x => x.Terms).Returns([rootTerm.Object]);

            child.SetupGet(x => x.Id).Returns(2);
            child.SetupGet(x => x.Parents).Returns([root.Object]);
            child.SetupGet(x => x.Children).Returns([root.Object]);
            child.SetupGet(x => x.Terms).Returns([childTerm.Object]);

            source
                .Setup(x => x.GetConcepts())
                .ReturnsAsync(Result<List<IThesaurusConcept>>.Success(
                    [root.Object, child.Object]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            TreeNode<IThesaurusConcept>? result =
                await service.GetConceptsTree();

            Assert.Null(result);
        }


        [Fact]
        public async Task ReturnsNull_WhenConceptHasNoPreferredNormalizedTerm()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            var term = new Mock<IThesaurusTerm>();

            term.SetupGet(x => x.IsPreferred).Returns(true);
            term.SetupGet(x => x.NormalizedTerm).Returns(string.Empty);

            var concept = new Mock<IThesaurusConcept>();

            concept.SetupGet(x => x.Id).Returns(1);
            concept.SetupGet(x => x.Parents).Returns([]);
            concept.SetupGet(x => x.Children).Returns([]);
            concept.SetupGet(x => x.Terms).Returns([term.Object]);

            source
                .Setup(x => x.GetConcepts())
                .ReturnsAsync(Result<List<IThesaurusConcept>>.Success(
                    [concept.Object]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            TreeNode<IThesaurusConcept>? result =
                await service.GetConceptsTree();

            Assert.Null(result);
        }


        [Fact]
        public async Task ReturnsNull_WhenConceptHasNoId()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();

            var term = CreatePreferredTerm("Root");

            var concept = new Mock<IThesaurusConcept>();

            concept.SetupGet(x => x.Id).Returns((int?)null);
            concept.SetupGet(x => x.Parents).Returns([]);
            concept.SetupGet(x => x.Children).Returns([]);
            concept.SetupGet(x => x.Terms).Returns([term.Object]);

            source
                .Setup(x => x.GetConcepts())
                .ReturnsAsync(Result<List<IThesaurusConcept>>.Success(
                    [concept.Object]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            TreeNode<IThesaurusConcept>? result =
                await service.GetConceptsTree();

            Assert.Null(result);
        }


        private static Mock<IThesaurusTerm> CreatePreferredTerm(
            string normalizedTerm)
        {
            var term = new Mock<IThesaurusTerm>();

            term.SetupGet(x => x.IsPreferred).Returns(true);
            term.SetupGet(x => x.NormalizedTerm).Returns(normalizedTerm);

            return term;
        }
    }
}