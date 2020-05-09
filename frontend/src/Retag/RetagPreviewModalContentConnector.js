import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createArtistSelector from 'Store/Selectors/createArtistSelector';
import { fetchRetagPreview } from 'Store/Actions/retagPreviewActions';
import { executeCommand } from 'Store/Actions/commandActions';
import * as commandNames from 'Commands/commandNames';
import RetagPreviewModalContent from './RetagPreviewModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.retagPreview,
    createArtistSelector(),
    (retagPreview, artist) => {
      const props = { ...retagPreview };
      props.isFetching = retagPreview.isFetching;
      props.isPopulated = retagPreview.isPopulated;
      props.error = retagPreview.error;
      props.path = artist.path;

      return props;
    }
  );
}

const mapDispatchToProps = {
  fetchRetagPreview,
  executeCommand
};

class RetagPreviewModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      authorId,
      bookId
    } = this.props;

    this.props.fetchRetagPreview({
      authorId,
      bookId
    });
  }

  //
  // Listeners

  onRetagPress = (files) => {
    this.props.executeCommand({
      name: commandNames.RETAG_FILES,
      authorId: this.props.authorId,
      files
    });

    this.props.onModalClose();
  }

  //
  // Render

  render() {
    return (
      <RetagPreviewModalContent
        {...this.props}
        onRetagPress={this.onRetagPress}
      />
    );
  }
}

RetagPreviewModalContentConnector.propTypes = {
  authorId: PropTypes.number.isRequired,
  bookId: PropTypes.number,
  isPopulated: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  fetchRetagPreview: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(RetagPreviewModalContentConnector);
