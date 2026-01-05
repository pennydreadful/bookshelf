import PropTypes from 'prop-types';
import React, { Component } from 'react';
import BookFileAudioModal from 'BookFile/BookFileAudioModal';
import BookFileReaderModal from 'BookFile/BookFileReaderModal';
import FileDetailsModal from 'BookFile/FileDetailsModal';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons, kinds } from 'Helpers/Props';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import translate from 'Utilities/String/translate';
import styles from './BookFileActionsCell.css';

class BookFileActionsCell extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isConfirmDeleteModalOpen: false,
      isAudioModalOpen: false,
      isReaderModalOpen: false
    };
  }

  //
  // Listeners

  onDetailsPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  onDeleteFilePress = () => {
    this.setState({ isConfirmDeleteModalOpen: true });
  };

  onPlayPress = () => {
    this.setState({ isAudioModalOpen: true });
  };

  onReaderPress = () => {
    this.setState({ isReaderModalOpen: true });
  };

  onConfirmDelete = () => {
    this.setState({ isConfirmDeleteModalOpen: false });
    this.props.deleteBookFile({ id: this.props.id });
  };

  onConfirmDeleteModalClose = () => {
    this.setState({ isConfirmDeleteModalOpen: false });
  };

  onAudioModalClose = () => {
    this.setState({ isAudioModalOpen: false });
  };

  onReaderModalClose = () => {
    this.setState({ isReaderModalOpen: false });
  };

  //
  // Render

  render() {

    const {
      id,
      path
    } = this.props;

    const {
      isDetailsModalOpen,
      isConfirmDeleteModalOpen,
      isAudioModalOpen,
      isReaderModalOpen
    } = this.state;

    const extensionIndex = path ? path.lastIndexOf('.') : -1;
    const extension = extensionIndex > -1 ? path.slice(extensionIndex).toLowerCase() : '';
    const isAudio = extension === '.mp3' || extension === '.m4b' || extension === '.m4a';
    const isEpub = extension === '.epub';
    const isPdf = extension === '.pdf';
    const isEbook = isEpub || isPdf;
    const fileType = isEpub ? 'epub' : 'pdf';

    const streamUrl = path ?
      getPathWithUrlBase(`/api/v1/bookfile/${id}/stream?apikey=${encodeURIComponent(window.Readarr.apiKey)}`) :
      null;

    return (
      <TableRowCell className={styles.TrackActionsCell}>
        {
          path &&
            <IconButton
              name={icons.INFO}
              onPress={this.onDetailsPress}
            />
        }
        {
          path && isAudio &&
            <IconButton
              name={icons.PLAY}
              title={translate('PlayAudio')}
              onPress={this.onPlayPress}
            />
        }
        {
          path && isEbook &&
            <IconButton
              name={icons.BOOK_READER}
              title={translate('ReadEbook')}
              onPress={this.onReaderPress}
            />
        }
        {
          path &&
            <IconButton
              name={icons.DELETE}
              onPress={this.onDeleteFilePress}
            />
        }

        <FileDetailsModal
          isOpen={isDetailsModalOpen}
          onModalClose={this.onDetailsModalClose}
          id={id}
        />

        {
          streamUrl && isAudio &&
            <BookFileAudioModal
              isOpen={isAudioModalOpen}
              onModalClose={this.onAudioModalClose}
              streamUrl={streamUrl}
            />
        }
        {
          streamUrl && isEbook &&
            <BookFileReaderModal
              isOpen={isReaderModalOpen}
              onModalClose={this.onReaderModalClose}
              streamUrl={streamUrl}
              fileType={fileType}
              title={path}
            />
        }

        <ConfirmModal
          isOpen={isConfirmDeleteModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteBookFile')}
          message={translate('DeleteBookFileMessageText', [path])}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDelete}
          onCancel={this.onConfirmDeleteModalClose}
        />
      </TableRowCell>

    );
  }
}

BookFileActionsCell.propTypes = {
  id: PropTypes.number.isRequired,
  path: PropTypes.string,
  deleteBookFile: PropTypes.func.isRequired
};

export default BookFileActionsCell;
