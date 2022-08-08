import _ from 'lodash';
import { createSelector } from 'reselect';
import filterCollection from 'Utilities/Array/filterCollection';
import sortCollection from 'Utilities/Array/sortCollection';
import createCustomFiltersSelector from './createCustomFiltersSelector';

function createClientSideCollectionSelector(section, uiSection) {
  return createSelector(
    (state) => _.get(state, section),
    (state) => _.get(state, uiSection),
    createCustomFiltersSelector(section, uiSection),
    (sectionState, uiSectionState = {}, customFilters) => {
      const state = Object.assign({}, sectionState, uiSectionState, { customFilters });

      const filtered = filterCollection(state.items, state);
      const sorted = sortCollection(filtered, state);

      return {
        ...sectionState,
        ...uiSectionState,
        customFilters,
        items: sorted,
        totalItems: state.items.length
      };
    }
  );
}

export default createClientSideCollectionSelector;
