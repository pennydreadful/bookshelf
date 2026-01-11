import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchGeneralSettings } from 'Store/Actions/settingsActions';
import { fetchUpdateLogFiles } from 'Store/Actions/systemActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import combinePath from 'Utilities/String/combinePath';
import LogFiles from '../Files/LogFiles';

function createMapStateToProps() {
  return createSelector(
    (state) => state.system.updateLogFiles,
    (state) => state.system.status.item,
    (state) => state.settings.general,
    createCommandExecutingSelector(commandNames.DELETE_UPDATE_LOG_FILES),
    (updateLogFiles, status, generalSettings, deleteFilesExecuting) => {
      const {
        isFetching,
        items
      } = updateLogFiles;

      const {
        appData
      } = status;

      const logLevelValue = generalSettings?.item?.logLevel;
      const logLevelLabel = logLevelValue ? `${logLevelValue[0].toUpperCase()}${logLevelValue.slice(1)}` : 'Info';

      return {
        isFetching,
        items,
        deleteFilesExecuting,
        currentLogView: 'Updater Log Files',
        location: combinePath(appData, ['UpdateLogs']),
        logLevelLabel
      };
    }
  );
}

const mapDispatchToProps = {
  fetchGeneralSettings,
  fetchUpdateLogFiles,
  executeCommand
};

class UpdateLogFilesConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchUpdateLogFiles();
    this.props.fetchGeneralSettings();
  }

  componentDidUpdate(prevProps) {
    if (prevProps.deleteFilesExecuting && !this.props.deleteFilesExecuting) {
      this.props.fetchUpdateLogFiles();
    }
  }

  //
  // Listeners

  onRefreshPress = () => {
    this.props.fetchUpdateLogFiles();
  };

  onDeleteFilesPress = () => {
    this.props.executeCommand({ name: commandNames.DELETE_UPDATE_LOG_FILES });
  };

  //
  // Render

  render() {
    return (
      <LogFiles
        onRefreshPress={this.onRefreshPress}
        onDeleteFilesPress={this.onDeleteFilesPress}
        {...this.props}
      />
    );
  }
}

UpdateLogFilesConnector.propTypes = {
  deleteFilesExecuting: PropTypes.bool.isRequired,
  fetchUpdateLogFiles: PropTypes.func.isRequired,
  fetchGeneralSettings: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(UpdateLogFilesConnector);
