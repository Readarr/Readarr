import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Fragment } from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import Link from 'Components/Link/Link';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import styles from './FileDetails.css';

function renderRejections(rejections) {
  return (
    <span>
      <DescriptionListItemTitle>
        Rejections
      </DescriptionListItemTitle>
      {
        _.map(rejections, (item, key) => {
          return (
            <DescriptionListItemDescription key={key}>
              {item.reason}
            </DescriptionListItemDescription>
          );
        })
      }
    </span>
  );
}

function FileDetails(props) {

  const {
    filename,
    audioTags,
    rejections
  } = props;

  return (
    <Fragment>
      <div className={styles.audioTags}>
        <DescriptionList>
          {
            filename &&
              <DescriptionListItem
                title={translate('Filename')}
                data={filename}
                descriptionClassName={styles.filename}
              />
          }
          {
            audioTags.title !== undefined &&
              <DescriptionListItem
                title={translate('TrackTitle')}
                data={audioTags.title}
              />
          }
          {
            audioTags.trackNumbers[0] > 0 &&
              <DescriptionListItem
                title={translate('TrackNumber')}
                data={audioTags.trackNumbers[0]}
              />
          }
          {
            audioTags.discNumber > 0 &&
              <DescriptionListItem
                title={translate('DiscNumber')}
                data={audioTags.discNumber}
              />
          }
          {
            audioTags.discCount > 0 &&
              <DescriptionListItem
                title={translate('DiscCount')}
                data={audioTags.discCount}
              />
          }
          {
            audioTags.bookTitle !== undefined &&
              <DescriptionListItem
                title={translate('Book')}
                data={audioTags.bookTitle}
              />
          }
          {
            audioTags.authorTitle !== undefined &&
              <DescriptionListItem
                title={translate('Author')}
                data={audioTags.authorTitle}
              />
          }
          {
            audioTags.seriesTitle !== undefined &&
              <DescriptionListItem
                title={translate('Series')}
                data={audioTags.seriesTitle}
              />
          }
          {
            audioTags.seriesIndex !== undefined &&
              <DescriptionListItem
                title={translate('SeriesNumber')}
                data={audioTags.seriesIndex}
              />
          }
          {
            audioTags.country !== undefined &&
              <DescriptionListItem
                title={translate('Country')}
                data={audioTags.country.name}
              />
          }
          {
            audioTags.language !== undefined && audioTags.language !== 'UND' &&
              <DescriptionListItem
                title={translate('Language')}
                data={audioTags.language}
              />
          }
          {
            audioTags.year > 0 &&
              <DescriptionListItem
                title={translate('Year')}
                data={audioTags.year}
              />
          }
          {
            audioTags.label !== undefined &&
              <DescriptionListItem
                title={translate('Label')}
                data={audioTags.label}
              />
          }
          {
            audioTags.publisher !== undefined &&
              <DescriptionListItem
                title={translate('Publisher')}
                data={audioTags.publisher}
              />
          }
          {
            audioTags.catalogNumber !== undefined &&
              <DescriptionListItem
                title={translate('CatalogNumber')}
                data={audioTags.catalogNumber}
              />
          }
          {
            audioTags.disambiguation !== undefined &&
              <DescriptionListItem
                title={translate('Overview')}
                data={stripHtml(audioTags.disambiguation)}
              />
          }
          {
            audioTags.isbn !== undefined &&
              <DescriptionListItem
                title={translate('ISBN')}
                data={audioTags.isbn}
              />
          }
          {
            audioTags.asin !== undefined &&
              <DescriptionListItem
                title={translate('ASIN')}
                data={audioTags.asin}
              />
          }       {
            audioTags.authorMBId !== undefined &&
              <Link
                to={`https://musicbrainz.org/author/${audioTags.authorMBId}`}
              >
                <DescriptionListItem
                  title={translate('MusicBrainzAuthorID')}
                  data={audioTags.authorMBId}
                />
              </Link>
          }
          {
            audioTags.bookMBId !== undefined &&
              <Link
                to={`https://musicbrainz.org/release-group/${audioTags.bookMBId}`}
              >
                <DescriptionListItem
                  title={translate('MusicBrainzBookID')}
                  data={audioTags.bookMBId}
                />
              </Link>
          }
          {
            audioTags.releaseMBId !== undefined &&
              <Link
                to={`https://musicbrainz.org/release/${audioTags.releaseMBId}`}
              >
                <DescriptionListItem
                  title={translate('MusicBrainzReleaseID')}
                  data={audioTags.releaseMBId}
                />
              </Link>
          }
          {
            audioTags.recordingMBId !== undefined &&
              <Link
                to={`https://musicbrainz.org/recording/${audioTags.recordingMBId}`}
              >
                <DescriptionListItem
                  title={translate('MusicBrainzRecordingID')}
                  data={audioTags.recordingMBId}
                />
              </Link>
          }
          {
            audioTags.trackMBId !== undefined &&
              <Link
                to={`https://musicbrainz.org/track/${audioTags.trackMBId}`}
              >
                <DescriptionListItem
                  title={translate('MusicBrainzTrackID')}
                  data={audioTags.trackMBId}
                />
              </Link>
          }
          {
            !!rejections && rejections.length > 0 &&
              renderRejections(rejections)
          }
        </DescriptionList>
      </div>
    </Fragment>
  );
}

FileDetails.propTypes = {
  filename: PropTypes.string,
  audioTags: PropTypes.object.isRequired,
  rejections: PropTypes.arrayOf(PropTypes.object)
};

export default FileDetails;
