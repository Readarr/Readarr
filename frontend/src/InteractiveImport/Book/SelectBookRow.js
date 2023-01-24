import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Label from 'Components/Label';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './SelectBookRow.css';

function getBookCountKind(monitored, bookFileCount, bookCount) {
  if (bookFileCount === bookCount && bookCount > 0) {
    return kinds.SUCCESS;
  }

  if (!monitored) {
    return kinds.WARNING;
  }

  return kinds.DANGER;
}

class SelectBookRow extends Component {

  //
  // Listeners

  onPress = () => {
    this.props.onBookSelect(this.props.id);
  };

  //
  // Render

  render() {
    const {
      title,
      releaseDate,
      statistics,
      monitored,
      columns
    } = this.props;

    const {
      bookCount,
      bookFileCount,
      totalBookCount
    } = statistics;

    return (
      <TableRow
        onClick={this.onPress}
        className={styles.bookRow}
      >
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'title') {
              return (
                <TableRowCell key={name}>
                  {title}
                </TableRowCell>
              );
            }

            if (name === 'releaseDate') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  date={releaseDate}
                />
              );
            }

            if (name === 'status') {
              return (
                <TableRowCell
                  key={name}
                >
                  <Label
                    title={translate('TotalBookCountBooksTotalBookFileCountBooksWithFilesInterp', [totalBookCount, bookFileCount])}
                    kind={getBookCountKind(monitored, bookFileCount, bookCount)}
                    size={sizes.MEDIUM}
                  >
                    {
                      <span>{bookFileCount} / {bookCount}</span>
                    }
                  </Label>
                </TableRowCell>
              );
            }

            return null;
          })
        }
      </TableRow>

    );
  }
}

SelectBookRow.propTypes = {
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  releaseDate: PropTypes.string.isRequired,
  onBookSelect: PropTypes.func.isRequired,
  statistics: PropTypes.object.isRequired,
  monitored: PropTypes.bool.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired
};

SelectBookRow.defaultProps = {
  statistics: {
    bookCount: 0,
    bookFileCount: 0
  }
};

export default SelectBookRow;
