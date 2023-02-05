import PropTypes from 'prop-types';
import React from 'react';
import ProgressBar from 'Components/ProgressBar';
import { sizes } from 'Helpers/Props';
import getProgressBarKind from 'Utilities/Author/getProgressBarKind';
import translate from 'Utilities/String/translate';
import styles from './BookIndexProgressBar.css';

function BookIndexProgressBar(props) {
  const {
    monitored,
    bookCount,
    bookFileCount,
    totalBookCount,
    posterWidth,
    detailedProgressBar
  } = props;

  const progress = bookCount ? bookFileCount / totalBookCount * 100 : 0;
  const text = `${bookFileCount} / ${bookCount}`;

  return (
    <ProgressBar
      className={styles.progressBar}
      containerClassName={styles.progress}
      progress={100}
      kind={getProgressBarKind('ended', monitored, progress)}
      size={detailedProgressBar ? sizes.MEDIUM : sizes.SMALL}
      showText={detailedProgressBar}
      text={text}
      title={translate('BookFileCountBookCountTotalTotalBookCountInterp', [bookFileCount, bookCount, totalBookCount])}
      width={posterWidth}
    />
  );
}

BookIndexProgressBar.propTypes = {
  monitored: PropTypes.bool.isRequired,
  bookCount: PropTypes.number.isRequired,
  bookFileCount: PropTypes.number.isRequired,
  totalBookCount: PropTypes.number.isRequired,
  posterWidth: PropTypes.number.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired
};

export default BookIndexProgressBar;
