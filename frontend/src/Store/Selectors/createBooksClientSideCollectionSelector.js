import _ from 'lodash';
import { createSelector } from 'reselect';
import filterCollection from 'Utilities/Array/filterCollection';
import sortCollection from 'Utilities/Array/sortCollection';
import createCustomFiltersSelector from './createCustomFiltersSelector';

function createBooksClientSideCollectionSelector(uiSection) {
  return createSelector(
    (state) => _.get(state, 'books'),
    (state) => _.get(state, 'authors'),
    (state) => _.get(state, uiSection),
    createCustomFiltersSelector('books', uiSection),
    (bookState, authorState, uiSectionState = {}, customFilters) => {
      const state = Object.assign({}, bookState, uiSectionState, { customFilters });

      const books = state.items;
      for (const book of books) {
        book.author = authorState.items[authorState.itemMap[book.authorId]];
      }

      const filtered = filterCollection(books, state);
      const sorted = sortCollection(filtered, state);

      return {
        ...bookState,
        ...uiSectionState,
        customFilters,
        items: sorted,
        totalItems: state.items.length
      };
    }
  );
}

export default createBooksClientSideCollectionSelector;
