import migrateAddAuthorDefaults from './migrateAddAuthorDefaults';
import migrateAddBookDefaults from './migrateAddBookDefaults';
import migrateAuthorSortKey from './migrateAuthorSortKey';
import migrateBlacklistToBlocklist from './migrateBlacklistToBlocklist';

export default function migrate(persistedState) {
  migrateAddAuthorDefaults(persistedState);
  migrateAddBookDefaults(persistedState);
  migrateAuthorSortKey(persistedState);
  migrateBlacklistToBlocklist(persistedState);
}
