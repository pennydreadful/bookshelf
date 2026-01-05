/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { mergeAuthors, saveAuthorEditor, setAuthorFilter, setAuthorSort, setAuthorTableOption, setAuthorView } from 'Store/Actions/authorIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import scrollPositions from 'Store/scrollPositions';
import createAuthorClientSideCollectionItemsSelector from 'Store/Selectors/createAuthorClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import AuthorIndex from './AuthorIndex';

function createMapStateToProps() {
  return createSelector(
    createAuthorClientSideCollectionItemsSelector('authorIndex'),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_AUTHOR),
    createCommandExecutingSelector(commandNames.RSS_SYNC),
    createCommandExecutingSelector(commandNames.RENAME_AUTHOR),
    createCommandExecutingSelector(commandNames.RETAG_AUTHOR),
    createDimensionsSelector(),
    (
      author,
      isRefreshingAuthor,
      isRssSyncExecuting,
      isOrganizingAuthor,
      isRetaggingAuthor,
      dimensionsState
    ) => {
      return {
        ...author,
        isRefreshingAuthor,
        isRssSyncExecuting,
        isOrganizingAuthor,
        isRetaggingAuthor,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setAuthorTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setAuthorSort({ sortKey }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setAuthorFilter({ selectedFilterKey }));
    },

    dispatchSetAuthorView(view) {
      dispatch(setAuthorView({ view }));
    },

    dispatchSaveAuthorEditor(payload) {
      dispatch(saveAuthorEditor(payload));
    },

    dispatchMergeAuthors(payload) {
      dispatch(mergeAuthors(payload));
    },

    onRefreshAuthorPress(items) {
      dispatch(executeCommand({
        name: commandNames.BULK_REFRESH_AUTHOR,
        authorIds: items
      }));
    },

    onRssSyncPress() {
      dispatch(executeCommand({
        name: commandNames.RSS_SYNC
      }));
    }
  };
}

class AuthorIndexConnector extends Component {

  //
  // Listeners

  onViewSelect = (view) => {
    this.props.dispatchSetAuthorView(view);
  };

  onSaveSelected = (payload) => {
    this.props.dispatchSaveAuthorEditor(payload);
  };

  onMergeAuthors = (payload) => {
    this.props.dispatchMergeAuthors(payload);
  };

  onScroll = ({ scrollTop }) => {
    scrollPositions.authorIndex = scrollTop;
  };

  //
  // Render

  render() {
    return (
      <AuthorIndex
        {...this.props}
        onViewSelect={this.onViewSelect}
        onScroll={this.onScroll}
        onSaveSelected={this.onSaveSelected}
        onMergeAuthors={this.onMergeAuthors}
      />
    );
  }
}

AuthorIndexConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  view: PropTypes.string.isRequired,
  dispatchSetAuthorView: PropTypes.func.isRequired,
  dispatchSaveAuthorEditor: PropTypes.func.isRequired,
  dispatchMergeAuthors: PropTypes.func.isRequired
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(AuthorIndexConnector),
  'authorIndex'
);
