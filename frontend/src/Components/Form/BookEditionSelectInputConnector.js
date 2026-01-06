import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { push } from 'connected-react-router';
import { connect } from 'react-redux';
import { updateItem } from 'Store/Actions/baseActions';
import { fetchBooks } from 'Store/Actions/bookActions';
import { fetchEditions } from 'Store/Actions/editionActions';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import titleCase from 'Utilities/String/titleCase';
import SelectInput from './SelectInput';

class BookEditionSelectInputConnector extends Component {
  state = {
    lookupEditions: null,
    isLookingUp: false,
    isApplying: false
  };

  componentWillUnmount() {
    if (this._abortLookup) {
      this._abortLookup();
      this._abortLookup = null;
    }
  }

  getSourceEditions = () => {
    const bookEditions = this.props.bookEditions?.value ?? [];
    const lookupEditions = this.state.lookupEditions;

    if (!lookupEditions || !lookupEditions.length) {
      return bookEditions;
    }

    const merged = [...lookupEditions];

    bookEditions.forEach((edition) => {
      if (!merged.some((item) => item.foreignEditionId === edition.foreignEditionId)) {
        merged.push(edition);
      }
    });

    return merged;
  };

  buildValues = (editions) => {
    const values = _.map(editions, (bookEdition) => {

      let value = `${bookEdition.title}`;

      if (bookEdition.disambiguation) {
        value = `${value} (${titleCase(bookEdition.disambiguation)})`;
      }

      const extras = [];
      if (bookEdition.language) {
        extras.push(bookEdition.language);
      }
      if (bookEdition.publisher) {
        extras.push(bookEdition.publisher);
      }
      if (bookEdition.isbn13) {
        extras.push(bookEdition.isbn13);
      }
      if (bookEdition.format) {
        extras.push(bookEdition.format);
      }
      if (bookEdition.pageCount > 0) {
        extras.push(`${bookEdition.pageCount}p`);
      }
      if (bookEdition.bookTitle) {
        extras.push(bookEdition.bookTitle);
      }
      if (bookEdition.authorName) {
        extras.push(bookEdition.authorName);
      }

      if (extras.length) {
        value = `${value} [${extras.join(', ')}]`;
      }

      return {
        key: bookEdition.foreignEditionId,
        value
      };
    });

    return _.orderBy(values, ['value']);
  };

  getSelectedValue = () => {
    const bookEditions = this.props.bookEditions?.value ?? [];
    const monitored = _.find(bookEditions, { monitored: true });

    if (monitored) {
      return monitored.foreignEditionId;
    }

    const sourceEditions = this.getSourceEditions();
    return sourceEditions[0]?.foreignEditionId ?? '';
  };

  lookupEditions = () => {
    const { bookId } = this.props;

    if (!bookId || this.state.isLookingUp) {
      return;
    }

    if (this._abortLookup) {
      this._abortLookup();
    }

    this.setState({ isLookingUp: true });

    const { request, abortRequest } = createAjaxRequest({
      url: `/book/${bookId}/edition-lookup`,
      dataType: 'json'
    });

    this._abortLookup = abortRequest;

    request.done((data) => {
      this._abortLookup = null;
      this.setState({
        lookupEditions: Array.isArray(data) ? data : [],
        isLookingUp: false
      });
    });

    request.fail(() => {
      this._abortLookup = null;
      this.setState({ isLookingUp: false });
    });
  };

  applyLookupEdition = (edition) => {
    const {
      bookId,
      fetchEditions,
      fetchBooks,
      push,
      updateBookItem
    } = this.props;

    if (!bookId || !edition?.foreignBookId) {
      return;
    }

    this.setState({ isApplying: true });

    const request = createAjaxRequest({
      url: `/book/${bookId}/select-edition`,
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify({
        foreignBookId: edition.foreignBookId,
        foreignEditionId: edition.foreignEditionId
      })
    }).request;

    request.done((data) => {
      this.setState({ lookupEditions: null });
      if (data?.id && updateBookItem) {
        updateBookItem(data);
      }
      if (fetchBooks) {
        fetchBooks();
      }
      if (data?.titleSlug && push) {
        push(`/book/${data.titleSlug}`);
      }
      if (fetchEditions) {
        fetchEditions({ bookId });
      }
    });

    request.always(() => {
      this.setState({ isApplying: false });
    });
  };

  //
  // Listeners

  onChange = ({ name, value }) => {
    const sourceEditions = this.getSourceEditions();
    const selectedEdition = sourceEditions.find((edition) => edition.foreignEditionId === value);

    if (!selectedEdition) {
      return;
    }

    if (selectedEdition.foreignBookId) {
      this.applyLookupEdition(selectedEdition);
      return;
    }

    const updatedEditions = sourceEditions.map((edition) => ({
      ...edition,
      monitored: edition.foreignEditionId === value
    }));

    this.props.onChange({ name, value: updatedEditions });
  };

  render() {
    const {
      isDisabled,
      ...otherProps
    } = this.props;

    const sourceEditions = this.getSourceEditions();
    const values = this.buildValues(sourceEditions);
    const value = this.getSelectedValue();

    return (
      <SelectInput
        {...otherProps}
        isDisabled={isDisabled || this.state.isApplying}
        values={values}
        value={value}
        onChange={this.onChange}
        onFocus={this.lookupEditions}
      />
    );
  }
}

BookEditionSelectInputConnector.propTypes = {
  bookId: PropTypes.number,
  fetchEditions: PropTypes.func,
  name: PropTypes.string.isRequired,
  onChange: PropTypes.func.isRequired,
  bookEditions: PropTypes.object,
  isDisabled: PropTypes.bool,
  fetchBooks: PropTypes.func,
  push: PropTypes.func,
  updateBookItem: PropTypes.func
};

BookEditionSelectInputConnector.defaultProps = {
  fetchEditions: null,
  isDisabled: false,
  fetchBooks: null,
  push: null,
  updateBookItem: null
};

const mapDispatchToProps = {
  fetchBooks,
  fetchEditions,
  push,
  updateBookItem: (item) => updateItem({ section: 'books', ...item })
};

export default connect(null, mapDispatchToProps)(BookEditionSelectInputConnector);
