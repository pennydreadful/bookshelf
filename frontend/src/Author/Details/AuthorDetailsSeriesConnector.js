/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setSeriesSort, setSeriesTableOption } from 'Store/Actions/seriesActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import AuthorDetailsSeries from './AuthorDetailsSeries';

function createMapStateToProps() {
  return createSelector(
    (state, { seriesId }) => seriesId,
    (state) => state.books,
    (state) => state.series,
    createDimensionsSelector(),
    createUISettingsSelector(),
    (seriesId, books, series, dimensions, uiSettings) => {

      const currentSeries = _.find(series.items, { id: seriesId });

      const bookIds = currentSeries.links.map((x) => x.bookId);
      const positionMap = currentSeries.links.reduce((acc, curr) => {
        acc[curr.bookId] = curr.position;
        return acc;
      }, {});

      const booksInSeries = _.filter(books.items, (book) => bookIds.includes(book.id));

      let sortDir = 'asc';

      if (series.sortDirection === 'descending') {
        sortDir = 'desc';
      }

      let sortedBooks = [];
      if (series.sortKey === 'position') {
        sortedBooks = booksInSeries.sort((a, b) => {
          const apos = positionMap[a.id] || '';
          const bpos = positionMap[b.id] || '';
          return apos.localeCompare(bpos, undefined, { numeric: true, sensivity: 'base' });
        });
      } else {
        sortedBooks = _.orderBy(booksInSeries, series.sortKey, sortDir);
      }

      return {
        id: currentSeries.id,
        label: currentSeries.title,
        items: sortedBooks,
        positionMap,
        columns: series.columns,
        sortKey: series.sortKey,
        sortDirection: series.sortDirection,
        isSmallScreen: dimensions.isSmallScreen,
        uiSettings
      };
    }
  );
}

const mapDispatchToProps = {
  setSeriesTableOption,
  dispatchSetSeriesSort: setSeriesSort
};

class AuthorDetailsSeasonConnector extends Component {

  //
  // Listeners

  onTableOptionChange = (payload) => {
    this.props.setSeriesTableOption(payload);
  };

  onSortPress = (sortKey) => {
    this.props.dispatchSetSeriesSort({ sortKey });
  };

  //
  // Render

  render() {
    return (
      <AuthorDetailsSeries
        {...this.props}
        onSortPress={this.onSortPress}
        onTableOptionChange={this.onTableOptionChange}
      />
    );
  }
}

AuthorDetailsSeasonConnector.propTypes = {
  authorId: PropTypes.number.isRequired,
  setSeriesTableOption: PropTypes.func.isRequired,
  dispatchSetSeriesSort: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AuthorDetailsSeasonConnector);
