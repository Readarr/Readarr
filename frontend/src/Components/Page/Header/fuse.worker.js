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
  let suggestions = [];

  if (value.length === 1) {
    for (let i = 0; i < items.length; i++) {
      const s = items[i];
      if (s.firstCharacter === value.toLowerCase()) {
        suggestions.push({
          item: items[i],
          indices: [
            [0, 0]
          ],
          matches: [
            {
              value: s.title,
              key: 'title'
            }
          ],
          arrayIndex: 0
        });
        if (suggestions.length > limit) {
          break;
        }
      }
    }
  } else {
    const fuse = new Fuse(items, fuseOptions);
    suggestions = fuse.search(value, { limit });
  }

  return suggestions;
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
