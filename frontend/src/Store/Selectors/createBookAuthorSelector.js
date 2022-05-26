import { createSelector } from 'reselect';
import createBookSelector from './createBookSelector';

function createBookAuthorSelector() {
  return createSelector(
    createBookSelector(),
    (state) => state.authors.itemMap,
    (state) => state.authors.items,
    (book, authorMap, allAuthors) => {
      return allAuthors[authorMap[book.authorId]];
    }
  );
}

export default createBookAuthorSelector;
