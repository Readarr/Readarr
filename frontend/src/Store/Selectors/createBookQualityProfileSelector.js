import { createSelector } from 'reselect';
import createBookAuthorSelector from './createBookAuthorSelector';

function createBookQualityProfileSelector() {
  return createSelector(
    (state) => state.settings.qualityProfiles.items,
    createBookAuthorSelector(),
    (qualityProfiles, author) => {
      if (!author) {
        return {};
      }

      return qualityProfiles.find((profile) => profile.id === author.qualityProfileId);
    }
  );
}

export default createBookQualityProfileSelector;
