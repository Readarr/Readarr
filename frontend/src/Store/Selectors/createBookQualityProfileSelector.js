import { createSelector } from 'reselect';
import createBookSelector from './createBookSelector';

function createBookQualityProfileSelector() {
  return createSelector(
    (state) => state.settings.qualityProfiles.items,
    createBookSelector(),
    (qualityProfiles, book = {}) => {
      return qualityProfiles.find((profile) => {
        return profile.id === book.author.qualityProfileId;
      });
    }
  );
}

export default createBookQualityProfileSelector;
