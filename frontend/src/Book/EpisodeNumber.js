import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import Popover from 'Components/Tooltip/Popover';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import SceneInfo from './SceneInfo';
import styles from './EpisodeNumber.css';

function EpisodeNumber(props) {
  const {
    episodeNumber,
    absoluteEpisodeNumber,
    sceneSeasonNumber,
    sceneEpisodeNumber,
    sceneAbsoluteEpisodeNumber,
    unverifiedSceneNumbering,
    alternateTitles,
    authorType
  } = props;

  const hasSceneInformation = sceneSeasonNumber !== undefined ||
    sceneEpisodeNumber !== undefined ||
    (authorType === 'anime' && sceneAbsoluteEpisodeNumber !== undefined) ||
    !!alternateTitles.length;

  return (
    <span>
      {
        hasSceneInformation ?
          <Popover
            anchor={
              <span>
                {episodeNumber}

                {
                  authorType === 'anime' && !!absoluteEpisodeNumber &&
                    <span className={styles.absoluteEpisodeNumber}>
                      ({absoluteEpisodeNumber})
                    </span>
                }
              </span>
            }
            title={translate('SceneInformation')}
            body={
              <SceneInfo
                sceneSeasonNumber={sceneSeasonNumber}
                sceneEpisodeNumber={sceneEpisodeNumber}
                sceneAbsoluteEpisodeNumber={sceneAbsoluteEpisodeNumber}
                alternateTitles={alternateTitles}
                authorType={authorType}
              />
            }
            position={tooltipPositions.RIGHT}
          /> :
          <span>
            {episodeNumber}

            {
              authorType === 'anime' && !!absoluteEpisodeNumber &&
                <span className={styles.absoluteEpisodeNumber}>
                  ({absoluteEpisodeNumber})
                </span>
            }
          </span>
      }

      {
        unverifiedSceneNumbering &&
          <Icon
            className={styles.warning}
            name={icons.WARNING}
            kind={kinds.WARNING}
            title={translate('SceneNumberHasntBeenVerifiedYet')}
          />
      }

      {
        authorType === 'anime' && !absoluteEpisodeNumber &&
          <Icon
            className={styles.warning}
            name={icons.WARNING}
            kind={kinds.WARNING}
            title={translate('EpisodeDoesNotHaveAnAbsoluteEpisodeNumber')}
          />
      }
    </span>
  );
}

EpisodeNumber.propTypes = {
  seasonNumber: PropTypes.number.isRequired,
  episodeNumber: PropTypes.number.isRequired,
  absoluteEpisodeNumber: PropTypes.number,
  sceneSeasonNumber: PropTypes.number,
  sceneEpisodeNumber: PropTypes.number,
  sceneAbsoluteEpisodeNumber: PropTypes.number,
  unverifiedSceneNumbering: PropTypes.bool.isRequired,
  alternateTitles: PropTypes.arrayOf(PropTypes.object).isRequired,
  authorType: PropTypes.string
};

EpisodeNumber.defaultProps = {
  unverifiedSceneNumbering: false,
  alternateTitles: []
};

export default EpisodeNumber;
