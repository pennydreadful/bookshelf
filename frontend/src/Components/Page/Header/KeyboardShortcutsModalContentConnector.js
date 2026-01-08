import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import KeyboardShortcutsModalContent from './KeyboardShortcutsModalContent';

function createMapStateToProps() {
  return createSelector(() => ({}));
}

export default connect(createMapStateToProps)(KeyboardShortcutsModalContent);
