import PropTypes from 'prop-types';
import React from 'react';
import monitorOptions from 'Utilities/Author/monitorOptions';
import translate from 'Utilities/String/translate';
import SelectInput from './SelectInput';

function MonitorBooksSelectInput(props) {
  const {
    includeNoChange,
    includeMixed,
    includeSpecificBook,
    ...otherProps
  } = props;

  const values = [...monitorOptions];

  if (includeNoChange) {
    values.unshift({
      key: 'noChange',
      value: translate('NoChange'),
      isDisabled: true
    });
  }

  if (includeMixed) {
    values.unshift({
      key: 'mixed',
      value: '(Mixed)',
      isDisabled: true
    });
  }

  if (includeSpecificBook) {
    values.push({
      key: 'specificBook',
      value: 'Only This Book'
    });
  }

  return (
    <SelectInput
      values={values}
      {...otherProps}
    />
  );
}

MonitorBooksSelectInput.propTypes = {
  includeNoChange: PropTypes.bool.isRequired,
  includeMixed: PropTypes.bool.isRequired,
  includeSpecificBook: PropTypes.bool.isRequired,
  onChange: PropTypes.func.isRequired
};

MonitorBooksSelectInput.defaultProps = {
  includeNoChange: false,
  includeMixed: false,
  includeSpecificBook: false
};

export default MonitorBooksSelectInput;
