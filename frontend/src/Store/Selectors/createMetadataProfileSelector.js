import { createSelector } from 'reselect';

export function createMetadataProfileSelectorForHook(metadataProfileId) {
  return createSelector(
    (state) => state.settings.metadataProfiles.items,
    (metadataProfiles) => {
      return metadataProfiles.find((profile) => {
        return profile.id === metadataProfileId;
      });
    }
  );
}

function createMetadataProfileSelector() {
  return createSelector(
    (state, { metadataProfileId }) => metadataProfileId,
    (state) => state.settings.metadataProfiles.items,
    (metadataProfileId, metadataProfiles) => {
      return metadataProfiles.find((profile) => {
        return profile.id === metadataProfileId;
      });
    }
  );
}

export default createMetadataProfileSelector;
