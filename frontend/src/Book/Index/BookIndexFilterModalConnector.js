import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setBookFilter } from 'Store/Actions/bookIndexActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.books.items,
    (state) => state.bookIndex.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'bookIndex'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setBookFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
