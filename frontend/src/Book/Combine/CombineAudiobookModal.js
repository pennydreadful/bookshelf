import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import CombineAudiobookModalContent from './CombineAudiobookModalContent';

function CombineAudiobookModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <CombineAudiobookModalContent
        {...otherProps}
        isOpen={isOpen}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

CombineAudiobookModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default CombineAudiobookModal;
