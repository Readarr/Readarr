import PropTypes from 'prop-types';
import React from 'react';
import BookIndexFilterModalConnector from 'Book/Index/BookIndexFilterModalConnector';
import FilterMenu from 'Components/Menu/FilterMenu';
import { align } from 'Helpers/Props';

function BookIndexFilterMenu(props) {
  const {
    selectedFilterKey,
    filters,
    customFilters,
    isDisabled,
    onFilterSelect
  } = props;

  return (
    <FilterMenu
      alignMenu={align.RIGHT}
      isDisabled={isDisabled}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      customFilters={customFilters}
      filterModalConnectorComponent={BookIndexFilterModalConnector}
      onFilterSelect={onFilterSelect}
    />
  );
}

BookIndexFilterMenu.propTypes = {
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  isDisabled: PropTypes.bool.isRequired,
  onFilterSelect: PropTypes.func.isRequired
};

BookIndexFilterMenu.defaultProps = {
  showCustomFilters: false
};

export default BookIndexFilterMenu;
