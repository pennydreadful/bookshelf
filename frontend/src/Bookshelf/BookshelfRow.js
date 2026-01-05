import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AuthorNameLink from 'Author/AuthorNameLink';
import { getAuthorStatusDetails } from 'Author/AuthorStatus';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import BookshelfBook from './BookshelfBook';
import styles from './BookshelfRow.css';

class BookshelfRow extends Component {

  //
  // Render

  render() {
    const {
      authorId,
      status,
      titleSlug,
      authorName,
      books,
      isSelected,
      onSelectedChange
    } = this.props;

    const statusDetails = getAuthorStatusDetails(status);

    return (
      <>
        <VirtualTableSelectCell
          className={styles.selectCell}
          id={authorId}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
          isDisabled={false}
        />

        <VirtualTableRowCell className={styles.status}>
          <Icon
            className={styles.statusIcon}
            name={statusDetails.icon}
            title={statusDetails.title}
          />
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.title}>
          <AuthorNameLink
            titleSlug={titleSlug}
            authorName={authorName}
          />
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.books}>
          {
            books.map((book) => {
              return (
                <BookshelfBook
                  key={book.id}
                  {...book}
                />
              );
            })
          }
        </VirtualTableRowCell>
      </>
    );
  }
}

BookshelfRow.propTypes = {
  authorId: PropTypes.number.isRequired,
  status: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  authorName: PropTypes.string.isRequired,
  books: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

export default BookshelfRow;
