import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import AuthorPoster from 'Author/AuthorPoster';
import HeartRating from 'Components/HeartRating';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import Marquee from 'Components/Marquee';
import Measure from 'Components/Measure';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import Popover from 'Components/Tooltip/Popover';
import Tooltip from 'Components/Tooltip/Tooltip';
import { icons, kinds, sizes, tooltipPositions } from 'Helpers/Props';
import QualityProfileNameConnector from 'Settings/Profiles/Quality/QualityProfileNameConnector';
import fonts from 'Styles/Variables/fonts';
import formatBytes from 'Utilities/Number/formatBytes';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import AuthorAlternateTitles from './AuthorAlternateTitles';
import AuthorDetailsLinks from './AuthorDetailsLinks';
import AuthorTagsConnector from './AuthorTagsConnector';
import styles from './AuthorDetailsHeader.css';

const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

function getFanartUrl(images) {
  const fanartImage = images.find((x) => x.coverType === 'fanart');

  if (fanartImage) {
    // Remove protocol
    return fanartImage.url.replace(/^https?:/, '');
  }
}

class AuthorDetailsHeader extends Component {

  //
  // Lifecyle

  constructor(props) {
    super(props);

    this.state = {
      overviewHeight: 0,
      titleWidth: 0
    };
  }

  //
  // Listeners

  onOverviewMeasure = ({ height }) => {
    this.setState({ overviewHeight: height });
  };

  onTitleMeasure = ({ width }) => {
    this.setState({ titleWidth: width });
  };

  //
  // Render

  render() {
    const {
      id,
      width,
      authorName,
      ratings,
      path,
      statistics,
      qualityProfileId,
      monitored,
      status,
      overview,
      links,
      images,
      alternateTitles,
      tags,
      isSaving,
      isSmallScreen,
      onMonitorTogglePress
    } = this.props;

    const {
      bookFileCount,
      sizeOnDisk
    } = statistics;

    const {
      overviewHeight,
      titleWidth
    } = this.state;

    const marqueeWidth = titleWidth - (isSmallScreen ? 85 : 160);

    const continuing = status === 'continuing';

    let bookFilesCountMessage = translate('BookFilesCountMessage');

    if (bookFileCount === 1) {
      bookFilesCountMessage = '1 book file';
    } else if (bookFileCount > 1) {
      bookFilesCountMessage = `${bookFileCount} book files`;
    }

    return (
      <div className={styles.header} style={{ width }} >
        <div
          className={styles.backdrop}
          style={{
            backgroundImage: `url(${getFanartUrl(images)})`
          }}
        >
          <div className={styles.backdropOverlay} />
        </div>

        <div className={styles.headerContent}>
          <AuthorPoster
            className={styles.poster}
            images={images}
            size={250}
            lazy={false}
          />

          <div className={styles.info}>
            <Measure
              className={styles.titleRow}
              onMeasure={this.onTitleMeasure}
            >
              <div className={styles.titleContainer}>
                <div className={styles.toggleMonitoredContainer}>
                  <MonitorToggleButton
                    className={styles.monitorToggleButton}
                    monitored={monitored}
                    isSaving={isSaving}
                    size={isSmallScreen ? 30: 40}
                    onPress={onMonitorTogglePress}
                  />
                </div>

                <div className={styles.title} style={{ width: marqueeWidth }}>
                  <Marquee text={authorName} />
                </div>

                {
                  !!alternateTitles.length &&
                    <div className={styles.alternateTitlesIconContainer}>
                      <Popover
                        anchor={
                          <Icon
                            name={icons.ALTERNATE_TITLES}
                            size={20}
                          />
                        }
                        title={translate('AlternateTitles')}
                        body={<AuthorAlternateTitles alternateTitles={alternateTitles} />}
                        position={tooltipPositions.BOTTOM}
                      />
                    </div>
                }
              </div>
            </Measure>

            <div className={styles.details}>
              <div>
                <HeartRating
                  rating={ratings.value}
                  iconSize={20}
                />
              </div>
            </div>

            <div className={styles.detailsLabels}>
              <Label
                className={styles.detailsLabel}
                size={sizes.LARGE}
              >
                <Icon
                  name={icons.FOLDER}
                  size={17}
                />

                <span className={styles.path}>
                  {path}
                </span>
              </Label>

              <Label
                className={styles.detailsLabel}
                title={bookFilesCountMessage}
                size={sizes.LARGE}
              >
                <Icon
                  name={icons.DRIVE}
                  size={17}
                />

                <span className={styles.sizeOnDisk}>
                  {
                    formatBytes(sizeOnDisk || 0)
                  }
                </span>
              </Label>

              <Label
                className={styles.detailsLabel}
                title={translate('QualityProfile')}
                size={sizes.LARGE}
              >
                <Icon
                  name={icons.PROFILE}
                  size={17}
                />

                <span className={styles.qualityProfileName}>
                  {
                    <QualityProfileNameConnector
                      qualityProfileId={qualityProfileId}
                    />
                  }
                </span>
              </Label>

              <Label
                className={styles.detailsLabel}
                size={sizes.LARGE}
              >
                <Icon
                  name={monitored ? icons.MONITORED : icons.UNMONITORED}
                  size={17}
                />

                <span className={styles.qualityProfileName}>
                  {monitored ? 'Monitored' : 'Unmonitored'}
                </span>
              </Label>

              <Label
                className={styles.detailsLabel}
                title={continuing ? translate('ContinuingMoreBooksAreExpected') : translate('ContinuingNoAdditionalBooksAreExpected')}
                size={sizes.LARGE}
              >
                <Icon
                  name={continuing ? icons.AUTHOR_CONTINUING : icons.AUTHOR_ENDED}
                  size={17}
                />

                <span className={styles.qualityProfileName}>
                  {continuing ? 'Continuing' : 'Deceased'}
                </span>
              </Label>

              <Tooltip
                anchor={
                  <Label
                    className={styles.detailsLabel}
                    size={sizes.LARGE}
                  >
                    <Icon
                      name={icons.EXTERNAL_LINK}
                      size={17}
                    />

                    <span className={styles.links}>
                      Links
                    </span>
                  </Label>
                }
                tooltip={
                  <AuthorDetailsLinks
                    links={links}
                  />
                }
                kind={kinds.INVERSE}
                position={tooltipPositions.BOTTOM}
              />

              {
                !!tags.length &&
                  <Tooltip
                    anchor={
                      <Label
                        className={styles.detailsLabel}
                        size={sizes.LARGE}
                      >
                        <Icon
                          name={icons.TAGS}
                          size={17}
                        />

                        <span className={styles.tags}>
                          Tags
                        </span>
                      </Label>
                    }
                    tooltip={<AuthorTagsConnector authorId={id} />}
                    kind={kinds.INVERSE}
                    position={tooltipPositions.BOTTOM}
                  />

              }
            </div>
            <Measure
              onMeasure={this.onOverviewMeasure}
              className={styles.overview}
            >
              <TextTruncate
                line={Math.floor(overviewHeight / (defaultFontSize * lineHeight))}
                text={stripHtml(overview)}
              />
            </Measure>
          </div>
        </div>
      </div>
    );
  }
}

AuthorDetailsHeader.propTypes = {
  id: PropTypes.number.isRequired,
  width: PropTypes.number.isRequired,
  authorName: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  path: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  overview: PropTypes.string,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  alternateTitles: PropTypes.arrayOf(PropTypes.string).isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onMonitorTogglePress: PropTypes.func.isRequired
};

export default AuthorDetailsHeader;
