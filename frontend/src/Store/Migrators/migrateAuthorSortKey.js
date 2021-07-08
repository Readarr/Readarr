import { get, set } from 'lodash';

const TABLES_TO_MIGRATE = ['blocklist', 'history', 'queue.paged', 'wanted.missing', 'wanted.cutoffUnmet'];

export default function migrateAuthorSortKey(persistedState) {

  for (const table of TABLES_TO_MIGRATE) {
    const key = `${table}.sortKey`;
    const sortKey = get(persistedState, key);

    if (sortKey === 'authors.sortName') {
      set(persistedState, key, 'authorMetadata.sortName');
    }
  }
}
