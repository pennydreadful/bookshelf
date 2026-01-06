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
    this.state = {
      loadError: false
    };
  }

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
          this.setState({ loadError: true });
          return Promise.reject(new Error('epub reader unavailable'));
        }

        return this.loadEpubFile(this.props.streamUrl);
      })
      .then((epubUrl) => {
        if (!this.isReaderActive || !window.ePub)
        {
          this.setState({ loadError: true });
          return;
        }

        this.book = window.ePub(epubUrl, { openAs: 'epub' });
        this.rendition = this.book.renderTo(container, { width: '100%', height: '100%' });
        this.rendition.display();
      })
      .catch(() => {
        this.isReaderActive = false;
        this.setState({ loadError: true });
      });
  }

  loadEpubFile(streamUrl) {
    if (this.epubObjectUrl)
    {
      URL.revokeObjectURL(this.epubObjectUrl);
      this.epubObjectUrl = null;
    }

    return fetch(streamUrl, { credentials: 'same-origin' })
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
              isEpub ?
                (
                  loadError ?
                    <div className={styles.loadError}>
                      {translate('EbookReaderLoadFailed')}
                    </div> :
                    <div className={styles.reader} ref={this.readerRef} />
                ) :
                <iframe
                  className={styles.pdf}
                  src={streamUrl}
                  title={title}
                />
            }
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
}

BookFileReaderModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  streamUrl: PropTypes.string.isRequired,
  fileType: PropTypes.oneOf(['epub', 'pdf']).isRequired,
  title: PropTypes.string
};

BookFileReaderModal.defaultProps = {
  title: ''
};

export default BookFileReaderModal;
