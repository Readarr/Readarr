import _ from 'lodash';

export default function getIndexOfFirstCharacter(items, sortKey, character) {
  return _.findIndex(items, (item) => {
    const firstCharacter = item[sortKey].charAt(0);

    if (character === '#') {
      return !isNaN(firstCharacter);
    }

    return firstCharacter === character;
  });
}
