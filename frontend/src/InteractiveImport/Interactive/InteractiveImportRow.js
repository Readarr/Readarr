import PropTypes from 'prop-types';
import React, { Component } from 'react';
import BookQuality from 'Book/BookQuality';
import FileDetails from 'BookFile/FileDetails';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRowCellButton from 'Components/Table/Cells/TableRowCellButton';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import Popover from 'Components/Tooltip/Popover';
import Tooltip from 'Components/Tooltip/Tooltip';
import { icons, kinds, sizes, tooltipPositions } from 'Helpers/Props';
import SelectAuthorModal from 'InteractiveImport/Author/SelectAuthorModal';
import SelectBookModal from 'InteractiveImport/Book/SelectBookModal';
import SelectQualityModal from 'InteractiveImport/Quality/SelectQualityModal';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import InteractiveImportRowCellPlaceholder from './InteractiveImportRowCellPlaceholder';
import styles from './InteractiveImportRow.css';

class InteractiveImportRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isSelectAuthorModalOpen: false,
      isSelectBookModalOpen: false,
      isSelectQualityModalOpen: false
    };
  }

  componentDidMount() {
    const {
      id,
      author,
      book,
      foreignEditionId,
      quality
    } = this.props;

    if (
      author &&
      book != null &&
      foreignEditionId &&
      quality
    ) {
      this.props.onSelectedChange({ id, value: true });
    }
  }

  componentDidUpdate(prevProps) {
    const {
      id,
      author,
      book,
      foreignEditionId,
      quality,
      isSelected,
      onValidRowChange
    } = this.props;

    if (
      prevProps.author === author &&
      prevProps.book === book &&
      prevProps.foreignEditionId === foreignEditionId &&
      prevProps.quality === quality &&
      prevProps.isSelected === isSelected
    ) {
      return;
    }

    const isValid = !!(
      author &&
      book &&
      foreignEditionId &&
      quality
    );

    if (isSelected && !isValid) {
      onValidRowChange(id, false);
    } else {
      onValidRowChange(id, true);
    }
  }

  //
  // Control

  selectRowAfterChange = (value) => {
    const {
      id,
      isSelected
    } = this.props;

    if (!isSelected && value === true) {
      this.props.onSelectedChange({ id, value });
    }
  };

  //
  // Listeners

  onDetailsPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  onSelectAuthorPress = () => {
    this.setState({ isSelectAuthorModalOpen: true });
  };

  onSelectBookPress = () => {
    this.setState({ isSelectBookModalOpen: true });
  };

  onSelectQualityPress = () => {
    this.setState({ isSelectQualityModalOpen: true });
  };

  onSelectAuthorModalClose = (changed) => {
    this.setState({ isSelectAuthorModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectBookModalClose = (changed) => {
    this.setState({ isSelectBookModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectQualityModalClose = (changed) => {
    this.setState({ isSelectQualityModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  //
  // Render

  render() {
    const {
      id,
      allowAuthorChange,
      path,
      author,
      book,
      quality,
      size,
      rejections,
      additionalFile,
      isSelected,
      onSelectedChange,
      audioTags,
      isSaving
    } = this.props;

    const {
      isDetailsModalOpen,
      isSelectAuthorModalOpen,
      isSelectBookModalOpen,
      isSelectQualityModalOpen
    } = this.state;

    const authorName = author ? author.authorName : '';
    let bookTitle = '';
    if (book) {
      bookTitle = book.disambiguation ? `${book.title} (${book.disambiguation})` : book.title;
    }

    const showAuthorPlaceholder = isSelected && !author;
    const showBookNumberPlaceholder = isSelected && !!author && !book;
    const showQualityPlaceholder = isSelected && !quality;

    const pathCellContents = (
      <div onClick={this.onDetailsPress}>
        {path}
      </div>
    );

    const pathCell = additionalFile ? (
      <Tooltip
        anchor={pathCellContents}
        tooltip='This file is already in your library for a release you are currently importing'
        position={tooltipPositions.TOP}
      />
    ) : pathCellContents;

    const fileDetails = (
      <FileDetails
        audioTags={audioTags}
        filename={path}
      />
    );

    return (
      <TableRow
        className={additionalFile ? styles.additionalFile : undefined}
      >
        <TableSelectCell
          id={id}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
        />

        <TableRowCell
          className={styles.path}
          title={path}
        >
          {pathCell}
        </TableRowCell>

        <TableRowCellButton
          isDisabled={!allowAuthorChange}
          title={allowAuthorChange ? translate('AllowAuthorChangeClickToChangeAuthor') : undefined}
          onPress={this.onSelectAuthorPress}
        >
          {
            showAuthorPlaceholder ? <InteractiveImportRowCellPlaceholder /> : authorName
          }
        </TableRowCellButton>

        <TableRowCellButton
          isDisabled={!author}
          title={author ? translate('AuthorClickToChangeBook') : undefined}
          onPress={this.onSelectBookPress}
        >
          {
            showBookNumberPlaceholder ? <InteractiveImportRowCellPlaceholder /> : bookTitle
          }
        </TableRowCellButton>

        <TableRowCellButton
          className={styles.quality}
          title={translate('ClickToChangeQuality')}
          onPress={this.onSelectQualityPress}
        >
          {
            showQualityPlaceholder &&
              <InteractiveImportRowCellPlaceholder />
          }

          {
            !showQualityPlaceholder && !!quality &&
              <BookQuality
                className={styles.label}
                quality={quality}
              />
          }
        </TableRowCellButton>

        <TableRowCell>
          {formatBytes(size)}
        </TableRowCell>

        <TableRowCell>
          {
            isSaving &&
              <LoadingIndicator
                className={styles.loading}
                size={20}
              />
          }
          {
            !isSaving && rejections && rejections.length ?
              <Popover
                anchor={
                  <Icon
                    name={icons.DANGER}
                    kind={kinds.DANGER}
                  />
                }
                title={translate('ReleaseRejected')}
                body={
                  <ul>
                    {
                      rejections.map((rejection, index) => {
                        return (
                          <li key={index}>
                            {rejection.reason}
                          </li>
                        );
                      })
                    }
                  </ul>
                }
                position={tooltipPositions.LEFT}
              /> :
              null
          }
        </TableRowCell>

        <ConfirmModal
          isOpen={isDetailsModalOpen}
          title={translate('FileDetails')}
          message={fileDetails}
          size={sizes.LARGE}
          kind={kinds.DEFAULT}
          hideCancelButton={true}
          confirmLabel={translate('Close')}
          onConfirm={this.onDetailsModalClose}
          onCancel={this.onDetailsModalClose}
        />

        <SelectAuthorModal
          isOpen={isSelectAuthorModalOpen}
          ids={[id]}
          onModalClose={this.onSelectAuthorModalClose}
        />

        <SelectBookModal
          isOpen={isSelectBookModalOpen}
          ids={[id]}
          authorId={author && author.id}
          onModalClose={this.onSelectBookModalClose}
        />

        <SelectQualityModal
          isOpen={isSelectQualityModalOpen}
          ids={[id]}
          qualityId={quality ? quality.quality.id : 0}
          proper={quality ? quality.revision.version > 1 : false}
          real={quality ? quality.revision.real > 0 : false}
          onModalClose={this.onSelectQualityModalClose}
        />
      </TableRow>
    );
  }

}

InteractiveImportRow.propTypes = {
  id: PropTypes.number.isRequired,
  allowAuthorChange: PropTypes.bool.isRequired,
  path: PropTypes.string.isRequired,
  author: PropTypes.object,
  book: PropTypes.object,
  foreignEditionId: PropTypes.string,
  quality: PropTypes.object,
  size: PropTypes.number.isRequired,
  rejections: PropTypes.arrayOf(PropTypes.object).isRequired,
  audioTags: PropTypes.object.isRequired,
  additionalFile: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  isSaving: PropTypes.bool.isRequired,
  onSelectedChange: PropTypes.func.isRequired,
  onValidRowChange: PropTypes.func.isRequired
};

export default InteractiveImportRow;
