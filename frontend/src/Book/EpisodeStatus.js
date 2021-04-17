import PropTypes from 'prop-types';
import React from 'react';
import QueueDetails from 'Activity/Queue/QueueDetails';
import Icon from 'Components/Icon';
import ProgressBar from 'Components/ProgressBar';
import { icons, kinds, sizes } from 'Helpers/Props';
import isBefore from 'Utilities/Date/isBefore';
import translate from 'Utilities/String/translate';
import BookQuality from './BookQuality';
import styles from './EpisodeStatus.css';

function EpisodeStatus(props) {
  const {
    airDateUtc,
    monitored,
    grabbed,
    queueItem,
    bookFile
  } = props;

  const hasBookFile = !!bookFile;
  const isQueued = !!queueItem;
  const hasAired = isBefore(airDateUtc);

  if (isQueued) {
    const {
      sizeleft,
      size
    } = queueItem;

    const progress = (100 - sizeleft / size * 100);

    return (
      <div className={styles.center}>
        <QueueDetails
          {...queueItem}
          progressBar={
            <ProgressBar
              title={translate('BookIsDownloadingInterp', [progress.toFixed(1), queueItem.title])}
              progress={progress}
              kind={kinds.PURPLE}
              size={sizes.MEDIUM}
            />
          }
        />
      </div>
    );
  }

  if (grabbed) {
    return (
      <div className={styles.center}>
        <Icon
          name={icons.DOWNLOADING}
          title={translate('BookIsDownloading')}
        />
      </div>
    );
  }

  if (hasBookFile) {
    const quality = bookFile.quality;
    const isCutoffNotMet = bookFile.qualityCutoffNotMet;

    return (
      <div className={styles.center}>
        <BookQuality
          quality={quality}
          size={bookFile.size}
          isCutoffNotMet={isCutoffNotMet}
          title={translate('BookDownloaded')}
        />
      </div>
    );
  }

  if (!airDateUtc) {
    return (
      <div className={styles.center}>
        <Icon
          name={icons.TBA}
          title={translate('TBA')}
        />
      </div>
    );
  }

  if (!monitored) {
    return (
      <div className={styles.center}>
        <Icon
          name={icons.UNMONITORED}
          title={translate('BookIsNotMonitored')}
        />
      </div>
    );
  }

  if (hasAired) {
    return (
      <div className={styles.center}>
        <Icon
          name={icons.MISSING}
          title={translate('BookMissingFromDisk')}
        />
      </div>
    );
  }

  return (
    <div className={styles.center}>
      <Icon
        name={icons.NOT_AIRED}
        title={translate('BookHasNotAired')}
      />
    </div>
  );
}

EpisodeStatus.propTypes = {
  airDateUtc: PropTypes.string,
  monitored: PropTypes.bool,
  grabbed: PropTypes.bool,
  queueItem: PropTypes.object,
  bookFile: PropTypes.object
};

export default EpisodeStatus;
