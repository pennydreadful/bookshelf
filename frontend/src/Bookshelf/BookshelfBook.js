import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import translate from 'Utilities/String/translate';
import styles from './BookshelfBook.css';

class BookshelfBook extends Component {

  //
  // Render

  render() {
    const {
      title,
      disambiguation,
      monitored,
      statistics = {}
    } = this.props;

    const {
      bookCount = 0,
      bookFileCount = 0,
      totalBookCount = 0,
      percentOfBooks = 0
    } = statistics;

    return (
      <div className={styles.book}>
        <div className={styles.info}>
          <span>
            {
              disambiguation ? `${title} (${disambiguation})` : `${title}`
            }
          </span>
        </div>

        <div
          className={classNames(
            styles.books,
            percentOfBooks < 100 && monitored && styles.missingWanted,
            percentOfBooks === 100 && styles.allBooks
          )}
          title={translate('BookProgressBarText', {
            bookCount: bookFileCount ? bookCount : 0,
            bookFileCount,
            totalBookCount
          })}
        >
          {
            totalBookCount === 0 ? '0/0' : `${bookFileCount ? bookCount : 0}/${totalBookCount}`
          }
        </div>
      </div>
    );
  }
}

BookshelfBook.propTypes = {
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  disambiguation: PropTypes.string,
  monitored: PropTypes.bool.isRequired,
  statistics: PropTypes.object.isRequired
};

BookshelfBook.defaultProps = {
  statistics: {
    bookFileCount: 0,
    totalBookCount: 0,
    percentOfBooks: 0
  }
};

export default BookshelfBook;
