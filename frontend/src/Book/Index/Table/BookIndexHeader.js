import classNames from 'classnames';
import PropTypes from 'prop-types';
import React from 'react';
import IconButton from 'Components/Link/IconButton';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import VirtualTableHeader from 'Components/Table/VirtualTableHeader';
import VirtualTableHeaderCell from 'Components/Table/VirtualTableHeaderCell';
import VirtualTableSelectAllHeaderCell from 'Components/Table/VirtualTableSelectAllHeaderCell';
import { icons } from 'Helpers/Props';
import BookIndexTableOptionsConnector from './BookIndexTableOptionsConnector';
import styles from './BookIndexHeader.css';

function BookIndexHeader(props) {
  const {
    columns,
    onTableOptionChange,
    allSelected,
    allUnselected,
    onSelectAllChange,
    isEditorActive,
    ...otherProps
  } = props;

  return (
    <VirtualTableHeader>
      {
        columns.map((column) => {
          const {
            name,
            label,
            isSortable,
            isVisible
          } = column;

          if (!isVisible) {
            return null;
          }

          if (name === 'select') {
            if (isEditorActive) {
              return (
                <VirtualTableSelectAllHeaderCell
                  key={name}
                  allSelected={allSelected}
                  allUnselected={allUnselected}
                  onSelectAllChange={onSelectAllChange}
                />
              );
            }

            return null;
          }

          if (name === 'actions') {
            return (
              <VirtualTableHeaderCell
                key={name}
                className={styles[name]}
                name={name}
                isSortable={false}
                {...otherProps}
              >

                <TableOptionsModalWrapper
                  columns={columns}
                  optionsComponent={BookIndexTableOptionsConnector}
                  onTableOptionChange={onTableOptionChange}
                >
                  <IconButton
                    name={icons.ADVANCED_SETTINGS}
                  />
                </TableOptionsModalWrapper>
              </VirtualTableHeaderCell>
            );
          }

          return (
            <VirtualTableHeaderCell
              key={name}
              className={classNames(
                styles[name]
              )}
              name={name}
              isSortable={isSortable}
              {...otherProps}
            >
              {label}
            </VirtualTableHeaderCell>
          );
        })
      }
    </VirtualTableHeader>
  );
}

BookIndexHeader.propTypes = {
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  allSelected: PropTypes.bool.isRequired,
  allUnselected: PropTypes.bool.isRequired,
  onSelectAllChange: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired
};

export default BookIndexHeader;
