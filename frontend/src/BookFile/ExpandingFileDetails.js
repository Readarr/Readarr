import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import FileDetails from './FileDetails';
import styles from './ExpandingFileDetails.css';

class ExpandingFileDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isExpanded: props.isExpanded
    };
  }

  //
  // Listeners

  onExpandPress = () => {
    const {
      isExpanded
    } = this.state;
    this.setState({ isExpanded: !isExpanded });
  };

  //
  // Render

  render() {
    const {
      filename,
      audioTags,
      rejections
    } = this.props;

    const {
      isExpanded
    } = this.state;

    return (
      <div
        className={styles.fileDetails}
      >
        <div className={styles.header} onClick={this.onExpandPress}>
          <div className={styles.filename}>
            {filename}
          </div>

          <div className={styles.expandButton}>
            <Icon
              className={styles.expandButtonIcon}
              name={isExpanded ? icons.COLLAPSE : icons.EXPAND}
              title={isExpanded ? translate('IsExpandedHideFileInfo') : translate('IsExpandedShowFileInfo')}
              size={24}
            />
          </div>
        </div>

        {
          isExpanded &&
            <FileDetails
              audioTags={audioTags}
              rejections={rejections}
            />
        }
      </div>
    );
  }
}

ExpandingFileDetails.propTypes = {
  audioTags: PropTypes.object.isRequired,
  filename: PropTypes.string.isRequired,
  rejections: PropTypes.arrayOf(PropTypes.object),
  isExpanded: PropTypes.bool
};

export default ExpandingFileDetails;
