import PropTypes from 'prop-types';
import React, { Component } from 'react';
import ProgressBar from 'Components/ProgressBar';
import { kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './CombineAudiobookProgress.css';

const progressPrefix = 'CombineAudiobookProgress|';
const completeDisplayMs = 10000;

function parseProgress(message) {
  if (!message || !message.startsWith(progressPrefix)) {
    return null;
  }

  const parts = message.split('|');
  if (parts.length < 3) {
    return null;
  }

  const percent = Number(parts[1]);
  if (Number.isNaN(percent)) {
    return null;
  }

  const currentPart = parts.slice(2).join('|');

  return {
    percent: Math.min(100, Math.max(0, percent)),
    currentPart
  };
}

class CombineAudiobookProgress extends Component {
  constructor(props) {
    super(props);

    this.state = {
      showComplete: false
    };

    this._completeTimeout = null;
  }

  componentDidUpdate(prevProps) {
    const prevStatus = prevProps.command?.status;
    const nextStatus = this.props.command?.status;

    if (nextStatus === 'completed' && prevStatus !== 'completed') {
      this.setState({ showComplete: true });
      this.clearCompleteTimeout();
      this._completeTimeout = setTimeout(() => {
        this.setState({ showComplete: false });
      }, completeDisplayMs);
    }

    if (nextStatus && nextStatus !== 'completed' && this.state.showComplete) {
      this.setState({ showComplete: false });
      this.clearCompleteTimeout();
    }
  }

  componentWillUnmount() {
    this.clearCompleteTimeout();
  }

  clearCompleteTimeout() {
    if (this._completeTimeout) {
      clearTimeout(this._completeTimeout);
      this._completeTimeout = null;
    }
  }

  render() {
    const { command } = this.props;
    const { showComplete } = this.state;

    if (!command && !showComplete) {
      return null;
    }

    if (showComplete) {
      return (
        <div className={styles.container}>
          <ProgressBar
            progress={100}
            showText
            text={translate('CombineAudiobookComplete')}
            kind={kinds.SUCCESS}
            size={sizes.MEDIUM}
          />
        </div>
      );
    }

    const progress = parseProgress(command?.message);
    const percent = progress ? progress.percent : 0;
    const currentPart = progress?.currentPart;
    const detailText = currentPart ?
      translate('CombineAudiobookEncodingFile', [currentPart]) :
      command?.message;

    return (
      <div className={styles.container}>
        <ProgressBar
          progress={percent}
          showText
          text={translate('CombineAudiobookProgressText', [percent.toFixed(0)])}
          kind={kinds.PRIMARY}
          size={sizes.MEDIUM}
        />
        {
          detailText &&
            <div className={styles.detail}>
              {detailText}
            </div>
        }
      </div>
    );
  }
}

CombineAudiobookProgress.propTypes = {
  command: PropTypes.object
};

CombineAudiobookProgress.defaultProps = {
  command: null
};

export default CombineAudiobookProgress;
