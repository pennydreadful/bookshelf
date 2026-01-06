/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { refreshBookMetadata, toggleBooksMonitored } from 'Store/Actions/bookActions';
import { clearBookFiles, fetchBookFiles } from 'Store/Actions/bookFileActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { clearEditions, fetchEditions } from 'Store/Actions/editionActions';
import { cancelFetchReleases, clearReleases } from 'Store/Actions/releaseActions';
import createAllAuthorSelector from 'Store/Selectors/createAllAuthorsSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import { findCommand, isCommandExecuting } from 'Utilities/Command';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import BookDetails from './BookDetails';

const selectBookFiles = createSelector(
  (state) => state.bookFiles,
  (bookFiles) => {
    const {
      items,
      isFetching,
      isPopulated,
      error
    } = bookFiles;

    const hasBookFiles = !!items.length;

    return {
      bookFiles: items,
      isBookFilesFetching: isFetching,
      isBookFilesPopulated: isPopulated,
      bookFilesError: error,
      hasBookFiles
    };
  }
);

function createMapStateToProps() {
  return createSelector(
    (state, { titleSlug }) => titleSlug,
    selectBookFiles,
    (state) => state.books,
    (state) => state.editions,
    createAllAuthorSelector(),
    createCommandsSelector(),
    createUISettingsSelector(),
    createDimensionsSelector(),
    (titleSlug, bookFiles, books, editions, authors, commands, uiSettings, dimensions) => {
      const book = books.items.find((b) => b.titleSlug === titleSlug);
      const author = authors.find((a) => a.id === book.authorId);
      const sortedBooks = books.items.filter((b) => b.authorId === book.authorId);
      sortedBooks.sort((a, b) => ((a.releaseDate > b.releaseDate) ? 1 : -1));
      const bookIndex = sortedBooks.findIndex((b) => b.id === book.id);

      if (!book) {
        return {};
      }

      const {
        bookFiles: bookFileItems,
        isBookFilesFetching,
        isBookFilesPopulated,
        bookFilesError,
        hasBookFiles
      } = bookFiles;

      const previousBook = sortedBooks[bookIndex - 1] || _.last(sortedBooks);
      const nextBook = sortedBooks[bookIndex + 1] || _.first(sortedBooks);
      const isRefreshingCommand = findCommand(commands, { name: commandNames.REFRESH_BOOK });
      const isRefreshing = (
        isCommandExecuting(isRefreshingCommand) &&
        isRefreshingCommand.body.bookId === book.id
      );
      const isSearchingCommand = findCommand(commands, { name: commandNames.BOOK_SEARCH });
      const isSearching = (
        isCommandExecuting(isSearchingCommand) &&
        isSearchingCommand.body.bookIds.indexOf(book.id) > -1
      );
      const rescanCommand = findCommand(commands, {
        name: commandNames.RESCAN_FOLDERS,
        authorIds: [author.id]
      });
      const isRescanningFiles = rescanCommand ? isCommandExecuting(rescanCommand) : false;
      const isRenamingFiles = isCommandExecuting(findCommand(commands, { name: commandNames.RENAME_FILES, authorId: author.id }));
      const isRenamingAuthorCommand = findCommand(commands, { name: commandNames.RENAME_AUTHOR });
      const isRenamingAuthor = (
        isCommandExecuting(isRenamingAuthorCommand) &&
        isRenamingAuthorCommand.body.authorIds.indexOf(author.id) > -1
      );
      const combineCommand = findCommand(commands, { name: commandNames.COMBINE_AUDIOBOOK, bookId: book.id });
      const isCombining = combineCommand ? isCommandExecuting(combineCommand) : false;

      const isFetching = isBookFilesFetching || editions.isFetching;
      const isPopulated = isBookFilesPopulated && editions.isPopulated;

      return {
        ...book,
        shortDateFormat: uiSettings.shortDateFormat,
        author,
        isRefreshing,
        isSearching,
        isRescanningFiles,
        isRenamingFiles,
        isRenamingAuthor,
        isFetching,
        isPopulated,
        bookFilesError,
        bookFiles: bookFileItems,
        hasBookFiles,
        combineCommand,
        isCombining,
        previousBook,
        nextBook,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  executeCommand,
  fetchBookFiles,
  clearBookFiles,
  fetchEditions,
  clearEditions,
  clearReleases,
  cancelFetchReleases,
  refreshBookMetadata,
  toggleBooksMonitored
};

function getMonitoredEditions(props) {
  return _.map(_.filter(props.editions, { monitored: true }), 'id').sort();
}

class BookDetailsConnector extends Component {
  state = {
    isRefreshingMetadata: false
  };

  componentDidMount() {
    registerPagePopulator(this.populate);
    this.populate();
  }

  componentDidUpdate(prevProps) {
    const {
      id,
      anyReleaseOk,
      isRenamingFiles,
      isRenamingAuthor
    } = this.props;

    if (
      (prevProps.isRenamingFiles && !isRenamingFiles) ||
      (prevProps.isRenamingAuthor && !isRenamingAuthor) ||
      !_.isEqual(getMonitoredEditions(prevProps), getMonitoredEditions(this.props)) ||
      (prevProps.anyReleaseOk === false && anyReleaseOk === true)
    ) {
      this.unpopulate();
      this.populate();
    }

    // If the id has changed we need to clear the book
    // files and fetch from the server.

    if (prevProps.id !== id) {
      this.unpopulate();
      this.populate();
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.populate);
    this.unpopulate();
  }

  //
  // Control

  populate = () => {
    const bookId = this.props.id;

    this.props.fetchBookFiles({ bookId });
    this.props.fetchEditions({ bookId });
  };

  unpopulate = () => {
    this.props.cancelFetchReleases();
    this.props.clearReleases();
    this.props.clearBookFiles();
    this.props.clearEditions();
  };

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleBooksMonitored({
      bookIds: [this.props.id],
      monitored
    });
  };

  onRefreshPress = () => {
    this.props.executeCommand({
      name: commandNames.REFRESH_BOOK,
      bookId: this.props.id
    });
  };

  onRefreshMetadataPress = () => {
    this.setState({ isRefreshingMetadata: true });

    const bookId = this.props.id;
    const request = this.props.refreshBookMetadata({ bookId });

    if (request && request.done) {
      request.done(() => {
        this.props.fetchEditions({ bookId });
      });
    }

    if (request && request.always) {
      request.always(() => this.setState({ isRefreshingMetadata: false }));
    } else {
      this.setState({ isRefreshingMetadata: false });
    }
  };

  onRescanFilesPress = () => {
    const { author } = this.props;

    if (!author?.path) {
      return;
    }

    this.props.executeCommand({
      name: commandNames.RESCAN_FOLDERS,
      folders: [author.path],
      authorIds: [author.id],
      filter: 'matched',
      addNewAuthors: false
    });
  };

  onSearchPress = () => {
    this.props.executeCommand({
      name: commandNames.BOOK_SEARCH,
      bookIds: [this.props.id]
    });
  };

  onCombinePress = (bookFileIds, renameParts) => {
    this.props.executeCommand({
      name: commandNames.COMBINE_AUDIOBOOK,
      bookId: this.props.id,
      bookFileIds,
      renameParts,
      suppressMessages: true
    });
  };

  //
  // Render

  render() {
    return (
      <BookDetails
        {...this.props}
        isRefreshingMetadata={this.state.isRefreshingMetadata}
        onMonitorTogglePress={this.onMonitorTogglePress}
        onRefreshPress={this.onRefreshPress}
        onRefreshMetadataPress={this.onRefreshMetadataPress}
        onRescanFilesPress={this.onRescanFilesPress}
        onSearchPress={this.onSearchPress}
        onCombinePress={this.onCombinePress}
      />
    );
  }
}

BookDetailsConnector.propTypes = {
  id: PropTypes.number,
  anyReleaseOk: PropTypes.bool,
  isRenamingFiles: PropTypes.bool.isRequired,
  isRenamingAuthor: PropTypes.bool.isRequired,
  isBookFetching: PropTypes.bool,
  isBookPopulated: PropTypes.bool,
  titleSlug: PropTypes.string.isRequired,
  bookFiles: PropTypes.arrayOf(PropTypes.object),
  fetchBookFiles: PropTypes.func.isRequired,
  clearBookFiles: PropTypes.func.isRequired,
  fetchEditions: PropTypes.func.isRequired,
  clearEditions: PropTypes.func.isRequired,
  clearReleases: PropTypes.func.isRequired,
  cancelFetchReleases: PropTypes.func.isRequired,
  refreshBookMetadata: PropTypes.func.isRequired,
  toggleBooksMonitored: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(BookDetailsConnector);
