/* eslint max-params: 0 */
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import BookRow from './BookRow';

const selectBookFiles = createSelector(
  (state) => state.bookFiles,
  (bookFiles) => {
    const { items } = bookFiles;

    return items.reduce((acc, file) => {
      const bookId = file.bookId;
      if (!acc.hasOwnProperty(bookId)) {
        acc[bookId] = [];
      }

      acc[bookId].push(file);

      return acc;
    }, {});
  }
);

function createMapStateToProps() {
  return createSelector(
    createAuthorSelector(),
    selectBookFiles,
    (state, { id }) => id,
    (author = {}, bookFiles, bookId) => {
      const files = bookFiles[bookId] ?? [];
      const bookFile = files[0];

      return {
        authorMonitored: author.monitored,
        authorName: author.authorName,
        bookFiles: files,
        indexerFlags: bookFile ? bookFile.indexerFlags : 0
      };
    }
  );
}
export default connect(createMapStateToProps)(BookRow);
