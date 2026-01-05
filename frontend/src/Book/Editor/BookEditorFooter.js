import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import BookEditorFooterLabel from './BookEditorFooterLabel';
import DeleteBookModal from './Delete/DeleteBookModal';
import styles from './BookEditorFooter.css';

class BookEditorFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDeleteBookModalOpen: false,
      isTagsModalOpen: false,
      isConfirmMoveModalOpen: false,
      destinationRootFolder: null
    };
  }

  //
  // Listeners

  onDeleteSelectedPress = () => {
    this.setState({ isDeleteBookModalOpen: true });
  };

  onDeleteBookModalClose = () => {
    this.setState({ isDeleteBookModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      bookIds,
      selectedCount,
      isDeleting
    } = this.props;

    const {
      isDeleteBookModalOpen
    } = this.state;

    return (
      <PageContentFooter>
        <div className={styles.buttonContainer}>
          <div className={styles.buttonContainerContent}>
            <BookEditorFooterLabel
              label={translate('SelectedCountBooksSelectedInterp', [selectedCount])}
              isSaving={false}
            />

            <div className={styles.buttons}>
              <SpinnerButton
                className={styles.deleteSelectedButton}
                kind={kinds.DANGER}
                isSpinning={isDeleting}
                isDisabled={!selectedCount || isDeleting}
                onPress={this.onDeleteSelectedPress}
              >
                Delete
              </SpinnerButton>
            </div>
          </div>
        </div>

        <DeleteBookModal
          isOpen={isDeleteBookModalOpen}
          bookIds={bookIds}
          onModalClose={this.onDeleteBookModalClose}
        />

      </PageContentFooter>
    );
  }
}

BookEditorFooter.propTypes = {
  bookIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  selectedCount: PropTypes.number.isRequired,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object
};

export default BookEditorFooter;
