
function getNewAuthor(author, payload) {
  const {
    rootFolderPath,
    monitor,
    monitorNewItems,
    qualityProfileId,
    metadataProfileId,
    tags,
    status,
    searchForMissingBooks = false
  } = payload;

  const addOptions = {
    monitor,
    searchForMissingBooks
  };

  author.addOptions = addOptions;
  author.monitored = true;
  author.monitorNewItems = monitorNewItems;
  author.qualityProfileId = qualityProfileId;
  author.metadataProfileId = metadataProfileId;
  author.rootFolderPath = rootFolderPath;
  author.tags = tags;
  author.status = status;

  return author;
}

export default getNewAuthor;
