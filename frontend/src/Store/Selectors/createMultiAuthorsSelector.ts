import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Author from 'Author/Author';

function createMultiAuthorsSelector(authorIds: number[]) {
  return createSelector(
    (state: AppState) => state.authors.itemMap,
    (state: AppState) => state.authors.items,
    (itemMap, allAuthors) => {
      return authorIds.reduce((acc: Author[], authorId) => {
        const author = allAuthors[itemMap[authorId]];

        if (author) {
          acc.push(author);
        }

        return acc;
      }, []);
    }
  );
}

export default createMultiAuthorsSelector;
