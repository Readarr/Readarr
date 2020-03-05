import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchArtistHistory, clearArtistHistory, artistHistoryMarkAsFailed } from 'Store/Actions/artistHistoryActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.artistHistory,
    (artistHistory) => {
      return artistHistory;
    }
  );
}

const mapDispatchToProps = {
  fetchArtistHistory,
  clearArtistHistory,
  artistHistoryMarkAsFailed
};

class ArtistHistoryContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      artistId,
      albumId
    } = this.props;

    this.props.fetchArtistHistory({
      artistId,
      albumId
    });
  }

  componentWillUnmount() {
    this.props.clearArtistHistory();
  }

  //
  // Listeners

  onMarkAsFailedPress = (historyId) => {
    const {
      artistId,
      albumId
    } = this.props;

    this.props.artistHistoryMarkAsFailed({
      historyId,
      artistId,
      albumId
    });
  }

  //
  // Render

  render() {
    const {
      component: ViewComponent,
      ...otherProps
    } = this.props;

    return (
      <ViewComponent
        {...otherProps}
        onMarkAsFailedPress={this.onMarkAsFailedPress}
      />
    );
  }
}

ArtistHistoryContentConnector.propTypes = {
  component: PropTypes.elementType.isRequired,
  artistId: PropTypes.number.isRequired,
  albumId: PropTypes.number,
  fetchArtistHistory: PropTypes.func.isRequired,
  clearArtistHistory: PropTypes.func.isRequired,
  artistHistoryMarkAsFailed: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ArtistHistoryContentConnector);
