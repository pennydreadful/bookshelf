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
      path,
      quality,
      mediaType
    } = this.props;

    const {
      isDetailsModalOpen,
      isConfirmDeleteModalOpen,
      isAudioModalOpen,
      isReaderModalOpen
    } = this.state;

    const pathLower = path ? path.toLowerCase() : '';
    const extensionIndex = pathLower.lastIndexOf('.');
    const extension = extensionIndex > -1 ? pathLower.slice(extensionIndex) : '';
    const mediaTypeValue = typeof mediaType === 'string' ? mediaType.toLowerCase().trim() : mediaType;
    const isAudioByMediaType = mediaTypeValue === 'audiobook' || mediaTypeValue === 2 || mediaTypeValue === '2';
    const isEbookByMediaType = mediaTypeValue === 'ebook' || mediaTypeValue === 1 || mediaTypeValue === '1';

    const qualityName = quality && quality.quality && quality.quality.name ?
      quality.quality.name.toLowerCase() :
      '';
    const isAudioByQuality = ['m4b', 'mp3', 'm4a', 'aac', 'audiobook'].some((value) => qualityName.includes(value));
    const isEbookByQuality = ['epub', 'pdf', 'ebook'].some((value) => qualityName.includes(value));

    const isAudioByExtension = ['.mp3', '.m4b', '.m4a', '.aac'].some((value) => pathLower.endsWith(value));
    const isEpub = pathLower.endsWith('.epub');
    const isPdf = pathLower.endsWith('.pdf');
    const isEbookByExtension = isEpub || isPdf;

    const isAudio = isAudioByExtension || isAudioByMediaType || isAudioByQuality;
    const isEbook = isEbookByExtension || isEbookByMediaType || isEbookByQuality;
    const fileType = isPdf ? 'pdf' : 'epub';

    const apiKey = window.Readarr && window.Readarr.apiKey;
    const apiKeyQuery = apiKey ? `?apikey=${encodeURIComponent(apiKey)}` : '';
    const streamUrl = path ?
      getPathWithUrlBase(`/api/v1/bookfile/${id}/stream${apiKeyQuery}`) :
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
  quality: PropTypes.object,
  mediaType: PropTypes.oneOfType([PropTypes.string, PropTypes.number]),
  deleteBookFile: PropTypes.func.isRequired
};

export default BookFileActionsCell;
