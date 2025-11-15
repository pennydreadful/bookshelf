# AudioBookShelf (ABS) Integration TODO

**Goal:** Import audiobooks from AudioBookShelf libraries into Readarr/Bookshelf using ABS library folder mapping to improve hardcover audiobook imports.

**Target Image:** hardcover variant
**Estimated Effort:** 2-3 days (16-24 hours)

---

## 1. RESEARCH & SETUP

### 1.1 Understand Existing Architecture
- [ ] **1.1.1** Read and document Goodreads import list flow (`GoodreadsOwnedBooks.cs`, `GoodreadsImportListBase.cs`)
  - Understand how `Fetch()` method works
  - Document pagination pattern
  - Note OAuth flow (we'll replace with simpler token auth)

- [ ] **1.1.2** Analyze `ImportListSyncService.cs` to understand book processing
  - How `ImportListItemInfo` gets converted to `Book` entities
  - How metadata lookup works (line ~100-150)
  - How authors are matched/created

- [ ] **1.1.3** Review ABS API documentation
  - Read https://raw.githubusercontent.com/magrhino/mam-audiofinder/refs/heads/main/abs-api-agents.md
  - Document required endpoints: `/api/libraries`, `/api/libraries/{id}/items`, `/api/items/{id}`
  - Note authentication: Bearer token in Authorization header

- [ ] **1.1.4** Test ABS API manually (if instance available)
  - Use curl/Postman to test authentication
  - Fetch library list
  - Fetch items from a library
  - Document actual response structure

### 1.2 Development Environment Setup
- [ ] **1.2.1** Ensure hardcover build configuration is active
  - Verify `HARDCOVER=true` environment variable
  - Check `MetadataProfileService.cs:283` uses hardcover metadata

- [ ] **1.2.2** Set up test ABS instance (if needed)
  - Document connection details for testing
  - Create test library with sample audiobooks

---

## 2. BACKEND - FOUNDATION

### 2.1 Create ABS Resource Models
**Directory:** `src/NzbDrone.Core/ImportLists/AudioBookShelf/Resources/`

- [ ] **2.1.1** Create `AudioBookShelfLibraryResource.cs`
  ```csharp
  // Represents GET /api/libraries response
  public class AudioBookShelfLibraryResource
  {
      public string Id { get; set; }
      public string Name { get; set; }
      public string MediaType { get; set; }  // "book" or "podcast"
      public string FolderPath { get; set; }
      public int NumBooks { get; set; }
  }
  ```

- [ ] **2.1.2** Create `AudioBookShelfLibraryItemResource.cs`
  ```csharp
  // Represents library item from GET /api/libraries/{id}/items
  public class AudioBookShelfLibraryItemResource
  {
      public string Id { get; set; }
      public string MediaType { get; set; }
      public AudioBookShelfMediaResource Media { get; set; }
      public AudioBookShelfMetadataResource Metadata { get; set; }
      public string Path { get; set; }
      public long Size { get; set; }
  }
  ```

- [ ] **2.1.3** Create `AudioBookShelfMediaResource.cs`
  ```csharp
  // Nested media object
  public class AudioBookShelfMediaResource
  {
      public List<AudioBookShelfAudioFileResource> AudioFiles { get; set; }
      public string EbookFormat { get; set; }  // "epub", "pdf", etc.
      public int NumAudioFiles { get; set; }
      public int NumTracks { get; set; }
      public double Duration { get; set; }
  }
  ```

- [ ] **2.1.4** Create `AudioBookShelfAudioFileResource.cs`
  ```csharp
  public class AudioBookShelfAudioFileResource
  {
      public string Filename { get; set; }
      public string Format { get; set; }  // "mp3", "m4b", etc.
      public double Duration { get; set; }
      public string Path { get; set; }
  }
  ```

- [ ] **2.1.5** Create `AudioBookShelfMetadataResource.cs`
  ```csharp
  // Book metadata from ABS
  public class AudioBookShelfMetadataResource
  {
      public string Title { get; set; }
      public string Subtitle { get; set; }
      public string Author { get; set; }  // May be comma-separated
      public List<AudioBookShelfAuthorResource> Authors { get; set; }
      public string Narrator { get; set; }
      public string Publisher { get; set; }
      public string PublishedYear { get; set; }
      public string Isbn { get; set; }
      public string Asin { get; set; }
      public string Description { get; set; }
      public List<string> Genres { get; set; }
  }
  ```

- [ ] **2.1.6** Create `AudioBookShelfAuthorResource.cs`
  ```csharp
  public class AudioBookShelfAuthorResource
  {
      public string Name { get; set; }
  }
  ```

- [ ] **2.1.7** Create `AudioBookShelfPaginatedResponse.cs`
  ```csharp
  // Paginated response wrapper
  public class AudioBookShelfPaginatedResponse<T>
  {
      public List<T> Results { get; set; }
      public int Total { get; set; }
      public int Limit { get; set; }
      public int Page { get; set; }
  }
  ```

### 2.2 Create Exception Handling
- [ ] **2.2.1** Create `AudioBookShelfException.cs`
  ```csharp
  namespace NzbDrone.Core.ImportLists.AudioBookShelf
  {
      public class AudioBookShelfException : Exception
      {
          public AudioBookShelfException(string message) : base(message) { }
          public AudioBookShelfException(string message, Exception innerException)
              : base(message, innerException) { }
      }
  }
  ```

### 2.3 Extend ImportListItemInfo (if needed)
- [ ] **2.3.1** Review `src/NzbDrone.Core/Parser/Model/ImportListItemInfo.cs`
  - Check if existing fields are sufficient
  - Existing: `Author`, `AuthorGoodreadsId`, `Book`, `BookGoodreadsId`, `EditionGoodreadsId`, `ReleaseDate`

- [ ] **2.3.2** Add ABS-specific fields (optional extension)
  ```csharp
  public string AbsLibraryItemId { get; set; }
  public string AbsPath { get; set; }
  public string Format { get; set; }  // "audiobook", "ebook", "hardcover"
  public string Isbn { get; set; }
  public string Asin { get; set; }
  ```
  **Decision:** Only add if needed for enhanced matching

---

## 3. BACKEND - ABS API CLIENT

### 3.1 Create Settings Base Class
**File:** `src/NzbDrone.Core/ImportLists/AudioBookShelf/AudioBookShelfSettings.cs`

- [ ] **3.1.1** Create settings class
  ```csharp
  public class AudioBookShelfSettings : IProviderConfig
  {
      [FieldDefinition(1, Label = "AudioBookShelf URL", HelpText = "URL of your AudioBookShelf server (e.g., http://localhost:13378)")]
      public string BaseUrl { get; set; }

      [FieldDefinition(2, Label = "API Token", Type = FieldType.Password, Privacy = PrivacyLevel.Password, HelpText = "API token from AudioBookShelf settings")]
      public string ApiToken { get; set; }

      [FieldDefinition(3, Label = "Library", Type = FieldType.Select, SelectOptionsProviderAction = "getLibraries", HelpText = "AudioBookShelf library to import from")]
      public string LibraryId { get; set; }

      [FieldDefinition(4, Label = "Import Audiobooks Only", Type = FieldType.Checkbox, HelpText = "Only import items with audio files")]
      public bool AudiobooksOnly { get; set; } = true;

      public NzbDroneValidationResult Validate() { return new NzbDroneValidationResult(); }
  }
  ```

- [ ] **3.1.2** Add validation logic
  - Validate BaseUrl is valid URL
  - Validate ApiToken is not empty
  - Validate LibraryId is selected

### 3.2 Create API Client
**File:** `src/NzbDrone.Core/ImportLists/AudioBookShelf/AudioBookShelfClient.cs`

- [ ] **3.2.1** Create client class skeleton
  ```csharp
  public class AudioBookShelfClient
  {
      private readonly IHttpClient _httpClient;
      private readonly Logger _logger;
      private readonly string _baseUrl;
      private readonly string _apiToken;

      public AudioBookShelfClient(IHttpClient httpClient, AudioBookShelfSettings settings, Logger logger)
      {
          _httpClient = httpClient;
          _logger = logger;
          _baseUrl = settings.BaseUrl.TrimEnd('/');
          _apiToken = settings.ApiToken;
      }
  }
  ```

- [ ] **3.2.2** Implement authentication helper
  ```csharp
  private HttpRequest BuildRequest(string endpoint)
  {
      var request = new HttpRequest($"{_baseUrl}{endpoint}");
      request.Headers.Add("Authorization", $"Bearer {_apiToken}");
      request.Headers.Set("Content-Type", "application/json");
      return request;
  }
  ```

- [ ] **3.2.3** Implement `GetLibraries()` method
  ```csharp
  public List<AudioBookShelfLibraryResource> GetLibraries()
  {
      try
      {
          var request = BuildRequest("/api/libraries");
          var response = _httpClient.Get<AudioBookShelfLibrariesResponse>(request);
          return response.Resource.Libraries;
      }
      catch (HttpException ex)
      {
          _logger.Error(ex, "Failed to fetch libraries from AudioBookShelf");
          throw new AudioBookShelfException("Failed to connect to AudioBookShelf", ex);
      }
  }
  ```

- [ ] **3.2.4** Implement `GetLibraryItems()` with pagination
  ```csharp
  public List<AudioBookShelfLibraryItemResource> GetLibraryItems(string libraryId, int limit = 100)
  {
      var allItems = new List<AudioBookShelfLibraryItemResource>();
      var page = 0;

      while (true)
      {
          var endpoint = $"/api/libraries/{libraryId}/items?limit={limit}&page={page}";
          var request = BuildRequest(endpoint);

          try
          {
              var response = _httpClient.Get<AudioBookShelfPaginatedResponse<AudioBookShelfLibraryItemResource>>(request);

              if (response.Resource.Results == null || response.Resource.Results.Count == 0)
                  break;

              allItems.AddRange(response.Resource.Results);
              page++;

              if (allItems.Count >= response.Resource.Total)
                  break;
          }
          catch (HttpException ex)
          {
              _logger.Error(ex, $"Failed to fetch library items page {page}");
              throw new AudioBookShelfException($"Failed to fetch library items", ex);
          }
      }

      return allItems;
  }
  ```

- [ ] **3.2.5** Implement `GetItem()` for detailed metadata (optional)
  ```csharp
  public AudioBookShelfLibraryItemResource GetItem(string itemId)
  {
      var request = BuildRequest($"/api/items/{itemId}");
      var response = _httpClient.Get<AudioBookShelfLibraryItemResource>(request);
      return response.Resource;
  }
  ```

- [ ] **3.2.6** Implement `TestConnection()` for validation
  ```csharp
  public bool TestConnection()
  {
      try
      {
          var libraries = GetLibraries();
          return libraries != null;
      }
      catch
      {
          return false;
      }
  }
  ```

---

## 4. BACKEND - IMPORT LIST PROVIDER

### 4.1 Create Import List Base Class
**File:** `src/NzbDrone.Core/ImportLists/AudioBookShelf/AudioBookShelfImportListBase.cs`

- [ ] **4.1.1** Create base class
  ```csharp
  public abstract class AudioBookShelfImportListBase<TSettings> : ImportListBase<TSettings>
      where TSettings : AudioBookShelfSettings, new()
  {
      protected readonly IHttpClient _httpClient;

      protected AudioBookShelfImportListBase(
          IImportListStatusService importListStatusService,
          IConfigService configService,
          IParsingService parsingService,
          IHttpClient httpClient,
          Logger logger)
          : base(importListStatusService, configService, parsingService, logger)
      {
          _httpClient = httpClient;
      }

      public override ImportListType ListType => ImportListType.Other;

      protected AudioBookShelfClient GetClient()
      {
          return new AudioBookShelfClient(_httpClient, Settings, _logger);
      }
  }
  ```

- [ ] **4.1.2** Implement test connection
  ```csharp
  protected override void Test(List<ValidationFailure> failures)
  {
      failures.AddIfNotNull(TestConnection());
  }

  private ValidationFailure TestConnection()
  {
      try
      {
          var client = GetClient();
          if (!client.TestConnection())
          {
              return new ValidationFailure(string.Empty, "Failed to connect to AudioBookShelf");
          }
          return null;
      }
      catch (Exception ex)
      {
          _logger.Error(ex, "AudioBookShelf connection test failed");
          return new ValidationFailure(string.Empty, $"Connection failed: {ex.Message}");
      }
  }
  ```

- [ ] **4.1.3** Implement `RequestAction` for library dropdown
  ```csharp
  public override object RequestAction(string action, IDictionary<string, string> query)
  {
      if (action == "getLibraries")
      {
          var client = GetClient();
          var libraries = client.GetLibraries();

          return new
          {
              options = libraries
                  .Where(l => l.MediaType == "book")
                  .Select(l => new { value = l.Id, name = l.Name })
                  .ToList()
          };
      }

      return base.RequestAction(action, query);
  }
  ```

### 4.2 Create Main Import List Implementation
**File:** `src/NzbDrone.Core/ImportLists/AudioBookShelf/AudioBookShelfImport.cs`

- [ ] **4.2.1** Create import list class
  ```csharp
  public class AudioBookShelfImport : AudioBookShelfImportListBase<AudioBookShelfSettings>
  {
      public AudioBookShelfImport(
          IImportListStatusService importListStatusService,
          IConfigService configService,
          IParsingService parsingService,
          IHttpClient httpClient,
          Logger logger)
          : base(importListStatusService, configService, parsingService, httpClient, logger)
      {
      }

      public override string Name => "AudioBookShelf Library";
      public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(6);
  }
  ```

- [ ] **4.2.2** Implement `Fetch()` method
  ```csharp
  public override IList<ImportListItemInfo> Fetch()
  {
      _logger.Debug("Fetching books from AudioBookShelf library {0}", Settings.LibraryId);

      var client = GetClient();
      var items = client.GetLibraryItems(Settings.LibraryId);

      _logger.Debug("Retrieved {0} items from AudioBookShelf", items.Count);

      // Filter audiobooks if configured
      if (Settings.AudiobooksOnly)
      {
          items = items.Where(IsAudiobook).ToList();
          _logger.Debug("Filtered to {0} audiobook items", items.Count);
      }

      var result = items.Select(MapToImportListItem).Where(x => x != null).ToList();

      return CleanupListItems(result);
  }
  ```

- [ ] **4.2.3** Implement `IsAudiobook()` helper
  ```csharp
  private bool IsAudiobook(AudioBookShelfLibraryItemResource item)
  {
      return item.MediaType == "book" &&
             item.Media?.AudioFiles != null &&
             item.Media.AudioFiles.Any();
  }
  ```

- [ ] **4.2.4** Implement `MapToImportListItem()` - CRITICAL METHOD
  ```csharp
  private ImportListItemInfo MapToImportListItem(AudioBookShelfLibraryItemResource absItem)
  {
      try
      {
          if (absItem.Metadata == null)
          {
              _logger.Warn("Item {0} has no metadata, skipping", absItem.Id);
              return null;
          }

          var metadata = absItem.Metadata;

          // Get primary author (ABS can have multiple)
          var authorName = metadata.Authors?.FirstOrDefault()?.Name
                          ?? metadata.Author
                          ?? "Unknown Author";

          var bookTitle = metadata.Title;
          if (string.IsNullOrWhiteSpace(bookTitle))
          {
              _logger.Warn("Item {0} has no title, skipping", absItem.Id);
              return null;
          }

          return new ImportListItemInfo
          {
              Author = authorName.CleanSpaces(),
              Book = bookTitle.CleanSpaces(),
              // Leave Goodreads IDs null - will be looked up by metadata service
              AuthorGoodreadsId = null,
              BookGoodreadsId = null,
              EditionGoodreadsId = null,
              // Parse release date if available
              ReleaseDate = ParseReleaseDate(metadata.PublishedYear),
              // Store additional metadata for matching (if we extended the model)
              // AbsLibraryItemId = absItem.Id,
              // AbsPath = absItem.Path,
              // Isbn = metadata.Isbn,
              // Asin = metadata.Asin,
              // Format = "audiobook"
          };
      }
      catch (Exception ex)
      {
          _logger.Error(ex, "Failed to map ABS item {0}", absItem.Id);
          return null;
      }
  }
  ```

- [ ] **4.2.5** Implement `ParseReleaseDate()` helper
  ```csharp
  private DateTime ParseReleaseDate(string publishedYear)
  {
      if (string.IsNullOrWhiteSpace(publishedYear))
          return DateTime.MinValue;

      // Try to parse year
      if (int.TryParse(publishedYear, out var year) && year > 1000 && year < 3000)
      {
          return new DateTime(year, 1, 1);
      }

      // Try to parse full date
      if (DateTime.TryParse(publishedYear, out var date))
      {
          return date;
      }

      return DateTime.MinValue;
  }
  ```

### 4.3 Register Provider in Dependency Injection
**File:** `src/NzbDrone.Core/Readarr.Core.csproj` (verify it auto-registers, or find DI registration)

- [ ] **4.3.1** Verify provider auto-discovery
  - Check how `GoodreadsOwnedBooks` is registered
  - Ensure `AudioBookShelfImport` follows same pattern
  - May need to add to a provider list if not auto-discovered

---

## 5. BACKEND - INTEGRATION WITH EXISTING FLOW

### 5.1 Test Import Sync Service Compatibility
- [ ] **5.1.1** Review `ImportListSyncService.cs` method `ProcessListItems()`
  - Confirm it handles null Goodreads IDs gracefully
  - Verify metadata lookup by title+author works
  - Check author creation/matching logic

- [ ] **5.1.2** Add logging to debug import flow
  - Add trace logs to show when ABS items are processed
  - Log metadata lookup results
  - Track author/book matching success rate

### 5.2 Enhance Metadata Matching (Optional Enhancement)
**File:** `src/NzbDrone.Core/ImportLists/ImportListSyncService.cs`

- [ ] **5.2.1** If `ImportListItemInfo` was extended with ISBN/ASIN
  - Add logic to match by ISBN first (line ~120-150)
  - Fall back to title+author matching
  - Log match confidence

- [ ] **5.2.2** Add format preference logic
  - If multiple editions found, prefer audiobook edition
  - Use `Edition.Format` field to filter

### 5.3 Configure ImportListType
**File:** `src/NzbDrone.Core/ImportLists/ImportListType.cs` (if exists)

- [ ] **5.3.1** Check if ImportListType enum needs new entry
  - Review existing types: Goodreads, Readarr, etc.
  - Add `AudioBookShelf` if needed
  - Or use `Other` as fallback

---

## 6. FRONTEND - SETTINGS UI

### 6.1 Create React Components
**Directory:** `frontend/src/Settings/ImportLists/ImportLists/AudioBookShelf/`

- [ ] **6.1.1** Create `AudioBookShelfSettings.js`
  ```javascript
  import React from 'react';
  import { FormGroup, FormLabel, FormInputGroup } from 'Components/Form';

  function AudioBookShelfSettings(props) {
    const { settings, onFieldChange } = props;

    return (
      <>
        <FormGroup>
          <FormLabel>AudioBookShelf URL</FormLabel>
          <FormInputGroup
            type="text"
            name="baseUrl"
            value={settings.baseUrl}
            placeholder="http://localhost:13378"
            onChange={onFieldChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>API Token</FormLabel>
          <FormInputGroup
            type="password"
            name="apiToken"
            value={settings.apiToken}
            onChange={onFieldChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Library</FormLabel>
          <FormInputGroup
            type="select"
            name="libraryId"
            value={settings.libraryId}
            values={settings.libraryOptions || []}
            onChange={onFieldChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Import Audiobooks Only</FormLabel>
          <FormInputGroup
            type="checkbox"
            name="audiobooksOnly"
            value={settings.audiobooksOnly}
            onChange={onFieldChange}
          />
        </FormGroup>
      </>
    );
  }

  export default AudioBookShelfSettings;
  ```

- [ ] **6.1.2** Add test connection functionality
  - Implement test button
  - Show success/error message
  - Validate all fields before test

- [ ] **6.1.3** Implement library dropdown population
  - Call `getLibraries` action when BaseUrl/ApiToken change
  - Populate dropdown with library names
  - Handle loading state

### 6.2 Register in Import List UI
**Files:** Check frontend import list registration pattern

- [ ] **6.2.1** Find import list provider registration
  - Look for similar pattern to Goodreads registration
  - May be in `frontend/src/Settings/ImportLists/ImportLists/`

- [ ] **6.2.2** Add AudioBookShelf to provider list
  - Add to schema/provider enum
  - Map to AudioBookShelfSettings component
  - Add icon/logo (optional)

- [ ] **6.2.3** Test UI rendering
  - Verify settings panel appears
  - Test form field validation
  - Verify library dropdown works

---

## 7. TESTING

### 7.1 Unit Tests (Backend)
**Directory:** `src/NzbDrone.Core.Test/ImportLists/AudioBookShelf/`

- [ ] **7.1.1** Create `AudioBookShelfClientFixture.cs`
  - Test authentication header
  - Test GetLibraries response parsing
  - Test GetLibraryItems pagination
  - Mock HTTP responses

- [ ] **7.1.2** Create `AudioBookShelfImportFixture.cs`
  - Test MapToImportListItem with various inputs
  - Test filtering logic (audiobooks only)
  - Test null/empty handling
  - Test ParseReleaseDate with various formats

- [ ] **7.1.3** Create `AudioBookShelfSettingsFixture.cs`
  - Test validation logic
  - Test URL normalization

### 7.2 Integration Tests
**Directory:** `src/NzbDrone.Integration.Test/ApiTests/`

- [ ] **7.2.1** Create `AudioBookShelfImportListFixture.cs`
  - Test adding ABS import list via API
  - Test fetching items
  - Test import sync process
  - Requires real or mocked ABS instance

### 7.3 Manual Testing Checklist
- [ ] **7.3.1** End-to-end import flow
  - Add ABS import list in UI
  - Configure with real ABS instance
  - Trigger manual import
  - Verify books are added to library
  - Check author/book matching worked
  - Verify audiobook files are detected

- [ ] **7.3.2** Error handling
  - Test with invalid URL
  - Test with invalid API token
  - Test with empty library
  - Test with malformed metadata
  - Verify error messages are helpful

- [ ] **7.3.3** Performance testing
  - Test with large library (1000+ items)
  - Measure import time
  - Check memory usage
  - Verify pagination works correctly

---

## 8. DOCUMENTATION

### 8.1 Code Documentation
- [ ] **8.1.1** Add XML documentation to all public methods
  - AudioBookShelfClient methods
  - AudioBookShelfImport.Fetch()
  - Settings properties

- [ ] **8.1.2** Add inline comments for complex logic
  - Metadata mapping algorithm
  - Pagination logic
  - Format detection

### 8.2 User Documentation
- [ ] **8.2.1** Update README.md
  - Add AudioBookShelf import list to feature list
  - Document setup steps
  - Add example configuration

- [ ] **8.2.2** Create ABS_SETUP.md guide
  - How to get ABS API token
  - How to configure import list
  - Troubleshooting common issues
  - Explain library folder mapping benefits

- [ ] **8.2.3** Add configuration examples
  - Docker compose example with ABS
  - Environment variables
  - Sample import list JSON

### 8.3 Technical Documentation
- [ ] **8.3.1** Document API mappings
  - ABS API → ImportListItemInfo field mapping
  - Metadata matching strategy
  - Format detection logic

- [ ] **8.3.2** Architecture diagram
  - Show ABS integration in import flow
  - Data flow from ABS → Readarr → Files

---

## 9. DEPLOYMENT & FINALIZATION

### 9.1 Build Configuration
- [ ] **9.1.1** Verify hardcover build includes ABS support
  - Check no Goodreads-specific dependencies
  - Ensure works with hardcover metadata provider

- [ ] **9.1.2** Test Docker build
  - Build hardcover image
  - Verify ABS import list appears
  - Test runtime functionality

### 9.2 Code Quality
- [ ] **9.2.1** Run linter
  - Fix C# style warnings
  - Fix JavaScript/TypeScript warnings

- [ ] **9.2.2** Code review checklist
  - No hardcoded credentials
  - Proper error handling everywhere
  - Logging at appropriate levels
  - No memory leaks (dispose IHttpClient properly)

### 9.3 Git & Release
- [ ] **9.3.1** Commit with clear message
  - "Add AudioBookShelf import list provider"
  - Include detailed commit body

- [ ] **9.3.2** Push to feature branch

- [ ] **9.3.3** Create pull request
  - Reference issue #39 from README
  - Include testing notes
  - Add screenshots of UI

---

## DEPENDENCIES & ORDER

**Critical Path:**
1. Research (1.1-1.4) → Foundation (2.1-2.3) → API Client (3.1-3.2) → Import Provider (4.1-4.2) → Testing (7.3)

**Parallel Work Possible:**
- Frontend (6.1-6.2) can be done after Settings (3.1) is complete
- Unit tests (7.1) can be written alongside implementation
- Documentation (8.1-8.3) can be written anytime after implementation

**Estimated Hours:**
- Research & Setup: 2-3 hours
- Backend Foundation: 2-3 hours
- ABS API Client: 3-4 hours
- Import List Provider: 4-5 hours
- Integration: 1-2 hours
- Frontend UI: 3-4 hours
- Testing: 3-4 hours
- Documentation: 2-3 hours

**Total: 20-28 hours (2.5-3.5 days)**

---

## NOTES

- **Simpler than estimated** because we're reusing existing ImportList infrastructure
- No complex OAuth (unlike Goodreads) - just Bearer token
- ABS API is well-documented and consistent
- Existing metadata matching handles null Goodreads IDs
- Library folder mapping is automatic from ABS path data

**Risks:**
- ABS API changes (mitigation: version check)
- Metadata quality varies (mitigation: fallback to title+author search)
- Frontend React version compatibility (mitigation: follow existing patterns)

**Next Steps After Completion:**
- Monitor import success rate
- Gather user feedback
- Add advanced features (series support, narrator matching)
- Consider ABS → Readarr bidirectional sync
