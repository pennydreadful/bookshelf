import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import CheckInput from 'Components/Form/CheckInput';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Icon from 'Components/Icon';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons } from 'Helpers/Props';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './CombineAudiobookModalContent.css';

function getFileName(path) {
  if (!path) {
    return '';
  }

  const parts = path.split('/');
  return parts[parts.length - 1];
}

function sortFiles(files) {
  return [...files].sort((a, b) => {
    return getFileName(a.path).localeCompare(getFileName(b.path));
  });
}

class CombineAudiobookModalContent extends Component {
  constructor(props) {
    super(props);

    this.state = {
      orderedFiles: sortFiles(props.files),
      dragIndex: null,
      renameParts: false
    };
  }

  componentDidUpdate(prevProps) {
    if (this.props.isOpen && !prevProps.isOpen) {
      this.setState({
        orderedFiles: sortFiles(this.props.files),
        dragIndex: null,
        renameParts: false
      });
    }
  }

  onCheckInputChange = ({ name, value }) => {
    this.setState({ [name]: value });
  };

  onDragStart = (index) => (event) => {
    event.dataTransfer.effectAllowed = 'move';
    this.setState({ dragIndex: index });
  };

  onDragOver = (index) => (event) => {
    event.preventDefault();
    if (index !== this.state.dragIndex) {
      event.dataTransfer.dropEffect = 'move';
    }
  };

  onDrop = (index) => (event) => {
    event.preventDefault();
    const { dragIndex, orderedFiles } = this.state;

    if (dragIndex === null || dragIndex === index) {
      this.setState({ dragIndex: null });
      return;
    }

    const nextFiles = [...orderedFiles];
    const [moved] = nextFiles.splice(dragIndex, 1);
    nextFiles.splice(index, 0, moved);

    this.setState({
      orderedFiles: nextFiles,
      dragIndex: null
    });
  };

  onDragEnd = () => {
    this.setState({ dragIndex: null });
  };

  onCombinePress = () => {
    const {
      onCombinePress
    } = this.props;

    const { orderedFiles, renameParts } = this.state;

    onCombinePress(orderedFiles.map((file) => file.id), renameParts);
  };

  render() {
    const {
      bookTitle,
      isCombining,
      onModalClose
    } = this.props;

    const { orderedFiles, dragIndex, renameParts } = this.state;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('CombineAudiobookModalTitle', [bookTitle])}
        </ModalHeader>

        <ModalBody>
          <div className={styles.description}>
            {translate('CombineAudiobookModalDescription')}
          </div>

          <label className={styles.renamePartsContainer}>
            <span className={styles.renamePartsLabel}>
              {translate('CombineAudiobookRenameParts')}
            </span>

            <CheckInput
              containerClassName={styles.renamePartsInputContainer}
              className={styles.renamePartsInput}
              name="renameParts"
              value={renameParts}
              onChange={this.onCheckInputChange}
            />
          </label>
          <div className={styles.renamePartsHelp}>
            {translate('CombineAudiobookRenamePartsHelpText')}
          </div>

          <div className={styles.list}>
            {
              orderedFiles.map((file, index) => (
                <div
                  key={file.id}
                  className={classNames(
                    styles.row,
                    dragIndex === index && styles.isDragging
                  )}
                  onDragOver={this.onDragOver(index)}
                  onDrop={this.onDrop(index)}
                >
                  <div
                    className={styles.dragHandle}
                    draggable
                    onDragStart={this.onDragStart(index)}
                    onDragEnd={this.onDragEnd}
                    title={translate('DragToReorder')}
                  >
                    <Icon name={icons.REORDER} />
                  </div>

                  <div className={styles.fileName}>
                    {getFileName(file.path)}
                  </div>

                  <div className={styles.fileMeta}>
                    {formatBytes(file.size || 0)}
                  </div>
                </div>
              ))
            }
          </div>
        </ModalBody>

        <ModalFooter>
          <Button
            onPress={onModalClose}
          >
            {translate('Cancel')}
          </Button>

          <SpinnerButton
            isSpinning={isCombining}
            onPress={this.onCombinePress}
          >
            {translate('CombineAudiobook')}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

CombineAudiobookModalContent.propTypes = {
  bookTitle: PropTypes.string.isRequired,
  files: PropTypes.arrayOf(PropTypes.object).isRequired,
  isCombining: PropTypes.bool.isRequired,
  isOpen: PropTypes.bool.isRequired,
  onCombinePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default CombineAudiobookModalContent;
