import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { fetchBookFiles } from 'Store/Actions/bookFileActions';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import FileDetails from './FileDetails';

function createMapStateToProps() {
  return createSelector(
    (state) => state.bookFiles,
    (bookFiles) => {
      return {
        ...bookFiles
      };
    }
  );
}

const mapDispatchToProps = {
  fetchBookFiles
};

class FileDetailsConnector extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      historyItems: [],
      isHistoryFetching: false,
      historyError: null,
      historyKey: null
    };
  }

  componentDidMount() {
    this.props.fetchBookFiles({ id: this.props.id });
  }

  componentDidUpdate() {
    const item = _.find(this.props.items, { id: this.props.id });
    if (!item) {
      return;
    }

    const historyKey = `${item.authorId}-${item.bookId}-${item.id}`;
    if (this.state.historyKey === historyKey) {
      return;
    }

    this.fetchHistory(item, historyKey);
  }

  //
  // Helpers

  fetchHistory = (item, historyKey) => {
    if (!item.authorId || !item.bookId) {
      this.setState({
        historyItems: [],
        isHistoryFetching: false,
        historyError: null,
        historyKey
      });
      return;
    }

    this.setState({ isHistoryFetching: true, historyError: null, historyKey });

    const promise = createAjaxRequest({
      url: '/history/author',
      data: {
        authorId: item.authorId,
        bookId: item.bookId
      }
    }).request;

    promise.done((data) => {
      const historyItems = _.orderBy(data || [], ['date'], ['desc']);

      this.setState({
        historyItems,
        isHistoryFetching: false,
        historyError: null
      });
    });

    promise.fail((xhr) => {
      this.setState({
        historyItems: [],
        isHistoryFetching: false,
        historyError: xhr
      });
    });
  };

  //
  // Render

  render() {
    const {
      items,
      id,
      isFetching,
      error
    } = this.props;

    const item = _.find(items, { id });
    const errorMessage = getErrorMessage(error, 'Unable to load manual import items');

    if (isFetching || !item.audioTags) {
      return (
        <LoadingIndicator />
      );
    } else if (error) {
      return (
        <div>{errorMessage}</div>
      );
    }

    return (
      <FileDetails
        audioTags={item.audioTags}
        filename={item.path}
        historyItems={this.state.historyItems}
        isHistoryFetching={this.state.isHistoryFetching}
        historyError={this.state.historyError}
      />
    );

  }
}

FileDetailsConnector.propTypes = {
  fetchBookFiles: PropTypes.func.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  id: PropTypes.number.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object
};

export default connect(createMapStateToProps, mapDispatchToProps)(FileDetailsConnector);
