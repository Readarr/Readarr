import { push } from 'connected-react-router';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllAuthorSelector from 'Store/Selectors/createAllAuthorsSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import AuthorSearchInput from './AuthorSearchInput';

function createCleanAuthorSelector() {
  return createSelector(
    createAllAuthorSelector(),
    createTagsSelector(),
    (allAuthors, allTags) => {
      return allAuthors.map((author) => {
        const {
          authorName,
          sortName,
          images,
          titleSlug,
          tags = []
        } = author;

        return {
          type: 'author',
          name: authorName,
          sortName,
          titleSlug,
          images,
          firstCharacter: authorName.charAt(0).toLowerCase(),
          tags: tags.reduce((acc, id) => {
            const matchingTag = allTags.find((tag) => tag.id === id);

            if (matchingTag) {
              acc.push(matchingTag);
            }

            return acc;
          }, [])
        };
      });
    }
  );
}

function createCleanBookSelector() {
  return createSelector(
    (state) => state.books.items,
    (allBooks) => {
      return allBooks.map((book) => {
        const {
          title,
          images,
          titleSlug
        } = book;

        return {
          type: 'book',
          name: title,
          sortName: title,
          titleSlug,
          images,
          firstCharacter: title.charAt(0).toLowerCase(),
          tags: []
        };
      });
    }
  );
}

function createMapStateToProps() {
  return createDeepEqualSelector(
    createCleanAuthorSelector(),
    createCleanBookSelector(),
    (authors, books) => {
      const items = [
        ...authors,
        ...books
      ];
      return {
        items
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onGoToAuthor(titleSlug) {
      dispatch(push(`${window.Readarr.urlBase}/author/${titleSlug}`));
    },

    onGoToBook(titleSlug) {
      dispatch(push(`${window.Readarr.urlBase}/book/${titleSlug}`));
    },

    onGoToAddNewAuthor(query) {
      dispatch(push(`${window.Readarr.urlBase}/add/search?term=${encodeURIComponent(query)}`));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(AuthorSearchInput);
