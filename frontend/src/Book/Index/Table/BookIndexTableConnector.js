import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setBookSort } from 'Store/Actions/bookIndexActions';
import BookIndexTable from './BookIndexTable';

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.dimensions,
    (state) => state.bookIndex.tableOptions,
    (state) => state.bookIndex.columns,
    (dimensions, tableOptions, columns) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        showBanners: tableOptions.showBanners,
        columns
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSortPress(sortKey) {
      dispatch(setBookSort({ sortKey }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(BookIndexTable);
