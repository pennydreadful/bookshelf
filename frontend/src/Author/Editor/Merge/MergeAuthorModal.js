import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import MergeAuthorModalContent from './MergeAuthorModalContent';

function MergeAuthorModal(props) {
  const {
    isOpen,
    onModalClose,
    ...otherProps
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <MergeAuthorModalContent
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

MergeAuthorModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default MergeAuthorModal;
