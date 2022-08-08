import { createSelector } from 'reselect';

function createCustomFiltersSelector(type, alternateType) {
  return createSelector(
    (state) => state.customFilters.items,
    (customFilters) => {
      return customFilters.filter((customFilter) => {
        return customFilter.type === type || customFilter.type === alternateType;
      });
    }
  );
}

export default createCustomFiltersSelector;
