# CLAUDE.md - AI Assistant Guide for Bookshelf

## Project Overview

**Bookshelf** is a revival of [Readarr](https://github.com/Readarr/Readarr), an ebook and audiobook collection manager for Usenet and BitTorrent users. This fork improves upon the original by:

- Using higher-quality metadata providers (Hardcover instead of Goodreads)
- Removing analytics/telemetry
- Native support for MyAnonaMouse without Prowlarr
- Supporting self-hosted metadata servers
- Lower default match percentage (50% vs 80%)
- No local metadata caching

**Key Difference:** Bookshelf comes in two variants:
- **`softcover` tags**: Use Goodreads metadata (backward-compatible with Readarr)
- **`hardcover` tags**: Use Hardcover.app metadata (higher quality, not backward-compatible)

## Technology Stack

### Backend (C# / .NET)
- **.NET 6.0** - Primary runtime framework
- **ASP.NET Core** - Web API layer
- **NLog** - Logging framework
- **SQLite** - Database (via custom ORM)
- **FluentValidation** - Settings validation
- **SignalR** - Real-time notifications to frontend
- **RestSharp/HttpClient** - External API communication
- **NUnit** - Testing framework

### Frontend (React / TypeScript)
- **React 17** - UI library
- **Redux** - State management (with redux-thunk for async)
- **React Router 5** - Routing
- **TypeScript** - Type safety
- **Webpack 5** - Build tool
- **PostCSS** - CSS processing (with mixins, nested, variables)
- **ESLint + Prettier** - Code quality
- **Babel** - JavaScript transpilation

### Build & CI/CD
- **Bash scripts** - Build orchestration (`build.sh`, `test.sh`)
- **MSBuild** - .NET project compilation
- **GitHub Actions** - CI/CD pipeline
- **Docker** - Containerization (Alpine Linux base)
- **Yarn** - JavaScript package management
- **mise** - Development environment management

## Repository Structure

```
bookshelf/
├── src/                          # Backend C# code
│   ├── NzbDrone.Core/            # Core business logic
│   │   ├── Books/                # Book and author management
│   │   ├── ImportLists/          # Import list providers (Goodreads, etc.)
│   │   ├── MetadataSource/       # Metadata provider integrations
│   │   ├── Download/             # Download client integrations
│   │   ├── Indexers/             # Torrent/Usenet indexer support
│   │   ├── Parser/               # Book/release parsing
│   │   └── ...                   # Other core services
│   ├── Readarr.Api.V1/           # REST API controllers
│   ├── Readarr.Http/             # HTTP infrastructure
│   ├── NzbDrone.Common/          # Shared utilities
│   ├── NzbDrone.Host/            # Application host
│   ├── NzbDrone.*Test/           # Unit/integration tests
│   ├── Readarr.sln               # Visual Studio solution
│   └── Directory.Build.props     # MSBuild configuration
│
├── frontend/                     # Frontend React code
│   ├── src/                      # React components and logic
│   │   ├── Author/               # Author management UI
│   │   ├── Book/                 # Book management UI
│   │   ├── Settings/             # Settings pages
│   │   ├── Store/                # Redux store
│   │   ├── Components/           # Reusable components
│   │   └── typings/              # TypeScript type definitions
│   ├── build/                    # Webpack configuration
│   ├── .eslintrc.js              # ESLint configuration
│   ├── tsconfig.json             # TypeScript configuration
│   └── postcss.config.js         # PostCSS configuration
│
├── docker/                       # Docker build files
│   ├── Dockerfile                # Alpine-based container
│   └── root/                     # Container filesystem overlay
│
├── distribution/                 # Platform-specific packaging
│   ├── windows/                  # Windows installer
│   └── osx/                      # macOS app bundle
│
├── schemas/                      # JSON schemas
├── .github/workflows/            # GitHub Actions workflows
├── build.sh                      # Build orchestration script
├── test.sh                       # Test runner script
├── package.json                  # Frontend dependencies
└── TODO.md                       # Current development roadmap
```

## Architecture Patterns

### Backend Architecture

#### Dependency Injection
- **Container**: Custom DI container using reflection
- **Service Registration**: Auto-discovery via interfaces
- **Lifetime Management**: Singleton and transient services

#### Repository Pattern
- Base repository interfaces for data access
- Database context abstraction
- Custom SQL generation

#### Provider Pattern
All extensible features use the provider pattern:
- **Import Lists** (`IImportList`) - Import books from external sources
- **Metadata Providers** (`IProvideBookInfo`, `IProvideAuthorInfo`) - Fetch book metadata
- **Download Clients** (`IDownloadClient`) - Integrate with download tools
- **Indexers** (`IIndexer`) - Search torrent/Usenet sources
- **Notifications** (`INotification`) - Send notifications

#### Service Layer
Core services orchestrate business logic:
- `ImportListSyncService` - Syncs import lists
- `BookSearchService` - Searches for books
- `MetadataProfileService` - Manages metadata profiles
- `AuthorService`, `BookService` - Domain entity management

### Frontend Architecture

#### Component Structure
```
Component/
├── index.ts                  # Re-export (barrel pattern)
├── Component.tsx             # Main component
├── Component.css             # Component styles (CSS Modules)
├── ComponentConnector.ts     # Redux connection (if needed)
└── Component.test.tsx        # Unit tests
```

#### Redux State Management
- **Actions**: Redux actions with redux-actions
- **Reducers**: Standard Redux reducers
- **Selectors**: Reselect for memoized selectors
- **Thunks**: Async actions with redux-thunk
- **Middleware**: Custom middleware for API calls

#### API Communication
- Centralized API client in `Store/Actions/`
- SignalR for real-time updates
- Optimistic updates where applicable

## Coding Conventions

### C# Backend Conventions

#### Style (`.editorconfig`)
```csharp
// File-scoped namespaces (preferred in new code)
namespace NzbDrone.Core.ImportLists.AudioBookShelf;

// Use var everywhere
var client = new HttpClient();
var items = GetItems();

// Private fields prefixed with underscore
private readonly IHttpClient _httpClient;

// 4-space indentation, no tabs
public class Example
{
    public void Method()
    {
        // Method body
    }
}

// Avoid "this." qualification (warning if used)
_logger.Info("Message"); // Correct
this._logger.Info("Message"); // Warning

// Use inline variable declarations
if (int.TryParse(value, out var result))
{
    return result;
}
```

#### Naming Conventions
- **Interfaces**: `I` prefix (e.g., `IImportList`)
- **Settings Classes**: `{Provider}Settings` suffix
- **Resource Models**: `{Entity}Resource` suffix
- **Services**: `{Entity}Service` or `I{Action}Service`
- **Tests**: `{Class}Fixture` for test classes

#### Import List Provider Pattern
When implementing a new import list provider, follow this structure:

```
ImportLists/{ProviderName}/
├── {ProviderName}Import.cs           # Main provider class
├── {ProviderName}Settings.cs         # Settings with [FieldDefinition]
├── {ProviderName}Client.cs           # API client (optional)
├── {ProviderName}RequestGenerator.cs # Request generator (for HTTP)
├── {ProviderName}Parser.cs           # Response parser (for HTTP)
└── Resources/
    └── {Entity}Resource.cs           # API response models
```

**Example implementation structure:**
```csharp
// Main provider
public class AudioBookShelfImport : ImportListBase<AudioBookShelfSettings>
{
    public override string Name => "AudioBookShelf Library";
    public override ImportListType ListType => ImportListType.Other;
    public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(6);

    public override IList<ImportListItemInfo> Fetch()
    {
        // Fetch and return books
    }

    protected override void Test(List<ValidationFailure> failures)
    {
        // Validate settings
    }
}

// Settings
public class AudioBookShelfSettings : IImportListSettings
{
    [FieldDefinition(1, Label = "URL", HelpText = "Server URL")]
    public string BaseUrl { get; set; }

    [FieldDefinition(2, Label = "API Token", Type = FieldType.Password,
                     Privacy = PrivacyLevel.Password)]
    public string ApiToken { get; set; }

    public NzbDroneValidationResult Validate() => new();
}
```

#### Key Base Classes
- **`ImportListBase<TSettings>`** - Base for all import lists
  - Override: `Fetch()`, `Test()`, `Name`, `ListType`, `MinRefreshInterval`
- **`HttpImportListBase<TSettings>`** - For HTTP-based providers
  - Override: `GetRequestGenerator()`, `GetParser()`
- **`ProviderBase<TSettings>`** - Base for all providers (notifications, indexers, etc.)

#### Validation
Use FluentValidation for settings validation:
```csharp
public class AudioBookShelfSettingsValidator : AbstractValidator<AudioBookShelfSettings>
{
    public AudioBookShelfSettingsValidator()
    {
        RuleFor(c => c.BaseUrl).ValidRootUrl();
        RuleFor(c => c.ApiToken).NotEmpty();
    }
}
```

### Frontend Conventions

#### TypeScript Style (`.eslintrc.js`)
```typescript
// 2-space indentation
// Use function components (hooks preferred)
function BookList() {
  const books = useSelector(selectBooks);

  return (
    <div className={styles.bookList}>
      {books.map((book) => <BookItem key={book.id} {...book} />)}
    </div>
  );
}

// Use CSS Modules
import styles from './BookList.css';

// Prefer arrow functions for callbacks
const handleClick = useCallback(() => {
  dispatch(deleteBook(id));
}, [id, dispatch]);

// Use proper typing
interface BookListProps {
  authorId: number;
  onBookSelect: (book: Book) => void;
}
```

#### File Organization
- **Components**: PascalCase (e.g., `BookList.tsx`)
- **Utilities**: camelCase (e.g., `bookHelper.ts`)
- **Types**: Match component name (e.g., `Book.ts`, `Author.ts`)
- **Styles**: Match component name (e.g., `BookList.css`)

#### Redux Conventions
```typescript
// Action creators with redux-actions
import { createAction } from 'redux-actions';

export const fetchBooks = createAction('books/fetch');
export const setBooksSort = createAction('books/setSort');

// Thunks for async operations
export const fetchBooksIfNeeded = () => {
  return (dispatch, getState) => {
    const state = getState();
    if (!state.books.isPopulated) {
      dispatch(fetchBooks());
    }
  };
};

// Selectors with reselect
import { createSelector } from 'reselect';

export const selectBooksSorted = createSelector(
  selectBooks,
  selectSortKey,
  (books, sortKey) => books.sort((a, b) => a[sortKey] > b[sortKey] ? 1 : -1)
);
```

#### CSS Module Patterns
```css
/* Use PostCSS features */
.container {
  /* Variables */
  $padding: 20px;

  /* Nesting */
  .header {
    padding: $padding;

    &.highlighted {
      background: yellow;
    }
  }

  /* Mixins */
  @mixin large {
    width: 100%;
  }
}
```

## Development Workflows

### Initial Setup

```bash
# Clone repository
git clone https://github.com/magrhino/bookshelf
cd bookshelf

# Install .NET SDK 6.0.427
# Install Node.js 20.11.1 (specified in package.json volta)
# Install Yarn 1.22.19

# Install frontend dependencies
yarn install --frozen-lockfile

# Build backend and frontend
./build.sh --all

# Run tests
./test.sh Linux Unit Test
```

### Development Build

```bash
# Backend only
./build.sh --backend

# Frontend only (with watch mode)
yarn start

# Both
./build.sh --backend --frontend

# With linting
./build.sh --all --lint
```

### Testing

```bash
# Unit tests
./test.sh Linux Unit Test

# Integration tests
./test.sh Linux Integration Test

# Automation tests
./test.sh Linux Automation Test

# With coverage
./test.sh Linux Unit Coverage
```

### Docker Build

```bash
# Build for specific metadata provider
docker build -f docker/Dockerfile \
  --build-arg HARDCOVER=true \
  --build-arg METADATA_URL=https://api.bookinfo.pro \
  -t bookshelf:hardcover .

# Run container
docker run -p 8787:8787 -v ~/.config/bookshelf:/config bookshelf:hardcover
```

### Frontend Development

```bash
# Start webpack dev server (watch mode)
yarn start

# Run linting
yarn lint
yarn lint-fix

# Run style linting
yarn stylelint-linux  # or stylelint-windows
```

### Code Quality

```bash
# Lint C# (StyleCop built into build)
dotnet build src/Readarr.sln

# Lint JavaScript/TypeScript
yarn lint

# Lint CSS
yarn stylelint-linux

# Auto-fix where possible
yarn lint-fix
```

## Testing Strategy

### Backend Testing

#### Unit Tests (`NzbDrone.Core.Test`)
- Test business logic in isolation
- Mock dependencies with NSubstitute or Moq
- Follow `{Class}Fixture` naming convention

```csharp
[TestFixture]
public class AudioBookShelfClientFixture
{
    private AudioBookShelfClient _client;
    private Mock<IHttpClient> _httpClient;

    [SetUp]
    public void Setup()
    {
        _httpClient = new Mock<IHttpClient>();
        _client = new AudioBookShelfClient(_httpClient.Object, /*...*/);
    }

    [Test]
    public void should_fetch_libraries()
    {
        // Arrange
        _httpClient.Setup(x => x.Get(It.IsAny<HttpRequest>()))
            .Returns(new HttpResponse { Resource = /*...*/ });

        // Act
        var result = _client.GetLibraries();

        // Assert
        result.Should().NotBeEmpty();
    }
}
```

#### Integration Tests (`NzbDrone.Integration.Test`)
- Test full API endpoints
- Use real database (SQLite in-memory)
- Test provider integrations

### Frontend Testing
- Limited test coverage currently
- Consider adding tests for new features

## Git Workflow

### Branch Strategy
- **`master`** - Stable releases
- **`develop`** - Development branch (not actively used in this fork)
- **Feature branches** - Named `feature/{feature-name}` or `claude/{session-id}`

### Commit Conventions
```
<type>: <short summary>

<detailed description if needed>

Examples:
- feat: Add AudioBookShelf import list provider
- fix: Handle null metadata in book parser
- refactor: Extract metadata mapping to separate service
- docs: Update README with new features
- test: Add unit tests for import list client
```

### Pull Request Workflow
1. Create feature branch from `master`
2. Make changes and commit
3. Push to remote
4. Create pull request
5. CI runs tests automatically
6. Merge when approved and tests pass

## Common Tasks for AI Assistants

### Adding a New Import List Provider

1. **Create provider structure** in `src/NzbDrone.Core/ImportLists/{ProviderName}/`
2. **Implement base classes**:
   - Settings class with `[FieldDefinition]` attributes
   - Main provider extending `ImportListBase<TSettings>` or `HttpImportListBase<TSettings>`
   - Optional: Request generator and parser (for HTTP providers)
3. **Implement required methods**:
   - `Fetch()` - Return `List<ImportListItemInfo>`
   - `Test()` - Validate settings and connection
4. **Frontend integration** (auto-generated from `[FieldDefinition]` attributes)
5. **Add tests**:
   - Unit tests in `NzbDrone.Core.Test/ImportLists/{ProviderName}/`
   - Integration tests if needed
6. **Update documentation**:
   - Add to README.md feature list
   - Create setup guide if complex

**See `TODO.md` for detailed example of AudioBookShelf integration**

### Adding a New Metadata Provider

1. Implement `IProvideBookInfo` and/or `IProvideAuthorInfo`
2. Create in `src/NzbDrone.Core/MetadataSource/`
3. Register with DI container
4. Handle both search and lookup by ID
5. Map external IDs to internal model

### Modifying the Frontend

1. **Find the component** in `frontend/src/`
2. **Update TypeScript types** in `frontend/src/typings/` if needed
3. **Modify Redux store** if state changes required:
   - Add actions in `Store/Actions/`
   - Update reducers in appropriate reducer file
   - Add selectors if needed
4. **Update styles** in corresponding `.css` file
5. **Test in browser** via `yarn start`

### Debugging

#### Backend
- Add logging: `_logger.Debug("message")`, `_logger.Info()`, `_logger.Error()`
- Check logs in: `~/.config/Readarr/logs/`
- Set breakpoints in IDE (VS Code, Visual Studio, Rider)

#### Frontend
- Use React DevTools
- Redux DevTools for state inspection
- Browser console for errors
- Network tab for API calls

## Environment Variables

### Build Time
- `READARRVERSION` - Version number (format: `0.4.20.{build_number}`)
- `BUILD_SOURCEBRANCHNAME` - Git branch name
- `DOTNET_VERSION` - .NET SDK version

### Runtime (Docker)
- `METADATA_URL` - Metadata server URL (default: `https://api.bookinfo.pro`)
- `HARDCOVER` - Enable Hardcover metadata provider (`true`/`false`)
- `TMPDIR` - Temporary directory (default: `/run/readarr-temp`)
- `XDG_CONFIG_HOME` - Config directory (default: `/config/xdg`)

## Key Files Reference

### Configuration Files
- `.editorconfig` - Code style rules (C#, JS, CSS)
- `src/Stylecop.ruleset` - C# analyzer rules
- `frontend/.eslintrc.js` - ESLint rules
- `frontend/.stylelintrc` - Stylelint rules
- `frontend/tsconfig.json` - TypeScript configuration
- `src/Directory.Build.props` - MSBuild properties
- `package.json` - Frontend dependencies and scripts

### Build Files
- `build.sh` - Main build orchestration
- `test.sh` - Test runner
- `azure-pipelines.yml` - Azure Pipelines config (legacy)
- `.github/workflows/build.yml` - GitHub Actions CI

### Important Source Files
- `src/NzbDrone.Core/ImportLists/ImportListSyncService.cs` - Import list sync logic
- `src/NzbDrone.Core/MetadataSource/` - Metadata provider implementations
- `src/Readarr.Api.V1/` - REST API controllers
- `frontend/src/Store/` - Redux state management

## Documentation Resources

- **Readarr Docs** (upstream): https://wiki.servarr.com/en/readarr
- **Development**: Refer to `CONTRIBUTING.md`
- **TODO/Roadmap**: See `TODO.md` for planned features
- **Security**: See `SECURITY.md`

## Current State & TODOs

### Completed Features
- Native MyAnonaMouse support
- Hardcover metadata provider
- Self-hosted metadata support
- Removed analytics/telemetry
- Lower match percentage defaults

### Active Development (see `TODO.md`)
- AudioBookShelf import list integration (detailed spec in TODO.md)
- Monitor series functionality
- Hardcover bookshelf import
- Support for ebook + audio in same root

### Known Issues
- Goodreads list imports may not work with hardcover variant
- Not backward-compatible with Readarr databases when using hardcover

## AI Assistant Best Practices

1. **Always read existing code** before implementing new features to match patterns
2. **Follow the provider pattern** for extensible features (import lists, notifications, etc.)
3. **Use proper abstractions** - extend base classes rather than reimplementing
4. **Maintain test coverage** - add unit tests for business logic
5. **Document with comments** - especially for complex algorithms or workarounds
6. **Preserve backward compatibility** when possible
7. **Use dependency injection** - don't instantiate services directly
8. **Validate all inputs** using FluentValidation
9. **Log appropriately** - Debug for verbose, Info for key events, Error for failures
10. **Check TODO.md** before starting work to avoid duplicate effort

## Questions to Ask Before Implementing

- Does this feature belong in `softcover`, `hardcover`, or both variants?
- Is there an existing provider/service I can extend?
- What are the database migration implications?
- Does this change the API contract?
- Are there configuration/settings needed?
- How will this be tested?
- What error cases need handling?
- Is this documented?

---

**Last Updated**: 2025-11-15
**Repository**: https://github.com/magrhino/bookshelf
**License**: GPLv3
