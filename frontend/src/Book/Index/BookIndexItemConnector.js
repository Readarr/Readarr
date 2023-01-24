/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import createBookAuthorSelector from 'Store/Selectors/createBookAuthorSelector';
import createBookQualityProfileSelector from 'Store/Selectors/createBookQualityProfileSelector';
import createBookSelector from 'Store/Selectors/createBookSelector';
import createExecutingCommandsSelector from 'Store/Selectors/createExecutingCommandsSelector';

function selectShowSearchAction() {
  return createSelector(
    (state) => state.bookIndex,
    (bookIndex) => {
      const view = bookIndex.view;

      switch (view) {
        case 'posters':
          return bookIndex.posterOptions.showSearchAction;
        case 'banners':
          return bookIndex.bannerOptions.showSearchAction;
        case 'overview':
          return bookIndex.overviewOptions.showSearchAction;
        default:
          return bookIndex.tableOptions.showSearchAction;
      }
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createBookSelector(),
    createBookAuthorSelector(),
    createBookQualityProfileSelector(),
    selectShowSearchAction(),
    createExecutingCommandsSelector(),
    (
      book,
      author,
      qualityProfile,
      showSearchAction,
      executingCommands
    ) => {

      // If a book is deleted this selector may fire before the parent
      // selectors, which will result in an undefined book, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show an book that has no information available.

      if (!book) {
        return {};
      }

      const isRefreshingBook = executingCommands.some((command) => {
        return (
          (command.name === commandNames.REFRESH_AUTHOR &&
            command.body.authorId === book.authorId) ||
          (command.name === commandNames.REFRESH_BOOK &&
            command.body.bookId === book.id)
        );
      });

      const isSearchingBook = executingCommands.some((command) => {
        return (
          (command.name === commandNames.AUTHOR_SEARCH &&
            command.body.authorId === book.authorId) ||
          (command.name === commandNames.BOOK_SEARCH &&
            command.body.bookIds.includes(book.id))
        );
      });

      return {
        ...book,
        author,
        qualityProfile,
        showSearchAction,
        isRefreshingBook,
        isSearchingBook
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchExecuteCommand: executeCommand
};

class BookIndexItemConnector extends Component {

  //
  // Listeners

  onRefreshBookPress = () => {
    this.props.dispatchExecuteCommand({
      name: commandNames.REFRESH_BOOK,
      bookId: this.props.id
    });
  };

  onSearchPress = () => {
    this.props.dispatchExecuteCommand({
      name: commandNames.BOOK_SEARCH,
      bookIds: [this.props.id]
    });
  };

  //
  // Render

  render() {
    const {
      id,
      component: ItemComponent,
      ...otherProps
    } = this.props;

    if (!id) {
      return null;
    }

    return (
      <ItemComponent
        {...otherProps}
        id={id}
        onRefreshBookPress={this.onRefreshBookPress}
        onSearchPress={this.onSearchPress}
      />
    );
  }
}

BookIndexItemConnector.propTypes = {
  id: PropTypes.number,
  component: PropTypes.elementType.isRequired,
  dispatchExecuteCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(BookIndexItemConnector);
