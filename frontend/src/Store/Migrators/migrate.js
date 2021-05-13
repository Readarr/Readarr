import migrateAddAuthorDefaults from './migrateAddAuthorDefaults';
import migrateAuthorSortKey from './migrateAuthorSortKey';

export default function migrate(persistedState) {
  migrateAddAuthorDefaults(persistedState);
  migrateAuthorSortKey(persistedState);
}
