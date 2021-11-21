import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { bulkDeleteBook } from 'Store/Actions/bookIndexActions';
import DeleteBookModalContent from './DeleteBookModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { bookIds }) => bookIds,
    (state) => state.books.items,
    (state) => state.bookFiles.items,
    (bookIds, allBooks, allBookFiles) => {
      const selectedBook = _.intersectionWith(allBooks, bookIds, (s, id) => {
        return s.id === id;
      });

      const sortedBook = _.orderBy(selectedBook, 'title');

      const selectedFiles = _.intersectionWith(allBookFiles, bookIds, (s, id) => {
        return s.bookId === id;
      });

      const files = _.orderBy(selectedFiles, ['bookId', 'path']);

      const book = _.map(sortedBook, (s) => {
        return {
          title: s.title,
          path: s.path
        };
      });

      return {
        book,
        files
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onDeleteSelectedPress(deleteFiles, addImportListExclusion) {
      dispatch(bulkDeleteBook({
        bookIds: props.bookIds,
        deleteFiles,
        addImportListExclusion
      }));

      props.onModalClose();
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DeleteBookModalContent);
