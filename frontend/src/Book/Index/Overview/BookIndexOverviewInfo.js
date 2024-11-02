import PropTypes from 'prop-types';
import React from 'react';
import { icons } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import formatDateTime from 'Utilities/Date/formatDateTime';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import BookIndexOverviewInfoRow from './BookIndexOverviewInfoRow';
import styles from './BookIndexOverviewInfo.css';

const infoRowHeight = parseInt(dimensions.authorIndexOverviewInfoRowHeight);

const rows = [
  {
    name: 'monitored',
    showProp: 'showMonitored',
    valueProp: 'monitored'

  },
  {
    name: 'qualityProfileId',
    showProp: 'showQualityProfile',
    valueProp: 'qualityProfile'
  },
  {
    name: 'releaseDate',
    showProp: 'showReleaseDate',
    valueProp: 'releaseDate'
  },
  {
    name: 'added',
    showProp: 'showAdded',
    valueProp: 'added'
  },
  {
    name: 'path',
    showProp: 'showPath',
    valueProp: 'author'
  },
  {
    name: 'sizeOnDisk',
    showProp: 'showSizeOnDisk',
    valueProp: 'sizeOnDisk'
  }
];

function isVisible(row, props) {
  const {
    name,
    showProp,
    valueProp
  } = row;

  if (props[valueProp] == null) {
    return false;
  }

  return props[showProp] || props.sortKey === name;
}

function getInfoRowProps(row, props) {
  const { name } = row;

  if (name === 'monitored') {
    const monitoredText = props.monitored ? 'Monitored' : 'Unmonitored';

    return {
      title: monitoredText,
      iconName: props.monitored ? icons.MONITORED : icons.UNMONITORED,
      label: monitoredText
    };
  }

  if (name === 'qualityProfileId' && !!props.qualityProfile?.name) {
    return {
      title: translate('QualityProfile'),
      iconName: icons.PROFILE,
      label: props.qualityProfile.name
    };
  }

  if (name === 'releaseDate') {
    const {
      releaseDate,
      showRelativeDates,
      shortDateFormat,
      longDateFormat,
      timeFormat
    } = props;

    return {
      title: `ReleaseDate: ${formatDateTime(releaseDate, longDateFormat, timeFormat)}`,
      iconName: icons.CALENDAR,
      label: getRelativeDate(
        releaseDate,
        shortDateFormat,
        showRelativeDates,
        {
          timeFormat,
          timeForToday: true
        }
      )
    };
  }

  if (name === 'added') {
    const {
      added,
      showRelativeDates,
      shortDateFormat,
      longDateFormat,
      timeFormat
    } = props;

    return {
      title: `Added: ${formatDateTime(added, longDateFormat, timeFormat)}`,
      iconName: icons.ADD,
      label: getRelativeDate(
        added,
        shortDateFormat,
        showRelativeDates,
        {
          timeFormat,
          timeForToday: true
        }
      )
    };
  }

  if (name === 'path') {
    return {
      title: 'Path',
      iconName: icons.FOLDER,
      label: props.author.path
    };
  }

  if (name === 'sizeOnDisk') {
    return {
      title: 'Size on Disk',
      iconName: icons.DRIVE,
      label: formatBytes(props.sizeOnDisk)
    };
  }
}

function BookIndexOverviewInfo(props) {
  const {
    height
  } = props;

  let shownRows = 1;

  const maxRows = Math.floor(height / (infoRowHeight + 4));

  return (
    <div className={styles.infos}>
      {
        rows.map((row) => {
          if (!isVisible(row, props)) {
            return null;
          }

          if (shownRows >= maxRows) {
            return null;
          }

          shownRows++;

          const infoRowProps = getInfoRowProps(row, props);

          return (
            <BookIndexOverviewInfoRow
              key={row.name}
              {...infoRowProps}
            />
          );
        })
      }
    </div>
  );
}

BookIndexOverviewInfo.propTypes = {
  height: PropTypes.number.isRequired,
  showMonitored: PropTypes.bool.isRequired,
  showQualityProfile: PropTypes.bool.isRequired,
  showAdded: PropTypes.bool.isRequired,
  showReleaseDate: PropTypes.bool.isRequired,
  showPath: PropTypes.bool.isRequired,
  showSizeOnDisk: PropTypes.bool.isRequired,
  monitored: PropTypes.bool.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  author: PropTypes.object.isRequired,
  releaseDate: PropTypes.string,
  added: PropTypes.string,
  sizeOnDisk: PropTypes.number,
  sortKey: PropTypes.string.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

export default BookIndexOverviewInfo;
