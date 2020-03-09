import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllArtistSelector from './createAllArtistSelector';

function createExistingArtistSelector() {
  return createSelector(
    (state, { foreignAuthorId }) => foreignAuthorId,
    createAllArtistSelector(),
    (foreignAuthorId, artist) => {
      return _.some(artist, { foreignAuthorId });
    }
  );
}

export default createExistingArtistSelector;
