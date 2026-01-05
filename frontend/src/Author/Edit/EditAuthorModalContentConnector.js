import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveAuthor, setAuthorValue } from 'Store/Actions/authorActions';
import { addAuthorBooks } from 'Store/Actions/authorAvailableBooksActions';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import EditAuthorModalContent from './EditAuthorModalContent';

function createIsPathChangingSelector() {
  return createSelector(
    (state) => state.authors.pendingChanges,
    createAuthorSelector(),
    (pendingChanges, author) => {
      const path = pendingChanges.path;

      if (path == null) {
        return false;
      }

      return author.path !== path;
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    (state) => state.authors,
    (state) => state.authorAvailableBooks,
    (state) => state.settings.metadataProfiles,
    createAuthorSelector(),
    createIsPathChangingSelector(),
    (authorsState, authorAvailableBooks, metadataProfiles, author, isPathChanging) => {
      const {
        isSaving,
        saveError,
        pendingChanges
      } = authorsState;

      const authorSettings = _.pick(author, [
        'qualityProfileId',
        'metadataProfileId',
        'path',
        'tags'
      ]);

      const settings = selectSettings(authorSettings, pendingChanges, saveError);
      const isCurrentAuthor = authorAvailableBooks?.authorId === author.id;

      return {
        authorName: author.authorName,
        isSaving,
        saveError,
        isPathChanging,
        originalPath: author.path,
        isAddingAvailableBooks: isCurrentAuthor ? authorAvailableBooks.isAdding : false,
        isAvailableBooksPopulated: isCurrentAuthor ? authorAvailableBooks.isPopulated : false,
        availableBooksCount: isCurrentAuthor ? authorAvailableBooks.items.length : 0,
        item: settings.settings,
        showMetadataProfile: metadataProfiles.items.length > 1,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetAuthorValue: setAuthorValue,
  dispatchSaveAuthor: saveAuthor,
  dispatchAddAuthorBooks: addAuthorBooks
};

class EditAuthorModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.dispatchSetAuthorValue({ name, value });
  };

  onSavePress = (moveFiles) => {
    this.props.dispatchSaveAuthor({
      id: this.props.authorId,
      moveFiles
    });
  };

  onAddAllBooksPress = () => {
    this.props.dispatchAddAuthorBooks({
      authorId: this.props.authorId
    });
  };

  //
  // Render

  render() {
    return (
      <EditAuthorModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onAddAllBooksPress={this.onAddAllBooksPress}
        onSavePress={this.onSavePress}
        onMoveAuthorPress={this.onMoveAuthorPress}
      />
    );
  }
}

EditAuthorModalContentConnector.propTypes = {
  authorId: PropTypes.number,
  isSaving: PropTypes.bool.isRequired,
  isAddingAvailableBooks: PropTypes.bool.isRequired,
  isAvailableBooksPopulated: PropTypes.bool.isRequired,
  availableBooksCount: PropTypes.number.isRequired,
  saveError: PropTypes.object,
  dispatchSetAuthorValue: PropTypes.func.isRequired,
  dispatchSaveAuthor: PropTypes.func.isRequired,
  dispatchAddAuthorBooks: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditAuthorModalContentConnector);
