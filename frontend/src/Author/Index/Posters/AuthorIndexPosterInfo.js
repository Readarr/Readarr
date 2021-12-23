import PropTypes from 'prop-types';
import React from 'react';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import formatBytes from 'Utilities/Number/formatBytes';
import styles from './AuthorIndexPosterInfo.css';

function AuthorIndexPosterInfo(props) {
  const {
    qualityProfile,
    showQualityProfile,
    metadataProfile,
    added,
    nextBook,
    lastBook,
    bookCount,
    path,
    sizeOnDisk,
    sortKey,
    showRelativeDates,
    shortDateFormat,
    timeFormat
  } = props;

  if (sortKey === 'qualityProfileId' && !showQualityProfile) {
    return (
      <div className={styles.info}>
        {qualityProfile.name}
      </div>
    );
  }

  if (sortKey === 'metadataProfileId') {
    return (
      <div className={styles.info}>
        {metadataProfile.name}
      </div>
    );
  }

  if (sortKey === 'added' && added) {
    const addedDate = getRelativeDate(
      added,
      shortDateFormat,
      showRelativeDates,
      {
        timeFormat,
        timeForToday: false
      }
    );

    return (
      <div className={styles.info}>
        {`Added ${addedDate}`}
      </div>
    );
  }

  if (sortKey === 'nextBook' && nextBook) {
    const date = getRelativeDate(
      nextBook.releaseDate,
      shortDateFormat,
      showRelativeDates,
      {
        timeFormat,
        timeForToday: false
      }
    );

    return (
      <div className={styles.info}>
        {`Next Book ${date}`}
      </div>
    );
  }

  if (sortKey === 'lastBook' && lastBook) {
    const date = getRelativeDate(
      lastBook.releaseDate,
      shortDateFormat,
      showRelativeDates,
      {
        timeFormat,
        timeForToday: false
      }
    );

    return (
      <div className={styles.info}>
        {`Last Book ${date}`}
      </div>
    );
  }

  if (sortKey === 'bookCount') {
    let books = '1 book';

    if (bookCount === 0) {
      books = 'No books';
    } else if (bookCount > 1) {
      books = `${bookCount} books`;
    }

    return (
      <div className={styles.info}>
        {books}
      </div>
    );
  }

  if (sortKey === 'path') {
    return (
      <div className={styles.info}>
        {path}
      </div>
    );
  }

  if (sortKey === 'sizeOnDisk') {
    return (
      <div className={styles.info}>
        {formatBytes(sizeOnDisk)}
      </div>
    );
  }

  return null;
}

AuthorIndexPosterInfo.propTypes = {
  qualityProfile: PropTypes.object.isRequired,
  showQualityProfile: PropTypes.bool.isRequired,
  metadataProfile: PropTypes.object.isRequired,
  added: PropTypes.string,
  nextBook: PropTypes.object,
  lastBook: PropTypes.object,
  bookCount: PropTypes.number.isRequired,
  path: PropTypes.string.isRequired,
  sizeOnDisk: PropTypes.number,
  sortKey: PropTypes.string.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

export default AuthorIndexPosterInfo;
