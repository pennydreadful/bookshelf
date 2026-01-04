import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import {
  addAuthorBooks,
  clearAuthorAvailableBooks,
  fetchAuthorAvailableBooks
} from 'Store/Actions/authorAvailableBooksActions';
import AuthorDetailsAvailableBooks from './AuthorDetailsAvailableBooks';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authorAvailableBooks,
    (state, props) => props.authorId,
    (authorAvailableBooks, authorId) => {
      const isCurrentAuthor = authorAvailableBooks.authorId === authorId;

      return {
        items: isCurrentAuthor ? authorAvailableBooks.items : [],
        isFetching: isCurrentAuthor ? authorAvailableBooks.isFetching : false,
        isPopulated: isCurrentAuthor ? authorAvailableBooks.isPopulated : false,
        error: isCurrentAuthor ? authorAvailableBooks.error : null,
        isAdding: isCurrentAuthor ? authorAvailableBooks.isAdding : false,
        availableBooksCount: isCurrentAuthor ? authorAvailableBooks.items.length : 0
      };
    }
  );
}

const mapDispatchToProps = {
  addAuthorBooks,
  clearAuthorAvailableBooks,
  fetchAuthorAvailableBooks
};

class AuthorDetailsAvailableBooksConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.populate();
  }

  componentDidUpdate(prevProps) {
    if (prevProps.authorId !== this.props.authorId) {
      this.props.clearAuthorAvailableBooks();
      this.populate();
    }
  }

  componentWillUnmount() {
    this.props.clearAuthorAvailableBooks();
  }

  //
  // Control

  populate = () => {
    this.props.fetchAuthorAvailableBooks({ authorId: this.props.authorId });
  };

  //
  // Listeners

  onAddBookPress = (foreignBookId) => {
    this.props.addAuthorBooks({
      authorId: this.props.authorId,
      foreignBookIds: [foreignBookId]
    });
  };

  //
  // Render

  render() {
    return (
      <AuthorDetailsAvailableBooks
        {...this.props}
        onAddBookPress={this.onAddBookPress}
      />
    );
  }
}

AuthorDetailsAvailableBooksConnector.propTypes = {
  authorId: PropTypes.number.isRequired,
  addAuthorBooks: PropTypes.func.isRequired,
  clearAuthorAvailableBooks: PropTypes.func.isRequired,
  fetchAuthorAvailableBooks: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AuthorDetailsAvailableBooksConnector);
