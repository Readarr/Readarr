import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createMultiAuthorsSelector(authorIds: number[]) {
  return createSelector(
    (state: AppState) => state.authors.itemMap,
    (state: AppState) => state.authors.items,
    (itemMap, allAuthors) => {
      return authorIds.map((authorId) => allAuthors[itemMap[authorId]]);
    }
  );
}

export default createMultiAuthorsSelector;
