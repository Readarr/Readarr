/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { clearBooks, fetchBooks } from 'Store/Actions/bookActions';
import { setBookFilter, setBookSort, setBookTableOption, setBookView } from 'Store/Actions/bookIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import scrollPositions from 'Store/scrollPositions';
import createBookClientSideCollectionItemsSelector from 'Store/Selectors/createBookClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import BookIndex from './BookIndex';

function createMapStateToProps() {
  return createSelector(
    createBookClientSideCollectionItemsSelector('bookIndex'),
    createCommandExecutingSelector(commandNames.REFRESH_AUTHOR),
    createCommandExecutingSelector(commandNames.REFRESH_BOOK),
    createCommandExecutingSelector(commandNames.RSS_SYNC),
    createDimensionsSelector(),
    (
      book,
      isRefreshingAuthorCommand,
      isRefreshingBookCommand,
      isRssSyncExecuting,
      dimensionsState
    ) => {
      const isRefreshingBook = isRefreshingBookCommand || isRefreshingAuthorCommand;
      return {
        ...book,
        isRefreshingBook,
        isRssSyncExecuting,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setBookTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setBookSort({ sortKey }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setBookFilter({ selectedFilterKey }));
    },

    dispatchSetBookView(view) {
      dispatch(setBookView({ view }));
    },

    onRefreshAuthorPress() {
      dispatch(executeCommand({
        name: commandNames.REFRESH_AUTHOR
      }));
    },

    onRssSyncPress() {
      dispatch(executeCommand({
        name: commandNames.RSS_SYNC
      }));
    },

    dispatchFetchBooks() {
      dispatch(fetchBooks());
    },

    dispatchClearBooks() {
      dispatch(clearBooks());
    }
  };
}

class BookIndexConnector extends Component {

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
    this.props.dispatchFetchBooks();
  }

  unpopulate = () => {
    this.props.dispatchClearBooks();
  }

  //
  // Listeners

  onViewSelect = (view) => {
    this.props.dispatchSetBookView(view);
  }

  onScroll = ({ scrollTop }) => {
    scrollPositions.bookIndex = scrollTop;
  }

  //
  // Render

  render() {
    return (
      <BookIndex
        {...this.props}
        onViewSelect={this.onViewSelect}
        onScroll={this.onScroll}
      />
    );
  }
}

BookIndexConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  view: PropTypes.string.isRequired,
  dispatchFetchBooks: PropTypes.func.isRequired,
  dispatchClearBooks: PropTypes.func.isRequired,
  dispatchSetBookView: PropTypes.func.isRequired
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(BookIndexConnector),
  'bookIndex'
);

