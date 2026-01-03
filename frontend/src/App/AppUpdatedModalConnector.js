import { connect } from 'react-redux';
import { setAppValue } from 'Store/Actions/appActions';
import AppUpdatedModal from './AppUpdatedModal';

function createMapDispatchToProps(dispatch, props) {
  return {
    onModalClose() {
      dispatch(setAppValue({ isUpdated: false }));
      location.reload();
    }
  };
}

export default connect(null, createMapDispatchToProps)(AppUpdatedModal);
