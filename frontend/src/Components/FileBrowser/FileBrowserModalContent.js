import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import PathInput from 'Components/Form/PathInput';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Scroller from 'Components/Scroller/Scroller';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { kinds, scrollDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import FileBrowserRow from './FileBrowserRow';
import styles from './FileBrowserModalContent.css';

const columns = [
  {
    name: 'type',
    label: 'Type',
    isVisible: true
  },
  {
    name: 'name',
    label: 'Name',
    isVisible: true
  }
];

class FileBrowserModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this._scrollerRef = React.createRef();

    this.state = {
      isFileBrowserModalOpen: false,
      currentPath: props.value,
      newFolderName: '',
      isCreatingFolder: false,
      createFolderError: null
    };
  }

  componentDidUpdate(prevProps, prevState) {
    const {
      currentPath
    } = this.props;

    if (
      currentPath !== this.state.currentPath &&
      currentPath !== prevState.currentPath
    ) {
      this.setState({ currentPath });
      this._scrollerRef.current.scrollTop = 0;
    }
  }

  //
  // Listeners

  onPathInputChange = ({ value }) => {
    this.setState({ currentPath: value });
  };

  onRowPress = (path) => {
    this.props.onFetchPaths(path);
  };

  onOkPress = () => {
    this.props.onChange({
      name: this.props.name,
      value: this.state.currentPath
    });

    this.props.onClearPaths();
    this.props.onModalClose();
  };

  onNewFolderNameChange = ({ value }) => {
    this.setState({ newFolderName: value });
  };

  onCreateFolderPress = () => {
    const {
      isWindows,
      onCreateFolder
    } = this.props;

    const {
      currentPath,
      newFolderName
    } = this.state;

    const trimmedName = newFolderName.trim();

    if (!trimmedName) {
      return;
    }

    if (!currentPath) {
      this.setState({ createFolderError: translate('CreateFolderSelectPath') });
      return;
    }

    const basePath = currentPath.replace(/[\\/]+$/, '');
    const separator = isWindows ? '\\' : '/';
    const fullPath = `${basePath}${separator}${trimmedName}`;

    this.setState({ isCreatingFolder: true, createFolderError: null });

    const promise = onCreateFolder(fullPath, currentPath);

    promise.done(() => {
      this.setState({
        newFolderName: '',
        isCreatingFolder: false,
        createFolderError: null
      });
    });

    promise.fail((xhr) => {
      const message = xhr?.responseJSON?.message || xhr?.responseText || translate('CreateFolderFailed');
      this.setState({
        isCreatingFolder: false,
        createFolderError: message
      });
    });
  };

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      error,
      parent,
      directories,
      files,
      isWindows,
      isWindowsService,
      onCreateFolder,
      onModalClose,
      ...otherProps
    } = this.props;

    const emptyParent = parent === '';
    const createDisabled = this.state.isCreatingFolder || !this.state.newFolderName.trim() || !this.state.currentPath;

    return (
      <ModalContent
        onModalClose={onModalClose}
      >
        <ModalHeader>
          File Browser
        </ModalHeader>

        <ModalBody
          className={styles.modalBody}
          scrollDirection={scrollDirections.NONE}
        >
          {
            isWindowsService &&
              <Alert
                className={styles.mappedDrivesWarning}
                kind={kinds.WARNING}
              >
                Mapped network drives are not available when running as a Windows Service, see the <Link className={styles.faqLink} to="https://wiki.servarr.com/readarr/faq">FAQ</Link> for more information.
              </Alert>
          }

          <PathInput
            className={styles.pathInput}
            placeholder={translate('StartTypingOrSelectAPathBelow')}
            hasFileBrowser={false}
            {...otherProps}
            value={this.state.currentPath}
            onChange={this.onPathInputChange}
          />

          <div className={styles.createFolderRow}>
            <TextInput
              className={styles.createFolderInput}
              name="newFolderName"
              placeholder={translate('CreateFolderPlaceholder')}
              value={this.state.newFolderName}
              onChange={this.onNewFolderNameChange}
              spellCheck={false}
            />

            <Button
              className={styles.createFolderButton}
              kind={kinds.PRIMARY}
              isDisabled={createDisabled}
              onPress={this.onCreateFolderPress}
            >
              {translate('CreateFolder')}
            </Button>
          </div>

          {
            this.state.createFolderError &&
              <Alert kind={kinds.DANGER}>
                {this.state.createFolderError}
              </Alert>
          }

          <Scroller
            ref={this._scrollerRef}
            className={styles.scroller}
            scrollDirection={scrollDirections.BOTH}
          >
            {
              !!error &&
                <div>
                  {translate('ErrorLoadingContents')}
                </div>
            }

            {
              isPopulated && !error &&
                <Table
                  horizontalScroll={false}
                  columns={columns}
                >
                  <TableBody>
                    {
                      emptyParent &&
                        <FileBrowserRow
                          type="computer"
                          name="My Computer"
                          path={parent}
                          onPress={this.onRowPress}
                        />
                    }

                    {
                      !emptyParent && parent &&
                        <FileBrowserRow
                          type="parent"
                          name="..."
                          path={parent}
                          onPress={this.onRowPress}
                        />
                    }

                    {
                      directories.map((directory) => {
                        return (
                          <FileBrowserRow
                            key={directory.path}
                            type={directory.type}
                            name={directory.name}
                            path={directory.path}
                            onPress={this.onRowPress}
                          />
                        );
                      })
                    }

                    {
                      files.map((file) => {
                        return (
                          <FileBrowserRow
                            key={file.path}
                            type={file.type}
                            name={file.name}
                            path={file.path}
                            onPress={this.onRowPress}
                          />
                        );
                      })
                    }
                  </TableBody>
                </Table>
            }
          </Scroller>
        </ModalBody>

        <ModalFooter>
          {
            isFetching &&
              <LoadingIndicator
                className={styles.loading}
                size={20}
              />
          }

          <Button
            onPress={onModalClose}
          >
            Cancel
          </Button>

          <Button
            onPress={this.onOkPress}
          >
            Ok
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

FileBrowserModalContent.propTypes = {
  name: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  parent: PropTypes.string,
  currentPath: PropTypes.string.isRequired,
  directories: PropTypes.arrayOf(PropTypes.object).isRequired,
  files: PropTypes.arrayOf(PropTypes.object).isRequired,
  isWindows: PropTypes.bool.isRequired,
  isWindowsService: PropTypes.bool.isRequired,
  onFetchPaths: PropTypes.func.isRequired,
  onClearPaths: PropTypes.func.isRequired,
  onCreateFolder: PropTypes.func.isRequired,
  onChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default FileBrowserModalContent;
