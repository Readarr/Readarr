import PropTypes from 'prop-types';
import React from 'react';
import { getAuthorStatusDetails } from 'Author/AuthorStatus';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './AuthorStatusCell.css';

function AuthorStatusCell(props) {
  const {
    className,
    monitored,
    status,
    component: Component,
    ...otherProps
  } = props;

  const statusDetails = getAuthorStatusDetails(status);

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

      <Icon
        className={styles.statusIcon}
        name={statusDetails.icon}
        title={`${statusDetails.title}: ${statusDetails.message}`}
      />
    </Component>
  );
}

AuthorStatusCell.propTypes = {
  className: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  component: PropTypes.elementType
};

AuthorStatusCell.defaultProps = {
  className: styles.status,
  component: VirtualTableRowCell
};

export default AuthorStatusCell;
