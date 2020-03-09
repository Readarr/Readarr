import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { push } from 'connected-react-router';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import PageContent from 'Components/Page/PageContent';
import PageContentBodyConnector from 'Components/Page/PageContentBodyConnector';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import NotFound from 'Components/NotFound';
import ArtistDetailsConnector from './ArtistDetailsConnector';
import styles from './ArtistDetails.css';

function createMapStateToProps() {
  return createSelector(
    (state, { match }) => match,
    (state) => state.artist,
    (match, artist) => {
      const foreignAuthorId = match.params.foreignAuthorId;
      const {
        isFetching,
        isPopulated,
        error,
        items
      } = artist;

      const artistIndex = _.findIndex(items, { foreignAuthorId });

      if (artistIndex > -1) {
        return {
          isFetching,
          isPopulated,
          foreignAuthorId
        };
      }

      return {
        isFetching,
        isPopulated,
        error
      };
    }
  );
}

const mapDispatchToProps = {
  push
};

class ArtistDetailsPageConnector extends Component {

  //
  // Lifecycle

  componentDidUpdate(prevProps) {
    if (!this.props.foreignAuthorId) {
      this.props.push(`${window.Readarr.urlBase}/`);
      return;
    }
  }

  //
  // Render

  render() {
    const {
      foreignAuthorId,
      isFetching,
      isPopulated,
      error
    } = this.props;

    if (isFetching && !isPopulated) {
      return (
        <PageContent title='loading'>
          <PageContentBodyConnector>
            <LoadingIndicator />
          </PageContentBodyConnector>
        </PageContent>
      );
    }

    if (!isFetching && !!error) {
      return (
        <div className={styles.errorMessage}>
          {getErrorMessage(error, 'Failed to load artist from API')}
        </div>
      );
    }

    if (!foreignAuthorId) {
      return (
        <NotFound
          message="Sorry, that artist cannot be found."
        />
      );
    }

    return (
      <ArtistDetailsConnector
        foreignAuthorId={foreignAuthorId}
      />
    );
  }
}

ArtistDetailsPageConnector.propTypes = {
  foreignAuthorId: PropTypes.string,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  match: PropTypes.shape({ params: PropTypes.shape({ foreignAuthorId: PropTypes.string.isRequired }).isRequired }).isRequired,
  push: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ArtistDetailsPageConnector);
