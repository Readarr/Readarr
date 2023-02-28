import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAuthorSelector from 'Store/Selectors/createAuthorSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import AuthorTags from './AuthorTags';

function createMapStateToProps() {
  return createSelector(
    createAuthorSelector(),
    createTagsSelector(),
    (author, tagList) => {
      const tags = author.tags
        .map((tagId) => tagList.find((tag) => tag.id === tagId))
        .filter((tag) => !!tag)
        .map((tag) => tag.label)
        .sort((a, b) => a.localeCompare(b));

      return {
        tags
      };
    }
  );
}

export default connect(createMapStateToProps)(AuthorTags);
