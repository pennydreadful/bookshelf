import PropTypes from 'prop-types';
import React, { Component } from 'react';
import DeleteBookModal from 'Book/Delete/DeleteBookModal';
import BookSearchCellConnector from 'Book/BookSearchCellConnector';
import BookTitleLink from 'Book/BookTitleLink';
import IndexerFlags from 'Book/IndexerFlags';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import StarRating from 'Components/StarRating';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import Popover from 'Components/Tooltip/Popover';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import BookStatus from './BookStatus';
import styles from './BookRow.css';

class BookRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isEditBookModalOpen: false,
      isDeleteBookModalOpen: false
    };
  }

  //
  // Listeners

  onManualSearchPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  onEditBookPress = () => {
    this.setState({ isEditBookModalOpen: true });
  };

  onEditBookModalClose = () => {
    this.setState({ isEditBookModalOpen: false });
  };

  onDeleteBookPress = () => {
    this.setState({ isDeleteBookModalOpen: true });
  };

  onDeleteBookModalClose = () => {
    this.setState({ isDeleteBookModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      id,
      authorId,
      monitored,
      releaseDate,
      title,
      seriesTitle,
      authorName,
      authorSlug,
      position,
      pageCount,
      ratings,
      titleSlug,
      bookFiles,
      indexerFlags,
      isEditorActive,
      isSelected,
      onSelectedChange,
      columns
    } = this.props;

    const bookFile = bookFiles[0];
    const isAvailable = Date.parse(releaseDate) < new Date();

    return (
      <TableRow>
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (isEditorActive && name === 'select') {
              return (
                <TableSelectCell
                  key={name}
                  id={id}
                  isSelected={isSelected}
                  isDisabled={false}
                  onSelectedChange={onSelectedChange}
                />
              );
            }

            if (name === 'title') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.title}
                >
                  <BookTitleLink
                    titleSlug={titleSlug}
                    title={title}
                  />
                </TableRowCell>
              );
            }

            if (name === 'series') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.title}
                >
                  {seriesTitle || ''}
                </TableRowCell>
              );
            }

            if (name === 'position') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.position}
                >
                  {position || ''}
                </TableRowCell>
              );
            }

            if (name === 'rating') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.rating}
                >
                  {
                    <StarRating
                      rating={ratings.value}
                      votes={ratings.votes}
                    />
                  }
                </TableRowCell>
              );
            }

            if (name === 'releaseDate') {
              return (
                <RelativeDateCellConnector
                  className={styles.releaseDate}
                  key={name}
                  date={releaseDate}
                />
              );
            }

            if (name === 'pageCount') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.pageCount}
                >
                  {pageCount || ''}
                </TableRowCell>
              );
            }

            if (name === 'indexerFlags') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.indexerFlags}
                >
                  {indexerFlags ? (
                    <Popover
                      anchor={<Icon name={icons.FLAG} kind={kinds.PRIMARY} />}
                      title={translate('IndexerFlags')}
                      body={<IndexerFlags indexerFlags={indexerFlags} />}
                      position={tooltipPositions.LEFT}
                    />
                  ) : null}
                </TableRowCell>
              );
            }

            if (name === 'status') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.status}
                >
                  <BookStatus
                    isAvailable={isAvailable}
                    monitored={monitored}
                    bookFile={bookFile}
                  />
                </TableRowCell>
              );
            }

            if (name === 'actions') {
              return (
                <BookSearchCellConnector
                  key={name}
                  bookId={id}
                  authorId={authorId}
                  bookTitle={title}
                  authorName={authorName}
                >
                  <IconButton
                    name={icons.REMOVE}
                    title={translate('Delete')}
                    onPress={this.onDeleteBookPress}
                  />

                  <DeleteBookModal
                    isOpen={this.state.isDeleteBookModalOpen}
                    bookId={id}
                    authorSlug={authorSlug}
                    onModalClose={this.onDeleteBookModalClose}
                  />
                </BookSearchCellConnector>
              );
            }
            return null;
          })
        }
      </TableRow>
    );
  }
}

BookRow.propTypes = {
  id: PropTypes.number.isRequired,
  authorId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  releaseDate: PropTypes.string,
  title: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string.isRequired,
  authorName: PropTypes.string.isRequired,
  authorSlug: PropTypes.string.isRequired,
  position: PropTypes.string,
  pageCount: PropTypes.number,
  ratings: PropTypes.object.isRequired,
  indexerFlags: PropTypes.number.isRequired,
  titleSlug: PropTypes.string.isRequired,
  bookFiles: PropTypes.arrayOf(PropTypes.object).isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired
};

BookRow.defaultProps = {
  indexerFlags: 0
};

export default BookRow;
