import PropTypes from 'prop-types';
import React from 'react';
import BookQuality from 'Book/BookQuality';
import Label from 'Components/Label';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './BookStatus.css';

function BookStatus(props) {
  const {
    isAvailable,
    monitored,
    bookFiles
  } = props;

  const ebookFiles = [];
  const audiobookFiles = [];

  (bookFiles || []).forEach((file) => {
    const pathLower = file.path ? file.path.toLowerCase() : '';
    const mediaTypeValue = typeof file.mediaType === 'string' ?
      file.mediaType.toLowerCase().trim() :
      file.mediaType;
    const qualityName = file.quality && file.quality.quality && file.quality.quality.name ?
      file.quality.quality.name.toLowerCase() :
      '';

    const isAudioByMediaType = mediaTypeValue === 'audiobook' || mediaTypeValue === 2 || mediaTypeValue === '2';
    const isEbookByMediaType = mediaTypeValue === 'ebook' || mediaTypeValue === 1 || mediaTypeValue === '1';

    const isAudioByQuality = ['m4b', 'mp3', 'm4a', 'aac', 'flac', 'audiobook'].some((value) => qualityName.includes(value));
    const isEbookByQuality = ['epub', 'pdf', 'mobi', 'azw3', 'azw', 'ebook'].some((value) => qualityName.includes(value));

    const isAudioByExtension = ['.mp3', '.m4b', '.m4a', '.aac', '.flac'].some((value) => pathLower.endsWith(value));
    const isEbookByExtension = ['.epub', '.pdf', '.mobi', '.azw3', '.azw'].some((value) => pathLower.endsWith(value));

    const isAudio = isAudioByMediaType || isAudioByQuality || isAudioByExtension;
    const isEbook = isEbookByMediaType || isEbookByQuality || isEbookByExtension;

    if (isAudio && !isEbook) {
      audiobookFiles.push(file);
    } else if (isEbook && !isAudio) {
      ebookFiles.push(file);
    } else if (isAudio) {
      audiobookFiles.push(file);
    } else if (isEbook) {
      ebookFiles.push(file);
    }
  });

  const hasEbook = ebookFiles.length > 0;
  const hasAudiobook = audiobookFiles.length > 0;
  const hasBookFile = hasEbook || hasAudiobook;

  const selectBestFile = (files) => {
    if (!files.length) {
      return null;
    }

    return files
      .slice()
      .sort((left, right) => {
        const leftWeight = left.qualityWeight || 0;
        const rightWeight = right.qualityWeight || 0;
        if (leftWeight !== rightWeight) {
          return rightWeight - leftWeight;
        }

        return (right.size || 0) - (left.size || 0);
      })[0];
  };

  const bestEbook = selectBestFile(ebookFiles);
  const bestAudiobook = selectBestFile(audiobookFiles);

  if (hasBookFile) {
    return (
      <div className={styles.stack}>
        {
          bestEbook ?
            <BookQuality
              title={bestEbook.quality.quality.name}
              size={bestEbook.size}
              quality={bestEbook.quality}
              isMonitored={monitored}
              isCutoffNotMet={bestEbook.qualityCutoffNotMet}
            /> :
            null
        }
        {
          bestAudiobook ?
            <BookQuality
              title={bestAudiobook.quality.quality.name}
              size={bestAudiobook.size}
              quality={bestAudiobook.quality}
              isMonitored={monitored}
              isCutoffNotMet={bestAudiobook.qualityCutoffNotMet}
            /> :
            null
        }
        {
          monitored && isAvailable && !hasEbook ?
            <Label
              title={translate('MissingEbook')}
              kind={kinds.DANGER}
            >
              {translate('MissingEbook')}
            </Label> :
            null
        }
        {
          monitored && isAvailable && !hasAudiobook ?
            <Label
              title={translate('MissingAudiobook')}
              kind={kinds.DANGER}
            >
              {translate('MissingAudiobook')}
            </Label> :
            null
        }
      </div>
    );
  }

  if (!monitored) {
    return (
      <div className={styles.center}>
        <Label
          title={translate('NotMonitored')}
          kind={kinds.WARNING}
        >
          {translate('NotMonitored')}
        </Label>
      </div>
    );
  }

  if (isAvailable) {
    return (
      <div className={styles.center}>
        <Label
          title={translate('BookAvailableButMissing')}
          kind={kinds.DANGER}
        >
          {translate('Missing')}
        </Label>
      </div>
    );
  }

  return (
    <div className={styles.center}>
      <Label
        title={translate('NotAvailable')}
        kind={kinds.INFO}
      >
        {translate('NotAvailable')}
      </Label>
    </div>
  );
}

BookStatus.propTypes = {
  isAvailable: PropTypes.bool,
  monitored: PropTypes.bool.isRequired,
  bookFiles: PropTypes.arrayOf(PropTypes.object)
};

export default BookStatus;
