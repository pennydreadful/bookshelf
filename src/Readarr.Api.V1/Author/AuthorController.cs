using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.AuthorStats;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Http.REST.Attributes;
using NzbDrone.SignalR;
using Readarr.Http;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Author
{
    [V1ApiController]
    public class AuthorController : RestControllerWithSignalR<AuthorResource, NzbDrone.Core.Books.Author>,
                                IHandle<BookImportedEvent>,
                                IHandle<BookEditedEvent>,
                                IHandle<BookFileDeletedEvent>,
                                IHandle<AuthorAddedEvent>,
                                IHandle<AuthorUpdatedEvent>,
                                IHandle<AuthorEditedEvent>,
                                IHandle<AuthorDeletedEvent>,
                                IHandle<AuthorRenamedEvent>,
                                IHandle<MediaCoversUpdatedEvent>
    {
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IAddAuthorService _addAuthorService;
        private readonly IAuthorStatisticsService _authorStatisticsService;
        private readonly IAuthorExtraMetadataProvider _authorExtraMetadataProvider;
        private readonly IAuthorMetadataService _authorMetadataService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IRootFolderService _rootFolderService;

        public AuthorController(IBroadcastSignalRMessage signalRBroadcaster,
                            IAuthorService authorService,
                            IBookService bookService,
                            IAddAuthorService addAuthorService,
                            IAuthorStatisticsService authorStatisticsService,
                            IAuthorExtraMetadataProvider authorExtraMetadataProvider,
                            IAuthorMetadataService authorMetadataService,
                            IMapCoversToLocal coverMapper,
                            IManageCommandQueue commandQueueManager,
                            IRootFolderService rootFolderService,
                            RecycleBinValidator recycleBinValidator,
                            RootFolderValidator rootFolderValidator,
                            MappedNetworkDriveValidator mappedNetworkDriveValidator,
                            AuthorPathValidator authorPathValidator,
                            AuthorExistsValidator authorExistsValidator,
                            AuthorAncestorValidator authorAncestorValidator,
                            SystemFolderValidator systemFolderValidator,
                            QualityProfileExistsValidator qualityProfileExistsValidator,
                            MetadataProfileExistsValidator metadataProfileExistsValidator,
                            AuthorFolderAsRootFolderValidator authorFolderAsRootFolderValidator)
            : base(signalRBroadcaster)
        {
            _authorService = authorService;
            _bookService = bookService;
            _addAuthorService = addAuthorService;
            _authorStatisticsService = authorStatisticsService;
            _authorExtraMetadataProvider = authorExtraMetadataProvider;
            _authorMetadataService = authorMetadataService;

            _coverMapper = coverMapper;
            _commandQueueManager = commandQueueManager;
            _rootFolderService = rootFolderService;

            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.QualityProfileId));
            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.MetadataProfileId));

            SharedValidator.RuleFor(s => s.Path)
                           .Cascade(CascadeMode.Stop)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(authorPathValidator)
                           .SetValidator(authorAncestorValidator)
                           .SetValidator(recycleBinValidator)
                           .SetValidator(systemFolderValidator)
                           .When(s => !s.Path.IsNullOrWhiteSpace());

            SharedValidator.RuleFor(s => s.QualityProfileId).SetValidator(qualityProfileExistsValidator);
            SharedValidator.RuleFor(s => s.MetadataProfileId).SetValidator(metadataProfileExistsValidator);

            PostValidator.RuleFor(s => s.Path).IsValidPath().When(s => s.RootFolderPath.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.RootFolderPath)
                         .IsValidPath()
                         .SetValidator(authorFolderAsRootFolderValidator)
                         .When(s => s.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.AuthorName).NotEmpty();
            PostValidator.RuleFor(s => s.ForeignAuthorId).NotEmpty().SetValidator(authorExistsValidator);

            PutValidator.RuleFor(s => s.Path).IsValidPath();
        }

        protected override AuthorResource GetResourceById(int id)
        {
            var author = _authorService.GetAuthor(id);
            return GetAuthorResource(author);
        }

        private AuthorResource GetAuthorResource(NzbDrone.Core.Books.Author author)
        {
            if (author == null)
            {
                return null;
            }

            EnsureAuthorExtras(author);

            var resource = author.ToResource();
            MapCoversToLocal(resource);
            FetchAndLinkAuthorStatistics(resource);
            LinkNextPreviousBooks(resource);

            LinkRootFolderPath(resource);

            return resource;
        }

        [HttpGet]
        public List<AuthorResource> AllAuthors()
        {
            var authorStats = _authorStatisticsService.AuthorStatistics();
            var authors = _authorService.GetAllAuthors();
            foreach (var author in authors)
            {
                EnsureAuthorExtras(author);
            }

            var authorResources = authors.ToResource();

            MapCoversToLocal(authorResources.ToArray());
            LinkNextPreviousBooks(authorResources.ToArray());
            LinkAuthorStatistics(authorResources, authorStats.ToDictionary(x => x.AuthorId));
            LinkRootFolderPath(authorResources.ToArray());

            return authorResources;
        }

        [RestPostById]
        public ActionResult<AuthorResource> AddAuthor(AuthorResource authorResource)
        {
            var author = _addAuthorService.AddAuthor(authorResource.ToModel(), authorResource.DoRefresh ?? true);

            return Created(author.Id);
        }

        [RestPutById]
        public ActionResult<AuthorResource> UpdateAuthor(AuthorResource authorResource, bool moveFiles = false)
        {
            var author = _authorService.GetAuthor(authorResource.Id);

            if (moveFiles)
            {
                var sourcePath = author.Path;
                var destinationPath = authorResource.Path;

                _commandQueueManager.Push(new MoveAuthorCommand
                {
                    AuthorId = author.Id,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    Trigger = CommandTrigger.Manual
                });
            }

            var model = authorResource.ToModel(author);

            _authorService.UpdateAuthor(model);

            BroadcastResourceChange(ModelAction.Updated, authorResource);

            return Accepted(authorResource.Id);
        }

        [RestDeleteById]
        public void DeleteAuthor(int id, bool deleteFiles = false, bool addImportListExclusion = false)
        {
            _authorService.DeleteAuthor(id, deleteFiles, addImportListExclusion);
        }

        private void MapCoversToLocal(params AuthorResource[] authors)
        {
            foreach (var authorResource in authors)
            {
                _coverMapper.ConvertToLocalUrls(authorResource.Id, MediaCoverEntity.Author, authorResource.Images);
            }
        }

        private void EnsureAuthorExtras(NzbDrone.Core.Books.Author author)
        {
            var metadata = author?.Metadata?.Value;
            if (metadata == null)
            {
                return;
            }

            metadata.Images ??= new List<MediaCover>();
            metadata.Links ??= new List<Links>();

            var hasPoster = metadata.Images.Any(x => x.CoverType == MediaCoverTypes.Poster && x.Url.IsNotNullOrWhiteSpace());
            var needsOverview = metadata.Overview.IsNullOrWhiteSpace();
            var hasWikipediaLink = metadata.Links.Any(x =>
                x.Url.IsNotNullOrWhiteSpace() &&
                x.Url.Contains("wikipedia.org", StringComparison.OrdinalIgnoreCase));

            if (hasPoster && !needsOverview && hasWikipediaLink)
            {
                return;
            }

            var extras = _authorExtraMetadataProvider.GetAuthorExtraMetadata(metadata.Name);
            if (extras == null)
            {
                return;
            }

            var changed = false;

            if (!hasPoster && extras.ImageUrl.IsNotNullOrWhiteSpace())
            {
                metadata.Images.Add(new MediaCover
                {
                    Url = extras.ImageUrl,
                    CoverType = MediaCoverTypes.Poster
                });
                changed = true;
            }

            if (needsOverview && extras.Overview.IsNotNullOrWhiteSpace())
            {
                metadata.Overview = extras.Overview;
                changed = true;
            }

            if (extras.Links != null)
            {
                foreach (var link in extras.Links)
                {
                    if (link?.Url.IsNullOrWhiteSpace() ?? true)
                    {
                        continue;
                    }

                    if (metadata.Links.Any(x => x.Url.Equals(link.Url, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    metadata.Links.Add(link);
                    changed = true;
                }
            }

            if (changed)
            {
                _authorMetadataService.Upsert(metadata);
            }
        }

        private void LinkNextPreviousBooks(params AuthorResource[] authors)
        {
            var nextBooks = _bookService.GetNextBooksByAuthorMetadataId(authors.Select(x => x.AuthorMetadataId));
            var lastBooks = _bookService.GetLastBooksByAuthorMetadataId(authors.Select(x => x.AuthorMetadataId));

            foreach (var authorResource in authors)
            {
                authorResource.NextBook = nextBooks.FirstOrDefault(x => x.AuthorMetadataId == authorResource.AuthorMetadataId);
                authorResource.LastBook = lastBooks.FirstOrDefault(x => x.AuthorMetadataId == authorResource.AuthorMetadataId);
            }
        }

        private void FetchAndLinkAuthorStatistics(AuthorResource resource)
        {
            LinkAuthorStatistics(resource, _authorStatisticsService.AuthorStatistics(resource.Id));
        }

        private void LinkAuthorStatistics(List<AuthorResource> resources, Dictionary<int, AuthorStatistics> authorStatistics)
        {
            foreach (var author in resources)
            {
                if (authorStatistics.TryGetValue(author.Id, out var stats))
                {
                    LinkAuthorStatistics(author, stats);
                }
            }
        }

        private void LinkAuthorStatistics(AuthorResource resource, AuthorStatistics authorStatistics)
        {
            resource.Statistics = authorStatistics.ToResource();
        }

        private void LinkRootFolderPath(params AuthorResource[] authors)
        {
            var rootFolders = _rootFolderService.All();

            foreach (var author in authors)
            {
                author.RootFolderPath = _rootFolderService.GetBestRootFolderPath(author.Path, rootFolders);
            }
        }

        [NonAction]
        public void Handle(BookImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetAuthorResource(message.Author));
        }

        [NonAction]
        public void Handle(BookEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetAuthorResource(message.Book.Author.Value));
        }

        [NonAction]
        public void Handle(BookFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            BroadcastResourceChange(ModelAction.Updated, GetAuthorResource(message.BookFile.Author.Value));
        }

        [NonAction]
        public void Handle(AuthorAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetAuthorResource(message.Author));
        }

        [NonAction]
        public void Handle(AuthorUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetAuthorResource(message.Author));
        }

        [NonAction]
        public void Handle(AuthorEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetAuthorResource(message.Author));
        }

        [NonAction]
        public void Handle(AuthorDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.Author.ToResource());
        }

        [NonAction]
        public void Handle(AuthorRenamedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.Author.Id);
        }

        [NonAction]
        public void Handle(MediaCoversUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetAuthorResource(message.Author));
        }
    }
}
