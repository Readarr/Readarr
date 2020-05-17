import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setBookshelfFilter } from 'Store/Actions/bookshelfActions';
import FilterModal from 'Components/Filter/FilterModal';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authors.items,
    (state) => state.bookshelf.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'bookshelf'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setBookshelfFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
