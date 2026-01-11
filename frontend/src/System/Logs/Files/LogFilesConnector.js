import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchGeneralSettings } from 'Store/Actions/settingsActions';
import { fetchLogFiles } from 'Store/Actions/systemActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import combinePath from 'Utilities/String/combinePath';
import LogFiles from './LogFiles';

function createMapStateToProps() {
  return createSelector(
    (state) => state.system.logFiles,
    (state) => state.system.status.item,
    (state) => state.settings.general,
    createCommandExecutingSelector(commandNames.DELETE_LOG_FILES),
    (logFiles, status, generalSettings, deleteFilesExecuting) => {
      const {
        isFetching,
        items
      } = logFiles;

      const {
        appData
      } = status;

      const logLevelValue = generalSettings?.item?.logLevel;
      const logLevelLabel = logLevelValue ? `${logLevelValue[0].toUpperCase()}${logLevelValue.slice(1)}` : 'Info';

      return {
        isFetching,
        items,
        deleteFilesExecuting,
        currentLogView: 'Log Files',
        location: combinePath(appData, ['logs']),
        logLevelLabel
      };
    }
  );
}

const mapDispatchToProps = {
  fetchGeneralSettings,
  fetchLogFiles,
  executeCommand
};

class LogFilesConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchLogFiles();
    this.props.fetchGeneralSettings();
  }

  //
  // Listeners

  onRefreshPress = () => {
    this.props.fetchLogFiles();
  };

  onDeleteFilesPress = () => {
    this.props.executeCommand({
      name: commandNames.DELETE_LOG_FILES,
      commandFinished: this.onCommandFinished
    });
  };

  onCommandFinished = () => {
    this.props.fetchLogFiles();
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

LogFilesConnector.propTypes = {
  deleteFilesExecuting: PropTypes.bool.isRequired,
  fetchLogFiles: PropTypes.func.isRequired,
  fetchGeneralSettings: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(LogFilesConnector);
