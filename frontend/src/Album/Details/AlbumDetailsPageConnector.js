import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { push } from 'connected-react-router';
import NotFound from 'Components/NotFound';
import { fetchAlbums, clearAlbums } from 'Store/Actions/albumActions';
import PageContent from 'Components/Page/PageContent';
import PageContentBodyConnector from 'Components/Page/PageContentBodyConnector';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import AlbumDetailsConnector from './AlbumDetailsConnector';

function createMapStateToProps() {
  return createSelector(
    (state, { match }) => match,
    (state) => state.albums,
    (state) => state.artist,
    (match, albums, artist) => {
      const foreignBookId = match.params.foreignBookId;
      const isFetching = albums.isFetching || artist.isFetching;
      const isPopulated = albums.isPopulated && artist.isPopulated;

      // if albums have been fetched, make sure requested one exists
      // otherwise don't map foreignBookId to trigger not found page
      if (!isFetching && isPopulated) {
        const albumIndex = _.findIndex(albums.items, { foreignBookId });
        if (albumIndex === -1) {
          return {
            isFetching,
            isPopulated
          };
        }
      }

      return {
        foreignBookId,
        isFetching,
        isPopulated
      };
    }
  );
}

const mapDispatchToProps = {
  push,
  fetchAlbums,
  clearAlbums
};

class AlbumDetailsPageConnector extends Component {

  constructor(props) {
    super(props);
    this.state = { hasMounted: false };
  }
  //
  // Lifecycle

  componentDidMount() {
    this.populate();
  }

  componentWillUnmount() {
    this.unpopulate();
  }

  //
  // Control

  populate = () => {
    const foreignBookId = this.props.foreignBookId;
    this.setState({ hasMounted: true });
    this.props.fetchAlbums({
      foreignBookId,
      includeAllArtistAlbums: true
    });
  }

  unpopulate = () => {
    this.props.clearAlbums();
  }

  //
  // Render

  render() {
    const {
      foreignBookId,
      isFetching,
      isPopulated
    } = this.props;

    if (!foreignBookId) {
      return (
        <NotFound
          message="Sorry, that album cannot be found."
        />
      );
    }

    if ((isFetching || !this.state.hasMounted) ||
        (!isFetching && !isPopulated)) {
      return (
        <PageContent title='loading'>
          <PageContentBodyConnector>
            <LoadingIndicator />
          </PageContentBodyConnector>
        </PageContent>
      );
    }

    if (!isFetching && isPopulated && this.state.hasMounted) {
      return (
        <AlbumDetailsConnector
          foreignBookId={foreignBookId}
        />
      );
    }
  }
}

AlbumDetailsPageConnector.propTypes = {
  foreignBookId: PropTypes.string,
  match: PropTypes.shape({ params: PropTypes.shape({ foreignBookId: PropTypes.string.isRequired }).isRequired }).isRequired,
  push: PropTypes.func.isRequired,
  fetchAlbums: PropTypes.func.isRequired,
  clearAlbums: PropTypes.func.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AlbumDetailsPageConnector);
