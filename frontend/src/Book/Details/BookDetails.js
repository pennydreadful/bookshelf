import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { Tab, TabList, TabPanel, Tabs } from 'react-tabs';
import AuthorHistoryTable from 'Author/History/AuthorHistoryTable';
import DeleteBookModal from 'Book/Delete/DeleteBookModal';
import EditBookModalConnector from 'Book/Edit/EditBookModalConnector';
import BookFileEditorTable from 'BookFile/Editor/BookFileEditorTable';
import IconButton from 'Components/Link/IconButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import SwipeHeaderConnector from 'Components/Swipe/SwipeHeaderConnector';
import { icons } from 'Helpers/Props';
import InteractiveSearchFilterMenuConnector from 'InteractiveSearch/InteractiveSearchFilterMenuConnector';
import InteractiveSearchTable from 'InteractiveSearch/InteractiveSearchTable';
import OrganizePreviewModalConnector from 'Organize/OrganizePreviewModalConnector';
import RetagPreviewModalConnector from 'Retag/RetagPreviewModalConnector';
import translate from 'Utilities/String/translate';
import CombineAudiobookModal from 'Book/Combine/CombineAudiobookModal';
import CombineAudiobookProgress from './CombineAudiobookProgress';
import BookDetailsHeaderConnector from './BookDetailsHeaderConnector';
import styles from './BookDetails.css';

function isAudiobookMp3(file) {
  const mediaType = (file.mediaType || '').toString().toLowerCase();
  const isAudiobook = mediaType === 'audiobook' || mediaType === '2';
  const path = (file.path || '').toLowerCase();

  return isAudiobook && path.endsWith('.mp3');
}

class BookDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isRetagModalOpen: false,
      isEditBookModalOpen: false,
      isDeleteBookModalOpen: false,
      isCombineModalOpen: false,
      selectedTabIndex: 0
    };
  }

  //
  // Listeners

  onOrganizePress = () => {
    this.setState({ isOrganizeModalOpen: true });
  };

  onOrganizeModalClose = () => {
    this.setState({ isOrganizeModalOpen: false });
  };

  onRetagPress = () => {
    this.setState({ isRetagModalOpen: true });
  };

  onRetagModalClose = () => {
    this.setState({ isRetagModalOpen: false });
  };

  onEditBookPress = () => {
    this.setState({ isEditBookModalOpen: true });
  };

  onEditBookModalClose = () => {
    this.setState({ isEditBookModalOpen: false });
  };

  onDeleteBookPress = () => {
    this.setState({
      isEditBookModalOpen: false,
      isDeleteBookModalOpen: true
    });
  };

  onDeleteBookModalClose = () => {
    this.setState({ isDeleteBookModalOpen: false });
  };

  onCombineModalOpen = () => {
    this.setState({ isCombineModalOpen: true });
  };

  onCombineModalClose = () => {
    this.setState({ isCombineModalOpen: false });
  };

  onCombineConfirm = (bookFileIds, renameParts) => {
    const { onCombinePress } = this.props;

    onCombinePress(bookFileIds, renameParts);
    this.setState({ isCombineModalOpen: false });
  };

  onTabSelect = (index, lastIndex) => {
    this.setState({ selectedTabIndex: index });
  };

  //
  // Render

  render() {
    const {
      id,
      title,
      isRefreshing,
      isFetching,
      isPopulated,
      bookFilesError,
      bookFiles,
      hasBookFiles,
      author,
      previousBook,
      nextBook,
      isSearching,
      isCombining,
      combineCommand,
      onRefreshPress,
      onSearchPress,
      statistics = {}
    } = this.props;

    const {
      bookFileCount = 0
    } = statistics;

    const {
      isOrganizeModalOpen,
      isRetagModalOpen,
      isEditBookModalOpen,
      isDeleteBookModalOpen,
      isCombineModalOpen,
      selectedTabIndex
    } = this.state;

    const audioFiles = (bookFiles || []).filter(isAudiobookMp3);
    const canCombine = audioFiles.length > 1;

    return (
      <PageContent title={title}>
        <CombineAudiobookProgress command={combineCommand} />
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('Refresh')}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              title={translate('RefreshInformation')}
              isSpinning={isRefreshing}
              onPress={onRefreshPress}
            />

            <PageToolbarButton
              label={translate('SearchBook')}
              iconName={icons.SEARCH}
              isSpinning={isSearching}
              onPress={onSearchPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('PreviewRename')}
              iconName={icons.ORGANIZE}
              isDisabled={!hasBookFiles}
              onPress={this.onOrganizePress}
            />

            <PageToolbarButton
              label={translate('PreviewRetag')}
              iconName={icons.RETAG}
              isDisabled={!hasBookFiles}
              onPress={this.onRetagPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('CombineAudiobook')}
              iconName={icons.ORGANIZE}
              iconClassName={styles.combineIcon}
              isDisabled={!canCombine || isCombining}
              onPress={this.onCombineModalOpen}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('Edit')}
              iconName={icons.EDIT}
              onPress={this.onEditBookPress}
            />

            <PageToolbarButton
              label={translate('Delete')}
              iconName={icons.DELETE}
              onPress={this.onDeleteBookPress}
            />

          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody innerClassName={styles.innerContentBody}>
          <SwipeHeaderConnector
            className={styles.header}
            nextLink={`/book/${nextBook.titleSlug}`}
            nextComponent={(width) => (
              <BookDetailsHeaderConnector
                bookId={nextBook.id}
                author={author}
                width={width}
              />
            )}
            prevLink={`/book/${previousBook.titleSlug}`}
            prevComponent={(width) => (
              <BookDetailsHeaderConnector
                bookId={previousBook.id}
                author={author}
                width={width}
              />
            )}
            currentComponent={(width) => (
              <BookDetailsHeaderConnector
                bookId={id}
                author={author}
                width={width}
              />
            )}
          >
            <div className={styles.bookNavigationButtons}>
              <IconButton
                className={styles.bookNavigationButton}
                name={icons.ARROW_LEFT}
                size={30}
                title={translate('GoToInterp', [previousBook.title])}
                to={`/book/${previousBook.titleSlug}`}
              />

              <IconButton
                className={styles.bookUpButton}
                name={icons.ARROW_UP}
                size={30}
                title={translate('GoToInterp', [author.authorName])}
                to={`/author/${author.titleSlug}`}
              />

              <IconButton
                className={styles.bookNavigationButton}
                name={icons.ARROW_RIGHT}
                size={30}
                title={translate('GoToInterp', [nextBook.title])}
                to={`/book/${nextBook.titleSlug}`}
              />
            </div>
          </SwipeHeaderConnector>

          <div className={styles.contentContainer}>
            {
              !isPopulated && !bookFilesError &&
                <LoadingIndicator />
            }

            {
              !isFetching && bookFilesError &&
                <div>
                  {translate('LoadingBookFilesFailed')}
                </div>
            }

            <Tabs selectedIndex={this.state.tabIndex} onSelect={this.onTabSelect}>
              <TabList
                className={styles.tabList}
              >
                <Tab
                  className={styles.tab}
                  selectedClassName={styles.selectedTab}
                >
                  {translate('History')}
                </Tab>

                <Tab
                  className={styles.tab}
                  selectedClassName={styles.selectedTab}
                >
                  {translate('Search')}
                </Tab>

                <Tab
                  className={styles.tab}
                  selectedClassName={styles.selectedTab}
                >
                  {translate('FilesTotal', [bookFileCount])}
                </Tab>

                {
                  selectedTabIndex === 1 &&
                    <div className={styles.filterIcon}>
                      <InteractiveSearchFilterMenuConnector
                        type="book"
                      />
                    </div>
                }

              </TabList>

              <TabPanel>
                <AuthorHistoryTable
                  authorId={author.id}
                  bookId={id}
                />
              </TabPanel>

              <TabPanel>
                <InteractiveSearchTable
                  bookId={id}
                  type="book"
                />
              </TabPanel>

              <TabPanel>
                <BookFileEditorTable
                  authorId={author.id}
                  bookId={id}
                />
              </TabPanel>
            </Tabs>
          </div>

          <OrganizePreviewModalConnector
            isOpen={isOrganizeModalOpen}
            authorId={author.id}
            bookId={id}
            onModalClose={this.onOrganizeModalClose}
          />

          <RetagPreviewModalConnector
            isOpen={isRetagModalOpen}
            authorId={author.id}
            bookId={id}
            onModalClose={this.onRetagModalClose}
          />

          <EditBookModalConnector
            isOpen={isEditBookModalOpen}
            bookId={id}
            authorId={author.id}
            onModalClose={this.onEditBookModalClose}
            onDeleteAuthorPress={this.onDeleteBookPress}
          />

          <DeleteBookModal
            isOpen={isDeleteBookModalOpen}
            bookId={id}
            authorSlug={author.titleSlug}
            onModalClose={this.onDeleteBookModalClose}
          />

          <CombineAudiobookModal
            isOpen={isCombineModalOpen}
            bookTitle={title}
            files={audioFiles}
            isCombining={isCombining}
            onCombinePress={this.onCombineConfirm}
            onModalClose={this.onCombineModalClose}
          />

        </PageContentBody>
      </PageContent>
    );
  }
}

BookDetails.propTypes = {
  id: PropTypes.number.isRequired,
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string.isRequired,
  pageCount: PropTypes.number,
  overview: PropTypes.string,
  releaseDate: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  statistics: PropTypes.object.isRequired,
  monitored: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  isSaving: PropTypes.bool.isRequired,
  isRefreshing: PropTypes.bool,
  isSearching: PropTypes.bool,
  isFetching: PropTypes.bool,
  isPopulated: PropTypes.bool,
  bookFilesError: PropTypes.object,
  bookFiles: PropTypes.arrayOf(PropTypes.object),
  hasBookFiles: PropTypes.bool.isRequired,
  author: PropTypes.object,
  previousBook: PropTypes.object,
  nextBook: PropTypes.object,
  isSmallScreen: PropTypes.bool.isRequired,
  isCombining: PropTypes.bool.isRequired,
  combineCommand: PropTypes.object,
  onMonitorTogglePress: PropTypes.func.isRequired,
  onRefreshPress: PropTypes.func,
  onSearchPress: PropTypes.func.isRequired,
  onCombinePress: PropTypes.func.isRequired
};

BookDetails.defaultProps = {
  isSaving: false,
  bookFiles: [],
  combineCommand: null
};

export default BookDetails;
