import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { addBook, setBookAddDefault } from 'Store/Actions/searchActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import AddNewBookModalContent from './AddNewBookModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { isExistingAuthor }) => isExistingAuthor,
    (state) => state.search,
    (state) => state.settings.metadataProfiles,
    createDimensionsSelector(),
    (isExistingAuthor, searchState, metadataProfiles, dimensions) => {
      const {
        isAdding,
        addError,
        bookDefaults
      } = searchState;

      const {
        settings,
        validationErrors,
        validationWarnings
      } = selectSettings(bookDefaults, {}, addError);

      return {
        isAdding,
        addError,
        showMetadataProfile: true,
        isSmallScreen: dimensions.isSmallScreen,
        validationErrors,
        validationWarnings,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  setBookAddDefault,
  addBook
};

class AddNewBookModalContentConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setBookAddDefault({ [name]: value });
  };

  onAddBookPress = (searchForNewBook) => {
    const {
      foreignBookId,
      rootFolderPath,
      monitor,
      monitorNewItems,
      qualityProfileId,
      metadataProfileId,
      tags
    } = this.props;

    this.props.addBook({
      foreignBookId,
      rootFolderPath: rootFolderPath.value,
      monitor: monitor.value,
      monitorNewItems: monitorNewItems.value,
      qualityProfileId: qualityProfileId.value,
      metadataProfileId: metadataProfileId.value,
      tags: tags.value,
      searchForNewBook
    });
  };

  //
  // Render

  render() {
    return (
      <AddNewBookModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onAddBookPress={this.onAddBookPress}
      />
    );
  }
}

AddNewBookModalContentConnector.propTypes = {
  isExistingAuthor: PropTypes.bool.isRequired,
  foreignBookId: PropTypes.string.isRequired,
  rootFolderPath: PropTypes.object,
  monitor: PropTypes.object.isRequired,
  monitorNewItems: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.object,
  metadataProfileId: PropTypes.object,
  tags: PropTypes.object.isRequired,
  onModalClose: PropTypes.func.isRequired,
  setBookAddDefault: PropTypes.func.isRequired,
  addBook: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddNewBookModalContentConnector);
