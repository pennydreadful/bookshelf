import { get } from 'lodash';
import monitorOptions from 'Utilities/Author/monitorOptions';

export default function migrateAddBookDefaults(persistedState) {
  const monitor = get(persistedState, 'search.bookDefaults.monitor');

  if (!monitor) {
    return;
  }

  const validOptions = monitorOptions.map((option) => option.key).concat('specificBook');

  if (!validOptions.includes(monitor) || monitor === 'all') {
    persistedState.search.bookDefaults.monitor = 'specificBook';
  }
}
