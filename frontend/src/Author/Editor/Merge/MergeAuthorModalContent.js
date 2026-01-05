import PropTypes from 'prop-types';
import React from 'react';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import styles from './MergeAuthorModalContent.css';

function MergeAuthorModalContent(props) {
  const {
    authors,
    isMerging,
    mergeError,
    onMergeConfirmed,
    onModalClose
  } = props;

  const hasTwoAuthors = authors.length === 2;
  const leftAuthor = hasTwoAuthors ? authors[0] : null;
  const rightAuthor = hasTwoAuthors ? authors[1] : null;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('MergeAuthorsTitle')}
      </ModalHeader>

      <ModalBody>
        {
          mergeError &&
            <Alert kind={kinds.DANGER}>
              {getErrorMessage(mergeError, translate('MergeAuthorsFailed'))}
            </Alert>
        }

        {
          !hasTwoAuthors &&
            <Alert kind={kinds.WARNING}>
              {translate('MergeAuthorsSelectTwo')}
            </Alert>
        }

        {
          hasTwoAuthors &&
            <>
              <div className={styles.instructions}>
                {translate('MergeAuthorsChooseWinner')}
              </div>

              <div className={styles.compare}>
                <div className={styles.compareColumn}>
                  <div className={styles.compareLabel}>Left</div>
                  <div className={styles.compareName}>{leftAuthor.authorName}</div>
                </div>

                <div className={styles.compareColumn}>
                  <div className={styles.compareLabel}>Right</div>
                  <div className={styles.compareName}>{rightAuthor.authorName}</div>
                </div>
              </div>

              <Alert kind={kinds.WARNING}>
                {translate('MergeAuthorsWarning')}
              </Alert>
            </>
        }
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>
          {translate('Cancel')}
        </Button>

        <SpinnerButton
          kind={kinds.PRIMARY}
          isSpinning={isMerging}
          isDisabled={!hasTwoAuthors || isMerging}
          onPress={() => onMergeConfirmed(leftAuthor.id, rightAuthor.id)}
        >
          {translate('MergeKeepLeft')}
        </SpinnerButton>

        <SpinnerButton
          kind={kinds.PRIMARY}
          isSpinning={isMerging}
          isDisabled={!hasTwoAuthors || isMerging}
          onPress={() => onMergeConfirmed(rightAuthor.id, leftAuthor.id)}
        >
          {translate('MergeKeepRight')}
        </SpinnerButton>
      </ModalFooter>
    </ModalContent>
  );
}

MergeAuthorModalContent.propTypes = {
  authors: PropTypes.arrayOf(PropTypes.object).isRequired,
  isMerging: PropTypes.bool.isRequired,
  mergeError: PropTypes.object,
  onMergeConfirmed: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default MergeAuthorModalContent;
