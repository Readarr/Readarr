import PropTypes from 'prop-types';
import React, { Component } from 'react';
import BookSearchCellConnector from 'Book/BookSearchCellConnector';
import BookTitleLink from 'Book/BookTitleLink';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import StarRating from 'Components/StarRating';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import BookStatus from './BookStatus';
import styles from './BookRow.css';

class BookRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isEditBookModalOpen: false
    };
  }

  //
  // Listeners

  onManualSearchPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  onEditBookPress = () => {
    this.setState({ isEditBookModalOpen: true });
  };

  onEditBookModalClose = () => {
    this.setState({ isEditBookModalOpen: false });
  };

  onMonitorBookPress = (monitored, options) => {
    this.props.onMonitorBookPress(this.props.id, monitored, options);
  };

  //
  // Render

  render() {
    const {
      id,
      authorId,
      monitored,
      releaseDate,
      title,
      seriesTitle,
      authorName,
      position,
      pageCount,
      ratings,
      isSaving,
      authorMonitored,
      titleSlug,
      bookFiles,
      isEditorActive,
      isSelected,
      onSelectedChange,
      columns
    } = this.props;

    const bookFile = bookFiles[0];
    const isAvailable = Date.parse(releaseDate) < new Date();

    return (
      <TableRow>
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (isEditorActive && name === 'select') {
              return (
                <TableSelectCell
                  key={name}
                  id={id}
                  isSelected={isSelected}
                  isDisabled={false}
                  onSelectedChange={onSelectedChange}
                />
              );
            }

            if (name === 'monitored') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.monitored}
                >
                  <MonitorToggleButton
                    monitored={monitored}
                    isDisabled={!authorMonitored}
                    isSaving={isSaving}
                    onPress={this.onMonitorBookPress}
                  />
                </TableRowCell>
              );
            }

            if (name === 'title') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.title}
                >
                  <BookTitleLink
                    titleSlug={titleSlug}
                    title={title}
                  />
                </TableRowCell>
              );
            }

            if (name === 'series') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.title}
                >
                  {seriesTitle || ''}
                </TableRowCell>
              );
            }

            if (name === 'position') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.position}
                >
                  {position || ''}
                </TableRowCell>
              );
            }

            if (name === 'rating') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.rating}
                >
                  {
                    <StarRating
                      rating={ratings.value}
                      votes={ratings.votes}
                    />
                  }
                </TableRowCell>
              );
            }

            if (name === 'releaseDate') {
              return (
                <RelativeDateCellConnector
                  className={styles.releaseDate}
                  key={name}
                  date={releaseDate}
                />
              );
            }

            if (name === 'pageCount') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.pageCount}
                >
                  {pageCount || ''}
                </TableRowCell>
              );
            }

            if (name === 'status') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.status}
                >
                  <BookStatus
                    isAvailable={isAvailable}
                    monitored={monitored}
                    bookFile={bookFile}
                  />
                </TableRowCell>
              );
            }

            if (name === 'actions') {
              return (
                <BookSearchCellConnector
                  key={name}
                  bookId={id}
                  authorId={authorId}
                  bookTitle={title}
                  authorName={authorName}
                />
              );
            }
            return null;
          })
        }
      </TableRow>
    );
  }
}

BookRow.propTypes = {
  id: PropTypes.number.isRequired,
  authorId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  releaseDate: PropTypes.string,
  title: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string.isRequired,
  authorName: PropTypes.string.isRequired,
  position: PropTypes.string,
  pageCount: PropTypes.number,
  ratings: PropTypes.object.isRequired,
  titleSlug: PropTypes.string.isRequired,
  isSaving: PropTypes.bool,
  authorMonitored: PropTypes.bool.isRequired,
  bookFiles: PropTypes.arrayOf(PropTypes.object).isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onMonitorBookPress: PropTypes.func.isRequired
};

export default BookRow;
