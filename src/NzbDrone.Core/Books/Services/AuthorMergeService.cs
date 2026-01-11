using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Books
{
    public interface IAuthorMergeService
    {
        Author MergeAuthors(Author winner, Author loser, bool moveFiles);
    }

    public class AuthorMergeService : IAuthorMergeService
    {
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public AuthorMergeService(IAuthorService authorService,
                                  IBookService bookService,
                                  IDiskProvider diskProvider,
                                  IDiskTransferService diskTransferService,
                                  IRootFolderWatchingService rootFolderWatchingService,
                                  IEventAggregator eventAggregator,
                                  Logger logger)
        {
            _authorService = authorService;
            _bookService = bookService;
            _diskProvider = diskProvider;
            _diskTransferService = diskTransferService;
            _rootFolderWatchingService = rootFolderWatchingService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public Author MergeAuthors(Author winner, Author loser, bool moveFiles)
        {
            if (winner == null)
            {
                throw new ArgumentNullException(nameof(winner));
            }

            if (loser == null)
            {
                throw new ArgumentNullException(nameof(loser));
            }

            if (winner.Id == loser.Id)
            {
                throw new InvalidOperationException("Cannot merge an author into itself.");
            }

            _logger.Info("Merging author {0} into {1}", loser, winner);

            // lgtm [cs/user-controlled-bypass] merge move is a user-requested action, not auth bypass.
            if (moveFiles)
            {
                MoveAuthorFiles(loser, winner);
            }

            var booksToMove = _bookService.GetBooksByAuthorMetadataId(loser.AuthorMetadataId);

            if (booksToMove.Any())
            {
                foreach (var book in booksToMove)
                {
                    book.AuthorMetadataId = winner.AuthorMetadataId;
                }

                _bookService.UpdateMany(booksToMove);

                foreach (var book in booksToMove)
                {
                    var updatedBook = _bookService.GetBook(book.Id);
                    _eventAggregator.PublishEvent(new BookUpdatedEvent(updatedBook));
                }
            }

            _authorService.DeleteAuthor(loser.Id, false);

            var updatedWinner = _authorService.GetAuthor(winner.Id);
            _eventAggregator.PublishEvent(new AuthorUpdatedEvent(updatedWinner));

            return updatedWinner;
        }

        private void MoveAuthorFiles(Author loser, Author winner)
        {
            var sourcePath = loser.Path;
            var destinationPath = winner.Path;

            if (sourcePath.IsNullOrWhiteSpace() || destinationPath.IsNullOrWhiteSpace())
            {
                _logger.Warn("Author merge skipped file move because source or destination path was empty (source: {0}, destination: {1})", sourcePath, destinationPath);
                return;
            }

            if (sourcePath.PathEquals(destinationPath))
            {
                _logger.Debug("Author merge skipped file move because source and destination are identical: {0}", sourcePath);
                return;
            }

            if (!_diskProvider.FolderExists(sourcePath))
            {
                _logger.Debug("Author merge skipped file move because source folder does not exist: {0}", sourcePath);
                return;
            }

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(sourcePath, destinationPath);
            _diskTransferService.TransferFolder(sourcePath, destinationPath, TransferMode.Move);
            _eventAggregator.PublishEvent(new AuthorMovedEvent(loser, sourcePath, destinationPath));
        }
    }
}
