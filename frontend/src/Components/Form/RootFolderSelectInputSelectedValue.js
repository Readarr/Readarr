import PropTypes from 'prop-types';
import React from 'react';
import formatBytes from 'Utilities/Number/formatBytes';
import EnhancedSelectInputSelectedValue from './EnhancedSelectInputSelectedValue';
import styles from './RootFolderSelectInputSelectedValue.css';

function RootFolderSelectInputSelectedValue(props) {
  const {
    name,
    value,
    freeSpace,
    authorFolder,
    includeFreeSpace,
    isWindows,
    ...otherProps
  } = props;

  const slashCharacter = isWindows ? '\\' : '/';

  const text = name === '' ? value : `[${name}] ${value}`;

  return (
    <EnhancedSelectInputSelectedValue
      className={styles.selectedValue}
      {...otherProps}
    >
      <div className={styles.pathContainer}>
        <div className={styles.path}>
          {text}
        </div>

        {
          authorFolder ?
            <div className={styles.authorFolder}>
              {slashCharacter}
              {authorFolder}
            </div> :
            null
        }
      </div>

      {
        freeSpace != null && includeFreeSpace &&
          <div className={styles.freeSpace}>
            {formatBytes(freeSpace)} Free
          </div>
      }
    </EnhancedSelectInputSelectedValue>
  );
}

RootFolderSelectInputSelectedValue.propTypes = {
  name: PropTypes.string,
  value: PropTypes.string,
  freeSpace: PropTypes.number,
  authorFolder: PropTypes.string,
  isWindows: PropTypes.bool,
  includeFreeSpace: PropTypes.bool.isRequired
};

RootFolderSelectInputSelectedValue.defaultProps = {
  name: '',
  includeFreeSpace: true
};

export default RootFolderSelectInputSelectedValue;
