import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import BookIndexFooter from './BookIndexFooter';

function createUnoptimizedSelector() {
  return createSelector(
    createClientSideCollectionSelector('books', 'bookIndex'),
    (books) => {
      return books.items.map((s) => {
        const {
          authorId,
          monitored,
          status,
          statistics
        } = s;

        return {
          authorId,
          monitored,
          status,
          statistics
        };
      });
    }
  );
}

function createBookSelector() {
  return createDeepEqualSelector(
    createUnoptimizedSelector(),
    (book) => book
  );
}

function createMapStateToProps() {
  return createSelector(
    createBookSelector(),
    (book) => {
      return {
        book
      };
    }
  );
}

export default connect(createMapStateToProps)(BookIndexFooter);
