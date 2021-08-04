import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import BookIndexFooter from './BookIndexFooter';

function createUnoptimizedSelector() {
  return createSelector(
    createClientSideCollectionSelector('authors', 'authorIndex'),
    (authors) => {
      return authors.items.map((s) => {
        const {
          monitored,
          status,
          statistics
        } = s;

        return {
          monitored,
          status,
          statistics
        };
      });
    }
  );
}

function createAuthorSelector() {
  return createDeepEqualSelector(
    createUnoptimizedSelector(),
    (author) => author
  );
}

function createMapStateToProps() {
  return createSelector(
    createAuthorSelector(),
    (author) => {
      return {
        author
      };
    }
  );
}

export default connect(createMapStateToProps)(BookIndexFooter);
