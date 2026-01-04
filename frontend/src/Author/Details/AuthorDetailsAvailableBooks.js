import moment from 'moment';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import BookCover from 'Book/BookCover';
import Alert from 'Components/Alert';
import CheckInput from 'Components/Form/CheckInput';
import IconButton from 'Components/Link/IconButton';
import SpinnerButton from 'Components/Link/SpinnerButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './AuthorDetailsAvailableBooks.css';

function renderReleaseYear(releaseDate) {
  if (!releaseDate) {
    return null;
  }

  return moment(releaseDate).format('YYYY');
}

class AuthorDetailsAvailableBooks extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      selectedState: {},
      isConfirmRemoveOpen: false,
      pendingRemoveIds: [],
      pendingRemoveTitle: ''
    };
  }

  componentDidUpdate(prevProps) {
    if (prevProps.items !== this.props.items) {
      this.clearSelection();
    }
  }

  //
  // Control

  clearSelection = () => {
    this.setState({
      selectedState: {},
      pendingRemoveIds: [],
      pendingRemoveTitle: '',
      isConfirmRemoveOpen: false
    });
  };

  getSelectedIds = () => {
    return Object.keys(this.state.selectedState)
      .filter((id) => this.state.selectedState[id]);
  };

  getSelectionCount = () => {
    return this.getSelectedIds().length;
  };

  //
  // Listeners

  onSelectBookChange = (foreignBookId, { value }) => {
    this.setState((prevState) => ({
      selectedState: {
        ...prevState.selectedState,
        [foreignBookId]: value
      }
    }));
  };

  onSelectAllChange = ({ value }) => {
    const selectedState = {};

    if (value) {
      this.props.items.forEach((item) => {
        selectedState[item.foreignBookId] = true;
      });
    }

    this.setState({ selectedState });
  };

  onAddSelectedPress = () => {
    const selectedIds = this.getSelectedIds();

    if (!selectedIds.length) {
      return;
    }

    this.props.onAddBooksPress(selectedIds);
    this.clearSelection();
  };

  onRemoveSelectedPress = () => {
    const selectedIds = this.getSelectedIds();

    if (!selectedIds.length) {
      return;
    }

    const singleSelected = selectedIds.length === 1 ?
      this.props.items.find((item) => item.foreignBookId === selectedIds[0]) :
      null;

    this.setState({
      isConfirmRemoveOpen: true,
      pendingRemoveIds: selectedIds,
      pendingRemoveTitle: singleSelected?.title ?? ''
    });
  };

  onRemoveBookPress = (item) => {
    this.setState({
      isConfirmRemoveOpen: true,
      pendingRemoveIds: [item.foreignBookId],
      pendingRemoveTitle: item.title
    });
  };

  onConfirmRemove = () => {
    const { pendingRemoveIds } = this.state;

    if (!pendingRemoveIds.length) {
      this.clearSelection();
      return;
    }

    this.props.onExcludeBooksPress(pendingRemoveIds);
    this.clearSelection();
  };

  onCancelRemove = () => {
    this.setState({
      isConfirmRemoveOpen: false,
      pendingRemoveIds: [],
      pendingRemoveTitle: ''
    });
  };

  //
  // Render

  render() {
    const {
      items,
      isFetching,
      error,
      excludeError,
      isAdding,
      isExcluding,
      onAddBookPress
    } = this.props;

    const {
      isConfirmRemoveOpen,
      pendingRemoveIds,
      pendingRemoveTitle
    } = this.state;

    const selectionCount = this.getSelectionCount();
    const allSelected = items.length > 0 && selectionCount === items.length;
    const isWorking = isAdding || isExcluding;

    const confirmTitle = pendingRemoveIds.length > 1 ?
      translate('RemoveAvailableBooksConfirmTitle') :
      translate('RemoveAvailableBookConfirmTitle');

    const confirmMessage = pendingRemoveIds.length > 1 ?
      translate('RemoveAvailableBooksConfirmMessage', [pendingRemoveIds.length]) :
      translate('RemoveAvailableBookConfirmMessage', [pendingRemoveTitle]);

    return (
      <div className={styles.section}>
        <div className={styles.header}>
          <div className={styles.title}>
            {translate('AvailableBooks')}
          </div>

          <div className={styles.headerActions}>
            <div className={styles.selectionInfo}>
              <CheckInput
                name="selectAllAvailableBooks"
                value={allSelected}
                checkedValue={true}
                uncheckedValue={false}
                onChange={this.onSelectAllChange}
                isDisabled={!items.length || isWorking}
              />
              <span>{translate('SelectedCountBooksSelectedInterp', [selectionCount])}</span>
            </div>

            <div className={styles.selectionButtons}>
              <SpinnerButton
                kind={kinds.SUCCESS}
                isSpinning={isAdding}
                isDisabled={!selectionCount || isWorking}
                onPress={this.onAddSelectedPress}
              >
                {translate('AddSelectedBooks')}
              </SpinnerButton>

              <SpinnerButton
                kind={kinds.DANGER}
                isSpinning={isExcluding}
                isDisabled={!selectionCount || isWorking}
                onPress={this.onRemoveSelectedPress}
              >
                {translate('RemoveSelected')}
              </SpinnerButton>
            </div>
          </div>
        </div>

        {
          isFetching &&
            <LoadingIndicator />
        }

      {
        !isFetching && error &&
          <Alert kind={kinds.DANGER}>
            {translate('LoadingBooksFailed')}
          </Alert>
      }

      {
        !isFetching && !error && excludeError &&
          <Alert kind={kinds.DANGER}>
            {translate('RemovingAvailableBooksFailed')}
          </Alert>
      }

        {
          !isFetching && !error && items.length === 0 &&
            <div className={styles.empty}>
              {translate('NoAvailableBooks')}
            </div>
        }

        {
          !isFetching && !error && items.length > 0 &&
            <div className={styles.grid}>
              {
                items.map((item) => {
                  const year = renderReleaseYear(item.releaseDate);
                  const isSelected = !!this.state.selectedState[item.foreignBookId];

                  return (
                    <div
                      key={item.foreignBookId}
                      className={styles.card}
                    >
                      <div className={styles.selectCell}>
                        <CheckInput
                          name={`availableBook-${item.foreignBookId}`}
                          value={isSelected}
                          checkedValue={true}
                          uncheckedValue={false}
                          onChange={(payload) => this.onSelectBookChange(item.foreignBookId, payload)}
                          isDisabled={isWorking}
                        />
                      </div>

                      <BookCover
                        className={styles.cover}
                        images={item.images}
                        size={90}
                      />

                      <div className={styles.meta}>
                        <div className={styles.bookTitle}>
                          {item.title}
                        </div>

                        {
                          item.seriesTitle &&
                            <div className={styles.bookSeries}>
                              {item.seriesTitle}
                            </div>
                        }

                        {
                          year &&
                            <div className={styles.bookYear}>
                              {year}
                            </div>
                        }
                      </div>

                      <IconButton
                        className={styles.addButton}
                        name={icons.ADD}
                        size={16}
                        title={translate('AddNewBook')}
                        isDisabled={isWorking}
                        onPress={() => onAddBookPress(item.foreignBookId)}
                      />

                      <IconButton
                        className={styles.removeButton}
                        name={icons.REMOVE}
                        size={16}
                        title={translate('Remove')}
                        isDisabled={isWorking}
                        onPress={() => this.onRemoveBookPress(item)}
                      />
                    </div>
                  );
                })
              }
            </div>
        }

        <ConfirmModal
          isOpen={isConfirmRemoveOpen}
          title={confirmTitle}
          message={confirmMessage}
          confirmLabel={translate('Remove')}
          cancelLabel={translate('Cancel')}
          isSpinning={isExcluding}
          onConfirm={this.onConfirmRemove}
          onCancel={this.onCancelRemove}
        />
      </div>
    );
  }
}

AuthorDetailsAvailableBooks.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  excludeError: PropTypes.object,
  isAdding: PropTypes.bool.isRequired,
  isExcluding: PropTypes.bool.isRequired,
  onAddBookPress: PropTypes.func.isRequired,
  onAddBooksPress: PropTypes.func.isRequired,
  onExcludeBooksPress: PropTypes.func.isRequired
};

export default AuthorDetailsAvailableBooks;
