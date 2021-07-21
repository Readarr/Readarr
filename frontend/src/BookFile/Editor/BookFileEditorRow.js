import PropTypes from 'prop-types';
import React from 'react';
import BookQuality from 'Book/BookQuality';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import formatBytes from 'Utilities/Number/formatBytes';
import BookFileActionsCell from './BookFileActionsCell';
import styles from './BookFileEditorRow.css';

function BookFileEditorRow(props) {
  const {
    id,
    path,
    size,
    dateAdded,
    quality,
    qualityCutoffNotMet,
    isSelected,
    onSelectedChange,
    deleteBookFile
  } = props;

  return (
    <TableRow>
      <TableSelectCell
        id={id}
        isSelected={isSelected}
        onSelectedChange={onSelectedChange}
      />
      <TableRowCell
        className={styles.path}
      >
        {path}
      </TableRowCell>

      <TableRowCell
        className={styles.size}
      >
        {formatBytes(size)}
      </TableRowCell>

      <RelativeDateCellConnector
        className={styles.dateAdded}
        date={dateAdded}
      />

      <TableRowCell
        className={styles.quality}
      >
        <BookQuality
          quality={quality}
          isCutoffNotMet={qualityCutoffNotMet}
        />
      </TableRowCell>

      <BookFileActionsCell
        id={id}
        path={path}
        deleteBookFile={deleteBookFile}
      />
    </TableRow>
  );
}

BookFileEditorRow.propTypes = {
  id: PropTypes.number.isRequired,
  path: PropTypes.string.isRequired,
  size: PropTypes.number.isRequired,
  quality: PropTypes.object.isRequired,
  qualityCutoffNotMet: PropTypes.bool.isRequired,
  dateAdded: PropTypes.string.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  deleteBookFile: PropTypes.func.isRequired
};

export default BookFileEditorRow;
