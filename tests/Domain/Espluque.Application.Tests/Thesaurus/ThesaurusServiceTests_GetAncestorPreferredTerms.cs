using Espluque.Contracts.Contributions;
using Espluque.Contracts.Thesaurus;
using Moq;
using Util;

namespace Espluque.Application.Tests.Thesaurus
{
    public class ThesaurusServiceTests_GetAncestorPreferredTerms
    {
        [Fact]
        public async Task ReturnsReferenceAncestors_WhenReferenceMatchExists()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();
            var fileFormat = new Mock<IFileFormat>();

            fileFormat.SetupGet(x => x.Referentiel).Returns("PRONOM");
            fileFormat.SetupGet(x => x.Label).Returns("fmt/18");
            fileFormat.SetupGet(x => x.MIMEType).Returns("application/pdf");

            source
                .Setup(x => x.GetAncestorPreferredTerms("PRONOM", "fmt/18"))
                .ReturnsAsync(Result<List<string>?>.Success(
                    ["PDF", "Document"]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            List<string>? result =
                await service.GetAncestorPreferredTerms(fileFormat.Object);

            Assert.Equal(["PDF", "Document"], result);

            source.Verify(
                x => x.GetAncestorPreferredTerms(
                    "MIMEType",
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task FallsBackToMimeType_WhenReferenceMatchIsEmpty()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();
            var fileFormat = new Mock<IFileFormat>();

            fileFormat.SetupGet(x => x.Referentiel).Returns("PRONOM");
            fileFormat.SetupGet(x => x.Label).Returns("fmt/18");
            fileFormat.SetupGet(x => x.MIMEType).Returns("application/pdf");

            source
                .Setup(x => x.GetAncestorPreferredTerms("PRONOM", "fmt/18"))
                .ReturnsAsync(Result<List<string>?>.Success([]));

            source
                .Setup(x => x.GetAncestorPreferredTerms(
                    "MIMEType",
                    "application/pdf"))
                .ReturnsAsync(Result<List<string>?>.Success(
                    ["PDF", "Document"]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            List<string>? result =
                await service.GetAncestorPreferredTerms(fileFormat.Object);

            Assert.Equal(["PDF", "Document"], result);
        }

        [Fact]
        public async Task ReturnsNull_WhenNoReferenceMatchAndMimeTypeIsMissing()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();
            var fileFormat = new Mock<IFileFormat>();

            fileFormat.SetupGet(x => x.Referentiel).Returns("PRONOM");
            fileFormat.SetupGet(x => x.Label).Returns("fmt/18");
            fileFormat.SetupGet(x => x.MIMEType).Returns((string?)null);

            source
                .Setup(x => x.GetAncestorPreferredTerms("PRONOM", "fmt/18"))
                .ReturnsAsync(Result<List<string>?>.Success([]));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            List<string>? result =
                await service.GetAncestorPreferredTerms(fileFormat.Object);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsNull_WhenReferenceLookupFails()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();
            var fileFormat = new Mock<IFileFormat>();

            fileFormat.SetupGet(x => x.Referentiel).Returns("PRONOM");
            fileFormat.SetupGet(x => x.Label).Returns("fmt/18");
            fileFormat.SetupGet(x => x.MIMEType).Returns("application/pdf");

            source
                .Setup(x => x.GetAncestorPreferredTerms("PRONOM", "fmt/18"))
                .ReturnsAsync(
                    Result<List<string>?>.Failure(
                        "ERROR",
                        "Test error"));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            List<string>? result =
                await service.GetAncestorPreferredTerms(fileFormat.Object);

            Assert.Null(result);

            source.Verify(
                x => x.GetAncestorPreferredTerms(
                    "MIMEType",
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ReturnsNull_WhenMimeLookupFails()
        {
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var source = new Mock<IThesaurusSource>();
            var fileFormat = new Mock<IFileFormat>();

            fileFormat.SetupGet(x => x.Referentiel).Returns("PRONOM");
            fileFormat.SetupGet(x => x.Label).Returns("fmt/18");
            fileFormat.SetupGet(x => x.MIMEType).Returns("application/pdf");

            source
                .Setup(x => x.GetAncestorPreferredTerms("PRONOM", "fmt/18"))
                .ReturnsAsync(Result<List<string>?>.Success([]));

            source
                .Setup(x => x.GetAncestorPreferredTerms(
                    "MIMEType",
                    "application/pdf"))
                .ReturnsAsync(
                    Result<List<string>?>.Failure(
                        "ERROR",
                        "Test error"));

            var service = new Espluque.Application.Thesaurus.ThesaurusService(
                logger.Object,
                source.Object);

            List<string>? result =
                await service.GetAncestorPreferredTerms(fileFormat.Object);

            Assert.Null(result);
        }
    }
}