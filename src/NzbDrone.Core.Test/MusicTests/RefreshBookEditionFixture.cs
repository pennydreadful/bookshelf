using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class RefreshBookEditionFixture : CoreTest<RefreshBookService>
    {
        [TestCase(true)]
        [TestCase(false)]
        public void should_preserve_missing_manual_or_owned_edition(bool manual)
        {
            var edition = new Edition { Id = 10, ForeignEditionId = "14496460", ManualAdd = manual, Monitored = true };
            Mocker.GetMock<IEditionService>().Setup(x => x.GetEditionsByBook(1)).Returns(new List<Edition> { edition });
            Mocker.GetMock<IMediaFileService>().Setup(x => x.GetFilesByEdition(It.IsAny<int>()))
                .Returns(manual ? new List<BookFile>() : new List<BookFile> { new BookFile() });
            var remote = new Book { Editions = new List<Edition>() };

            var method = typeof(RefreshBookService).GetMethod("GetRemoteChildren", BindingFlags.Instance | BindingFlags.NonPublic);
            var children = (List<Edition>)method.Invoke(Subject, new object[] { new Book { Id = 1 }, remote });

            children.Should().ContainSingle().Which.Should().BeSameAs(edition);
            edition.Monitored.Should().BeTrue();
        }
    }
}
