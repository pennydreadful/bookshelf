using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class UpgradeMediaFileServiceFixture : CoreTest<UpgradeMediaFileService>
    {
        private BookFile _trackFile;
        private LocalBook _localTrack;
        private string _rootPath = @"C:\Test\Music\Author".AsOsAgnostic();

        [SetUp]
        public void Setup()
        {
            _localTrack = new LocalBook();
            _localTrack.Author = new Author
            {
                Path = _rootPath
            };

            _trackFile = Builder<BookFile>
                .CreateNew()
                .Build();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderExists(It.IsAny<string>()))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                 .Setup(c => c.GetParentFolder(It.IsAny<string>()))
                 .Returns<string>(c => Path.GetDirectoryName(c));

            Mocker.GetMock<IRootFolderService>()
                .Setup(c => c.GetBestRootFolder(It.IsAny<string>()))
                .Returns(new RootFolder());
        }

        private void GivenSingleTrackWithSingleTrackFile()
        {
            _localTrack.Book = Builder<Book>.CreateNew()
                .With(e => e.BookFiles = new LazyLoaded<List<BookFile>>(
                          new List<BookFile>
                          {
                              new BookFile
                              {
                                  Id = 1,
                                  Path = Path.Combine(_rootPath, @"Season 01\30.rock.s01e01.avi"),
                              }
                          }))
                .Build();
        }

        [Test]
        public void should_delete_single_track_file_once()
        {
            GivenSingleTrackWithSingleTrackFile();

            Subject.UpgradeBookFile(_trackFile, _localTrack);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_delete_track_file_from_database()
        {
            GivenSingleTrackWithSingleTrackFile();

            Subject.UpgradeBookFile(_trackFile, _localTrack);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(It.IsAny<BookFile>(), DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_delete_existing_file_fromdb_if_file_doesnt_exist()
        {
            GivenSingleTrackWithSingleTrackFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeBookFile(_trackFile, _localTrack);

            // Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localTrack.Book.BookFiles.Value, DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_not_try_to_recyclebin_existing_file_if_file_doesnt_exist()
        {
            GivenSingleTrackWithSingleTrackFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeBookFile(_trackFile, _localTrack);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_return_old_track_file_in_oldFiles()
        {
            GivenSingleTrackWithSingleTrackFile();

            Subject.UpgradeBookFile(_trackFile, _localTrack).OldFiles.Count.Should().Be(1);
        }

        [Test]
        [Ignore("Pending readarr fix")]
        public void should_import_if_existing_file_doesnt_exist_in_db()
        {
            _localTrack.Book = Builder<Book>.CreateNew()
                .With(e => e.BookFiles = new LazyLoaded<List<BookFile>>())
                .Build();

            Subject.UpgradeBookFile(_trackFile, _localTrack);

            // Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localTrack.Book.BookFiles.Value, It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }

        private void GivenDestinationFolder(string folder)
        {
            Mocker.GetMock<IBuildFileNames>()
                .Setup(c => c.BuildBookFileName(It.IsAny<Author>(), It.IsAny<Edition>(), It.IsAny<BookFile>(), null, null))
                .Returns("book");

            Mocker.GetMock<IBuildFileNames>()
                .Setup(c => c.BuildBookFilePath(It.IsAny<Author>(), It.IsAny<Edition>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Path.Combine(folder, "book.azw3"));
        }

        private void GivenExistingFileAt(string path)
        {
            _localTrack.Book = Builder<Book>.CreateNew()
                .With(e => e.BookFiles = new LazyLoaded<List<BookFile>>(
                          new List<BookFile>
                          {
                              new BookFile { Id = 1, Path = path }
                          }))
                .Build();
        }

        [Test]
        public void should_not_delete_existing_file_from_a_different_book_folder()
        {
            // Regression: a file mis-matched to this book, living in another book's folder, was
            // deleted as an "upgrade", destroying an unrelated book.
            GivenExistingFileAt(Path.Combine(_rootPath, "Twisted Lies", "Twisted Lies.azw3"));
            GivenDestinationFolder(Path.Combine(_rootPath, "Twisted Games"));

            Subject.UpgradeBookFile(_trackFile, _localTrack);

            Mocker.GetMock<IRecycleBinProvider>()
                .Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            Mocker.GetMock<IMediaFileService>()
                .Verify(v => v.Delete(It.IsAny<BookFile>(), DeleteMediaFileReason.Upgrade), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_detach_but_not_delete_file_from_a_different_book_folder()
        {
            GivenExistingFileAt(Path.Combine(_rootPath, "Twisted Lies", "Twisted Lies.azw3"));
            GivenDestinationFolder(Path.Combine(_rootPath, "Twisted Games"));

            Subject.UpgradeBookFile(_trackFile, _localTrack);

            Mocker.GetMock<IMediaFileService>()
                .Verify(v => v.Delete(It.IsAny<BookFile>(), DeleteMediaFileReason.MissingFromDisk), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_still_delete_existing_file_in_the_destination_folder()
        {
            var folder = Path.Combine(_rootPath, "Twisted Games");
            GivenExistingFileAt(Path.Combine(folder, "old.azw3"));
            GivenDestinationFolder(folder);

            Subject.UpgradeBookFile(_trackFile, _localTrack);

            Mocker.GetMock<IRecycleBinProvider>()
                .Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once());

            Mocker.GetMock<IMediaFileService>()
                .Verify(v => v.Delete(It.IsAny<BookFile>(), DeleteMediaFileReason.Upgrade), Times.Once());
        }
    }
}
