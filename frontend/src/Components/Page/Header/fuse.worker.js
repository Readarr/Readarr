import Fuse from 'fuse.js';

const fuseOptions = {
  shouldSort: true,
  includeMatches: true,
  threshold: 0.3,
  location: 0,
  distance: 100,
  minMatchCharLength: 1,
  keys: [
    'name',
    'tags.label'
  ]
};

function getSuggestions(items, value) {
  const limit = 10;

  const fuse = new Fuse(items, fuseOptions);
  return fuse.search(value, { limit });
}

onmessage = function(e) {
  if (!e) {
    return;
  }

  const {
    items,
    value
  } = e.data;

  console.log(`got search request ${value} with ${items.length} items`);

  const suggestions = getSuggestions(items, value);

  const results = {
    value,
    suggestions
  };

  console.log(`return ${suggestions.length} results for search ${value}`);

  self.postMessage(results);
};
