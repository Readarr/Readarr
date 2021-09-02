/* eslint max-params: 0 */
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import BookRow from './BookRow';

const selectBookFiles = createSelector(
  (state) => state.bookFiles,
  (bookFiles) => {
    const {
      items
    } = bookFiles;

    const bookFileDict = items.reduce((acc, file) => {
      const bookId = file.bookId;
      if (!acc.hasOwnProperty(bookId)) {
        acc[bookId] = [];
      }

      acc[bookId].push(file);
      return acc;
    }, {});

    return bookFileDict;
  }
);

function createMapStateToProps() {
  return createSelector(
    createAuthorSelector(),
    selectBookFiles,
    (state, { id }) => id,
    (author = {}, bookFiles, bookId) => {
      return {
        authorMonitored: author.monitored,
        bookFiles: bookFiles[bookId] ?? []
      };
    }
  );
}
export default connect(createMapStateToProps)(BookRow);
