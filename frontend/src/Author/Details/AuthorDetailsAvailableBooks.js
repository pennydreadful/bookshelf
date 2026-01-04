import moment from 'moment';
import PropTypes from 'prop-types';
import React from 'react';
import BookCover from 'Book/BookCover';
import Alert from 'Components/Alert';
import IconButton from 'Components/Link/IconButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './AuthorDetailsAvailableBooks.css';

function renderReleaseYear(releaseDate) {
  if (!releaseDate) {
    return null;
  }

  return moment(releaseDate).format('YYYY');
}

function AuthorDetailsAvailableBooks(props) {
  const {
    items,
    isFetching,
    error,
    isAdding,
    onAddBookPress
  } = props;

  return (
    <div className={styles.section}>
      <div className={styles.header}>
        <div className={styles.title}>
          {translate('AvailableBooks')}
        </div>
      </div>

      {
        isFetching &&
          <LoadingIndicator />
      }

      {
        !isFetching && error &&
          <Alert kind={kinds.DANGER}>
            {translate('LoadingBooksFailed')}
          </Alert>
      }

      {
        !isFetching && !error && items.length === 0 &&
          <div className={styles.empty}>
            {translate('NoAvailableBooks')}
          </div>
      }

      {
        !isFetching && !error && items.length > 0 &&
          <div className={styles.grid}>
            {
              items.map((item) => {
                const year = renderReleaseYear(item.releaseDate);

                return (
                  <div
                    key={item.foreignBookId}
                    className={styles.card}
                  >
                    <BookCover
                      className={styles.cover}
                      images={item.images}
                      size={90}
                    />

                    <div className={styles.meta}>
                      <div className={styles.bookTitle}>
                        {item.title}
                      </div>

                      {
                        item.seriesTitle &&
                          <div className={styles.bookSeries}>
                            {item.seriesTitle}
                          </div>
                      }

                      {
                        year &&
                          <div className={styles.bookYear}>
                            {year}
                          </div>
                      }
                    </div>

                    <IconButton
                      className={styles.addButton}
                      name={icons.ADD}
                      size={16}
                      title={translate('AddNewBook')}
                      isDisabled={isAdding}
                      onPress={() => onAddBookPress(item.foreignBookId)}
                    />
                  </div>
                );
              })
            }
          </div>
      }
    </div>
  );
}

AuthorDetailsAvailableBooks.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isAdding: PropTypes.bool.isRequired,
  onAddBookPress: PropTypes.func.isRequired
};

export default AuthorDetailsAvailableBooks;
