import Fuse from 'fuse.js';

const fuseOptions = {
  shouldSort: true,
  includeMatches: true,
  threshold: 0.3,
  location: 0,
  distance: 100,
  minMatchCharLength: 1,
  keys: [
    'authorName',
    'tags.label'
  ]
};

function getSuggestions(authors, value) {
  const limit = 10;
  let suggestions = [];

  if (value.length === 1) {
    for (let i = 0; i < authors.length; i++) {
      const s = authors[i];
      if (s.firstCharacter === value.toLowerCase()) {
        suggestions.push({
          item: authors[i],
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
    const fuse = new Fuse(authors, fuseOptions);
    suggestions = fuse.search(value, { limit });
  }

  return suggestions;
}

onmessage = function(e) {
  if (!e) {
    return;
  }

  const {
    authors,
    value
  } = e.data;

  const suggestions = getSuggestions(authors, value);

  const results = {
    value,
    suggestions
  };

  self.postMessage(results);
};
