import PropTypes from 'prop-types';
import React from 'react';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import translate from 'Utilities/String/translate';
import styles from './BookFileAudioModal.css';

function BookFileAudioModal(props) {
  const {
    isOpen,
    onModalClose,
    streamUrl
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <ModalContent
        onModalClose={onModalClose}
      >
        <ModalHeader>
          {translate('AudiobookPlayer')}
        </ModalHeader>

        <ModalBody>
          <div className={styles.player}>
            <audio
              className={styles.audio}
              controls
              preload="metadata"
              src={streamUrl}
            />
          </div>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            {translate('Close')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

BookFileAudioModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  streamUrl: PropTypes.string.isRequired
};

export default BookFileAudioModal;
