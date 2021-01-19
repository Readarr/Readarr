import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AuthorNameLink from 'Author/AuthorNameLink';
import AuthorStatusCell from 'Author/Index/Table/AuthorStatusCell';
import CheckInput from 'Components/Form/CheckInput';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import TagListConnector from 'Components/TagListConnector';
import formatBytes from 'Utilities/Number/formatBytes';
import styles from './AuthorEditorRow.css';

class AuthorEditorRow extends Component {

  //
  // Listeners

  onBookFolderChange = () => {
    // Mock handler to satisfy `onChange` being required for `CheckInput`.
    //
  }

  //
  // Render

  render() {
    const {
      id,
      status,
      foreignAuthorId,
      authorName,
      authorType,
      bookFolder,
      monitored,
      metadataProfile,
      qualityProfile,
      path,
      statistics,
      tags,
      columns,
      isSaving,
      isSelected,
      onAuthorMonitoredPress,
      onSelectedChange
    } = this.props;

    return (
      <TableRow>
        <TableSelectCell
          id={id}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
        />

        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'status') {
              return (
                <AuthorStatusCell
                  key={name}
                  authorType={authorType}
                  monitored={monitored}
                  status={status}
                  isSaving={isSaving}
                  onMonitoredPress={onAuthorMonitoredPress}
                />
              );
            }

            if (name === 'sortName') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.title}
                >
                  <AuthorNameLink
                    foreignAuthorId={foreignAuthorId}
                    authorName={authorName}
                  />
                </TableRowCell>
              );
            }

            if (name === 'qualityProfileId') {
              return (
                <TableRowCell key={name}>
                  {qualityProfile.name}
                </TableRowCell>
              );
            }

            if (name === 'metadataProfileId') {
              return (
                <TableRowCell key={name}>
                  {metadataProfile.name}
                </TableRowCell>
              );
            }

            if (name === 'bookFolder') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.bookFolder}
                >
                  <CheckInput
                    name="bookFolder"
                    value={bookFolder}
                    isDisabled={true}
                    onChange={this.onBookFolderChange}
                  />
                </TableRowCell>
              );
            }

            if (name === 'path') {
              return (
                <TableRowCell key={name}>
                  {path}
                </TableRowCell>
              );
            }

            if (name === 'sizeOnDisk') {
              return (
                <TableRowCell key={name}>
                  {formatBytes(statistics.sizeOnDisk)}
                </TableRowCell>
              );
            }

            if (name === 'tags') {
              return (
                <TableRowCell key={name}>
                  <TagListConnector
                    tags={tags}
                  />
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

AuthorEditorRow.propTypes = {
  id: PropTypes.number.isRequired,
  status: PropTypes.string.isRequired,
  foreignAuthorId: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  authorName: PropTypes.string.isRequired,
  authorType: PropTypes.string,
  bookFolder: PropTypes.string,
  monitored: PropTypes.bool.isRequired,
  metadataProfile: PropTypes.object.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  path: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onAuthorMonitoredPress: PropTypes.func.isRequired,
  onSelectedChange: PropTypes.func.isRequired
};

AuthorEditorRow.defaultProps = {
  tags: []
};

export default AuthorEditorRow;
