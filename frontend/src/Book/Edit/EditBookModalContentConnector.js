import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveBook, setBookValue } from 'Store/Actions/bookActions';
import { saveEditions } from 'Store/Actions/editionActions';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import createBookSelector from 'Store/Selectors/createBookSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import EditBookModalContent from './EditBookModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.books,
    (state) => state.editions,
    createBookSelector(),
    createAuthorSelector(),
    (bookState, editionState, book, author) => {
      const {
        isSaving,
        saveError,
        pendingChanges
      } = bookState;

      const {
        isFetching,
        isPopulated,
        error
      } = editionState;

      const bookSettings = _.pick(book, [
        'monitored',
        'anyEditionOk'
      ]);
      bookSettings.editions = editionState.items;

      const settings = selectSettings(bookSettings, pendingChanges, saveError);

      return {
        title: book.title,
        authorName: author.authorName,
        bookType: book.bookType,
        statistics: book.statistics,
        isFetching,
        isPopulated,
        error,
        isSaving,
        saveError,
        item: settings.settings,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetBookValue: setBookValue,
  dispatchSaveBook: saveBook,
  dispatchSaveEditions: saveEditions
};

class EditBookModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.dispatchSetBookValue({ name, value });
  };

  onSavePress = () => {
    this.props.dispatchSaveBook({
      id: this.props.bookId
    });
    this.props.dispatchSaveEditions({
      id: this.props.bookId
    });
  };

  //
  // Render

  render() {
    return (
      <EditBookModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onSavePress={this.onSavePress}
      />
    );
  }
}

EditBookModalContentConnector.propTypes = {
  bookId: PropTypes.number,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  dispatchSetBookValue: PropTypes.func.isRequired,
  dispatchSaveBook: PropTypes.func.isRequired,
  dispatchSaveEditions: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditBookModalContentConnector);
