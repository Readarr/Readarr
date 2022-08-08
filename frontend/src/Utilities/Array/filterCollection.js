import _ from 'lodash';
import { filterTypePredicates, filterTypes } from 'Helpers/Props';
import findSelectedFilters from 'Utilities/Filter/findSelectedFilters';

function filterCollection(items, state) {
  const {
    selectedFilterKey,
    filters,
    customFilters,
    filterPredicates
  } = state;

  if (!selectedFilterKey) {
    return items;
  }

  const selectedFilters = findSelectedFilters(selectedFilterKey, filters, customFilters);

  return _.filter(items, (item) => {
    let i = 0;
    let accepted = true;

    while (accepted && i < selectedFilters.length) {
      const {
        key,
        value,
        type = filterTypes.EQUAL
      } = selectedFilters[i];

      if (filterPredicates && filterPredicates.hasOwnProperty(key)) {
        const predicate = filterPredicates[key];

        if (Array.isArray(value)) {
          if (
            type === filterTypes.NOT_CONTAINS ||
              type === filterTypes.NOT_EQUAL
          ) {
            accepted = value.every((v) => predicate(item, v, type));
          } else {
            accepted = value.some((v) => predicate(item, v, type));
          }
        } else {
          accepted = predicate(item, value, type);
        }
      } else if (item.hasOwnProperty(key)) {
        const predicate = filterTypePredicates[type];

        if (Array.isArray(value)) {
          if (
            type === filterTypes.NOT_CONTAINS ||
              type === filterTypes.NOT_EQUAL
          ) {
            accepted = value.every((v) => predicate(item[key], v));
          } else {
            accepted = value.some((v) => predicate(item[key], v));
          }
        } else {
          accepted = predicate(item[key], value);
        }
      } else {
        // Default to false if the filter can't be tested
        accepted = false;
      }

      i++;
    }

    return accepted;
  });
}

export default filterCollection;
