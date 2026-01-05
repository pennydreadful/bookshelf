import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import BookshelfRow from './BookshelfRow';

// Use a const to share the reselect cache between instances
const getBookMap = createSelector(
  (state) => state.books.items,
  (books) => {
    return books.reduce((acc, curr) => {
      (acc[curr.authorId] = acc[curr.authorId] || []).push(curr);
      return acc;
    }, {});
  }
);

function createMapStateToProps() {
  return createSelector(
    createAuthorSelector(),
    getBookMap,
    (author, bookMap) => {
      const booksInAuthor = bookMap.hasOwnProperty(author.id) ? bookMap[author.id] : [];
      const sortedBooks = _.orderBy(booksInAuthor, 'releaseDate', 'desc');

      return {
        ...author,
        authorId: author.id,
        authorName: author.authorName,
        status: author.status,
        books: sortedBooks
      };
    }
  );
}

const mapDispatchToProps = {
};

class BookshelfRowConnector extends Component {

  //
  // Render

  render() {
    return (
      <BookshelfRow
        {...this.props}
      />
    );
  }
}

BookshelfRowConnector.propTypes = {
  authorId: PropTypes.number.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(BookshelfRowConnector);
