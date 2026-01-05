/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import AuthorDetailsHeader from './AuthorDetailsHeader';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authors,
    createAuthorSelector(),
    createDimensionsSelector(),
    (authors, author, dimensions) => {
      const alternateTitles = _.reduce(author.alternateTitles, (acc, alternateTitle) => {
        if ((alternateTitle.seasonNumber === -1 || alternateTitle.seasonNumber === undefined) &&
            (alternateTitle.sceneSeasonNumber === -1 || alternateTitle.sceneSeasonNumber === undefined)) {
          acc.push(alternateTitle.title);
        }

        return acc;
      }, []);

      return {
        ...author,
        isSaving: authors.isSaving,
        alternateTitles,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
};

class AuthorDetailsHeaderConnector extends Component {

  //
  // Render

  render() {
    return (
      <AuthorDetailsHeader
        {...this.props}
      />
    );
  }
}

AuthorDetailsHeaderConnector.propTypes = {
  authorId: PropTypes.number.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AuthorDetailsHeaderConnector);
