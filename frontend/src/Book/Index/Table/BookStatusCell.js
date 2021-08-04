import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './BookStatusCell.css';

function BookStatusCell(props) {
  const {
    className,
    monitored,
    component: Component,
    ...otherProps
  } = props;

  return (
    <Component
      className={className}
      {...otherProps}
    >
      <Icon
        className={styles.statusIcon}
        name={monitored ? icons.MONITORED : icons.UNMONITORED}
        title={monitored ? translate('MonitoredAuthorIsMonitored') : translate('MonitoredAuthorIsUnmonitored')}
      />
    </Component>
  );
}

BookStatusCell.propTypes = {
  className: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  component: PropTypes.elementType
};

BookStatusCell.defaultProps = {
  className: styles.status,
  component: VirtualTableRowCell
};

export default BookStatusCell;
