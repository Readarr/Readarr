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
    bookFile
  } = props;

  const hasBookFile = !!bookFile;

  if (hasBookFile) {
    const quality = bookFile.quality;

    return (
      <div className={styles.center}>
        <BookQuality
          title={quality.quality.name}
          size={bookFile.size}
          quality={quality}
          isMonitored={monitored}
          isCutoffNotMet={bookFile.qualityCutoffNotMet}
        />
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
  bookFile: PropTypes.object
};

export default BookStatus;
