import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { scrollDirections } from 'Helpers/Props';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import translate from 'Utilities/String/translate';
import styles from './BookFileReaderModal.css';

const JSZIP_SCRIPT_PATH = '/Content/Scripts/jszip.min.js';
const EPUB_SCRIPT_PATH = '/Content/Scripts/epub.min.js';
let jszipLoadPromise;
let epubLoadPromise;

function getScriptUrl(scriptPath) {
  const apiKey = window.Readarr && window.Readarr.apiKey;
  const query = apiKey ? `?apikey=${encodeURIComponent(apiKey)}` : '';

  return getPathWithUrlBase(`${scriptPath}${query}`);
}

function getJsZipScriptUrl() {
  return getScriptUrl(JSZIP_SCRIPT_PATH);
}

function getEpubScriptUrl() {
  return getScriptUrl(EPUB_SCRIPT_PATH);
}

function loadJsZipScript() {
  if (window.JSZip) {
    return Promise.resolve();
  }

  if (jszipLoadPromise) {
    return jszipLoadPromise;
  }

  jszipLoadPromise = new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = getJsZipScriptUrl();
    script.async = true;
    script.onload = () => {
      if (window.JSZip) {
        resolve();
      } else {
        reject(new Error('JSZip unavailable'));
      }
    };
    script.onerror = () => reject(new Error('Failed to load JSZip'));
    document.body.appendChild(script);
  });

  return jszipLoadPromise;
}

function loadEpubScript() {
  if (window.ePub) {
    return Promise.resolve();
  }

  if (epubLoadPromise) {
    return epubLoadPromise;
  }

  epubLoadPromise = new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = getEpubScriptUrl();
    script.async = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('Failed to load epub.js'));
    document.body.appendChild(script);
  });

  return epubLoadPromise;
}

class BookFileReaderModal extends Component {
  constructor(props) {
    super(props);

    this.readerRef = React.createRef();
    this.book = null;
    this.rendition = null;
    this.epubObjectUrl = null;
    this.isReaderActive = false;
    this.themesRegistered = false;
    this.state = {
      loadError: false
    };
  }

  getReaderThemeName = () => {
    const rawTheme = window.Readarr && window.Readarr.theme ? `${window.Readarr.theme}` : '';
    const theme = rawTheme.toLowerCase();

    if (theme === 'dark' || theme === 'light')
    {
      return theme;
    }

    if (theme === 'auto')
    {
      if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches)
      {
        return 'dark';
      }

      return 'light';
    }

    return 'dark';
  };

  registerReaderThemes = () => {
    if (!this.rendition || !this.rendition.themes || this.themesRegistered)
    {
      return;
    }

    this.themesRegistered = true;

    this.rendition.themes.register('bookdarr-dark', {
      'html, body': {
        'background': '#121212 !important',
        'color': '#f1f1f1 !important'
      },
      'body *': {
        'color': '#f1f1f1 !important'
      },
      a: {
        'color': '#8ab4f8 !important'
      }
    });

    this.rendition.themes.register('bookdarr-light', {
      'html, body': {
        'background': '#ffffff !important',
        'color': '#111111 !important'
      },
      'body *': {
        'color': '#111111 !important'
      },
      a: {
        'color': '#0b57d0 !important'
      }
    });
  };

  applyReaderTheme = () => {
    if (!this.rendition || !this.rendition.themes)
    {
      return;
    }

    this.registerReaderThemes();

    const themeName = this.getReaderThemeName();
    const selectedTheme = themeName === 'light' ? 'bookdarr-light' : 'bookdarr-dark';

    this.rendition.themes.select(selectedTheme);
  };

  onPreviousPress = () => {
    if (this.rendition && this.rendition.prev)
    {
      this.rendition.prev();
    }
  };

  onNextPress = () => {
    if (this.rendition && this.rendition.next)
    {
      this.rendition.next();
    }
  };

  handleReaderError = () => {
    if (!this.isReaderActive)
    {
      return;
    }

    this.isReaderActive = false;
    this.setState({ loadError: true });
  };

  componentDidUpdate(prevProps) {
    const isOpening = this.props.isOpen && !prevProps.isOpen;
    const isClosing = !this.props.isOpen && prevProps.isOpen;
    const changedSource = this.props.isOpen && this.props.streamUrl !== prevProps.streamUrl;

    if (isClosing || changedSource)
    {
      this.cleanupReader();
    }

    if (isOpening || changedSource)
    {
      this.setState({ loadError: false });
      this.initializeReader();
    }
  }

  componentWillUnmount() {
    this.cleanupReader();
  }

  initializeReader() {
    if (this.props.fileType !== 'epub')
    {
      return;
    }

    const container = this.readerRef.current;
    if (!container)
    {
      return;
    }

    this.isReaderActive = true;

    loadJsZipScript()
      .then(() => loadEpubScript())
      .then(() => {
        if (!this.isReaderActive || !window.ePub)
        {
          this.handleReaderError();
          return Promise.reject(new Error('epub reader unavailable'));
        }

        return this.loadEpubFile(this.props.streamUrl);
      })
      .then((epubUrl) => {
        if (!this.isReaderActive || !window.ePub)
        {
          this.handleReaderError();
          return;
        }

        this.book = window.ePub(epubUrl, { openAs: 'epub' });
        this.rendition = this.book.renderTo(container, { width: '100%', height: '100%' });
        this.applyReaderTheme();
        if (this.book.ready && this.book.ready.catch)
        {
          this.book.ready.catch(this.handleReaderError);
        }

        if (this.book.on)
        {
          this.book.on('openFailed', this.handleReaderError);
        }

        if (this.rendition.on)
        {
          this.rendition.on('displayError', this.handleReaderError);
          this.rendition.on('rendered', () => {
            if (this.isReaderActive)
            {
              this.applyReaderTheme();
            }
          });
        }

        const displayPromise = this.rendition.display();
        if (displayPromise && displayPromise.catch)
        {
          displayPromise.catch(this.handleReaderError);
        }
        if (displayPromise && displayPromise.then)
        {
          displayPromise.then(() => null);
        }

        if (this.rendition.resize)
        {
          requestAnimationFrame(() => {
            if (this.isReaderActive && this.readerRef.current && this.rendition)
            {
              this.rendition.resize(this.readerRef.current.clientWidth, this.readerRef.current.clientHeight);
            }
          });
        }
      })
      .catch(() => {
        this.handleReaderError();
      });
  }

  loadEpubFile(streamUrl) {
    if (this.epubObjectUrl)
    {
      URL.revokeObjectURL(this.epubObjectUrl);
      this.epubObjectUrl = null;
    }

    const apiKey = window.Readarr && window.Readarr.apiKey;
    const headers = apiKey ? { 'X-Api-Key': apiKey } : undefined;

    return fetch(streamUrl, { credentials: 'same-origin', headers })
      .then((response) => {
        if (!response.ok)
        {
          throw new Error('Failed to fetch epub');
        }

        return response.blob();
      })
      .then((blob) => {
        this.epubObjectUrl = URL.createObjectURL(blob);
        return this.epubObjectUrl;
      });
  }

  cleanupReader() {
    this.isReaderActive = false;

    if (this.rendition && this.rendition.destroy)
    {
      this.rendition.destroy();
    }

    if (this.book && this.book.destroy)
    {
      this.book.destroy();
    }

    this.rendition = null;
    this.book = null;

    if (this.epubObjectUrl)
    {
      URL.revokeObjectURL(this.epubObjectUrl);
      this.epubObjectUrl = null;
    }

    if (this.readerRef.current)
    {
      this.readerRef.current.innerHTML = '';
    }
  }

  render() {
    const {
      isOpen,
      onModalClose,
      streamUrl,
      fileType,
      title
    } = this.props;

    const { loadError } = this.state;
    const isEpub = fileType === 'epub';
    const isPdf = fileType === 'pdf';
    const isUnsupported = fileType === 'unknown';
    const showNavigation = isEpub && !isUnsupported && !loadError;

    return (
      <Modal
        isOpen={isOpen}
        onModalClose={onModalClose}
      >
        <ModalContent
          onModalClose={onModalClose}
        >
          <ModalHeader>
            {translate('EbookReader')}
          </ModalHeader>

          <ModalBody
            className={styles.body}
            scrollDirection={scrollDirections.NONE}
          >
            {
              isUnsupported ?
                <div className={styles.loadError}>
                  {translate('EbookReaderUnsupportedFormat')}
                </div> :
              isEpub ?
                (
                  loadError ?
                    <div className={styles.loadError}>
                      {translate('EbookReaderLoadFailed')}
                    </div> :
                    <div className={styles.readerContainer}>
                      <div className={styles.reader} ref={this.readerRef} />
                    </div>
                ) :
                (
                  isPdf ?
                    <iframe
                      className={styles.pdf}
                      src={streamUrl}
                      title={title}
                    /> :
                    null
                )
            }
          </ModalBody>

          {
            showNavigation ?
              (
                <div className={styles.readerNav}>
                  <button
                    className={styles.readerNavButton}
                    data-reader-nav="prev"
                    type="button"
                    aria-label={translate('PreviousPage')}
                    onClick={this.onPreviousPress}
                  >
                    {'<'}
                  </button>
                  <button
                    className={styles.readerNavButton}
                    data-reader-nav="next"
                    type="button"
                    aria-label={translate('NextPage')}
                    onClick={this.onNextPress}
                  >
                    {'>'}
                  </button>
                </div>
              ) :
              null
          }

          <ModalFooter>
            <Button onPress={onModalClose}>
              {translate('Close')}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    );
  }
}

BookFileReaderModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  streamUrl: PropTypes.string.isRequired,
  fileType: PropTypes.oneOf(['epub', 'pdf', 'unknown']).isRequired,
  title: PropTypes.string
};

BookFileReaderModal.defaultProps = {
  title: ''
};

export default BookFileReaderModal;
