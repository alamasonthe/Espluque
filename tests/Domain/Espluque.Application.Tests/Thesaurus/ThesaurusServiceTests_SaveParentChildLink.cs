using Espluque.Contracts.Thesaurus;
using Moq;

namespace Espluque.Application.Tests.Thesaurus
{
    public class ThesaurusServiceTests_SaveParentChildLink
    {
        [Fact]
        public async Task ReturnsFalse_WhenParentAndChildAreSameConcept()
        {
            // Arrange
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var thesaurusSource = new Mock<IThesaurusSource>();

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                thesaurusSource.Object);

            // Act
            bool result = await service.SaveParentChildLink(10, 10);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsFalse_WhenChildIsAncestorOfParent()
        {
            var logger = new Mock<Espluque.Contracts.CrossCutting.ILogger>();
            var source = new Mock<Espluque.Contracts.Thesaurus.IThesaurusSource>();

            source
                .Setup(x => x.GetConceptPathExists(2, 1))
                .ReturnsAsync(Util.Result<bool>.Success(true));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            bool result = await service.SaveParentChildLink(1, 2);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsFalse_WhenPathCheckFails()
        {
            var logger = new Mock<Espluque.Contracts.CrossCutting.ILogger>();
            var source = new Mock<Espluque.Contracts.Thesaurus.IThesaurusSource>();

            source
                .Setup(x => x.GetConceptPathExists(2, 1))
                .ReturnsAsync(Util.Result<bool>.Failure("ERROR", "Test error"));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            bool result = await service.SaveParentChildLink(1, 2);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsFalse_WhenSaveFails()
        {
            var logger = new Mock<Espluque.Contracts.CrossCutting.ILogger>();
            var source = new Mock<Espluque.Contracts.Thesaurus.IThesaurusSource>();

            source
                .Setup(x => x.GetConceptPathExists(2, 1))
                .ReturnsAsync(Util.Result<bool>.Success(false));

            source
                .Setup(x => x.SaveParentChildLink(1, 2))
                .ReturnsAsync(Util.Result.Failure("ERROR", "Test error"));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            bool result = await service.SaveParentChildLink(1, 2);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsTrue_WhenSaveSucceeds()
        {
            var logger = new Mock<Espluque.Contracts.CrossCutting.ILogger>();
            var source = new Mock<Espluque.Contracts.Thesaurus.IThesaurusSource>();

            source
                .Setup(x => x.GetConceptPathExists(2, 1))
                .ReturnsAsync(Util.Result<bool>.Success(false));

            source
                .Setup(x => x.SaveParentChildLink(1, 2))
                .ReturnsAsync(Util.Result.Success());

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            bool result = await service.SaveParentChildLink(1, 2);

            Assert.True(result);
        }
    }
}
