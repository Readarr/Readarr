import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AuthorPoster from 'Author/AuthorPoster';
import DeleteAuthorModal from 'Author/Delete/DeleteAuthorModal';
import EditAuthorModalConnector from 'Author/Edit/EditAuthorModalConnector';
import EditBookModalConnector from 'Book/Edit/EditBookModalConnector';
import BookIndexProgressBar from 'Book/Index/ProgressBar/BookIndexProgressBar';
import CheckInput from 'Components/Form/CheckInput';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import translate from 'Utilities/String/translate';
import BookIndexPosterInfo from './BookIndexPosterInfo';
import styles from './BookIndexPoster.css';

class BookIndexPoster extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      hasPosterError: false,
      isEditAuthorModalOpen: false,
      isDeleteAuthorModalOpen: false,
      isEditBookModalOpen: false
    };
  }

  //
  // Listeners

  onEditAuthorPress = () => {
    this.setState({ isEditAuthorModalOpen: true });
  };

  onEditAuthorModalClose = () => {
    this.setState({ isEditAuthorModalOpen: false });
  };

  onDeleteAuthorPress = () => {
    this.setState({
      isEditAuthorModalOpen: false,
      isDeleteAuthorModalOpen: true
    });
  };

  onDeleteAuthorModalClose = () => {
    this.setState({ isDeleteAuthorModalOpen: false });
  };

  onEditBookPress = () => {
    this.setState({ isEditBookModalOpen: true });
  };

  onEditBookModalClose = () => {
    this.setState({ isEditBookModalOpen: false });
  };

  onPosterLoad = () => {
    if (this.state.hasPosterError) {
      this.setState({ hasPosterError: false });
    }
  };

  onPosterLoadError = () => {
    if (!this.state.hasPosterError) {
      this.setState({ hasPosterError: true });
    }
  };

  onChange = ({ value, shiftKey }) => {
    const {
      id,
      onSelectedChange
    } = this.props;

    onSelectedChange({ id, value, shiftKey });
  };

  //
  // Render

  render() {
    const {
      id,
      title,
      authorId,
      author,
      monitored,
      titleSlug,
      nextAiring,
      statistics,
      images,
      posterWidth,
      posterHeight,
      detailedProgressBar,
      showTitle,
      showAuthor,
      showMonitored,
      showQualityProfile,
      qualityProfile,
      showSearchAction,
      showRelativeDates,
      shortDateFormat,
      timeFormat,
      isRefreshingBook,
      isSearchingBook,
      onRefreshBookPress,
      onSearchPress,
      isEditorActive,
      isSelected,
      onSelectedChange,
      ...otherProps
    } = this.props;

    const {
      bookCount,
      sizeOnDisk,
      bookFileCount,
      totalBookCount
    } = statistics;

    const {
      hasPosterError,
      isEditAuthorModalOpen,
      isDeleteAuthorModalOpen,
      isEditBookModalOpen
    } = this.state;

    const link = `/book/${titleSlug}`;

    const elementStyle = {
      width: `${posterWidth}px`,
      height: `${posterHeight}px`,
      objectFit: 'contain'
    };

    return (
      <div>
        <div className={styles.content}>
          <div className={styles.posterContainer}>
            {
              isEditorActive &&
                <div className={styles.editorSelect}>
                  <CheckInput
                    className={styles.checkInput}
                    name={id.toString()}
                    value={isSelected}
                    onChange={this.onChange}
                  />
                </div>
            }

            <Label className={styles.controls}>
              <SpinnerIconButton
                className={styles.action}
                name={icons.REFRESH}
                title={translate('RefreshBook')}
                isSpinning={isRefreshingBook}
                onPress={onRefreshBookPress}
              />

              {
                showSearchAction &&
                  <SpinnerIconButton
                    className={styles.action}
                    name={icons.SEARCH}
                    title={translate('SearchForMonitoredBooks')}
                    isSpinning={isSearchingBook}
                    onPress={onSearchPress}
                  />
              }

              <IconButton
                className={styles.action}
                name={icons.INTERACTIVE}
                title={translate('EditAuthor')}
                onPress={this.onEditAuthorPress}
              />

              <IconButton
                className={styles.action}
                name={icons.EDIT}
                title={translate('EditBook')}
                onPress={this.onEditBookPress}
              />
            </Label>

            <Link
              className={styles.link}
              style={elementStyle}
              to={link}
            >
              <AuthorPoster
                className={styles.poster}
                style={elementStyle}
                images={images}
                coverType={'cover'}
                size={250}
                lazy={false}
                overflow={true}
                blurBackground={true}
                onError={this.onPosterLoadError}
                onLoad={this.onPosterLoad}
              />

              {
                hasPosterError &&
                  <div className={styles.overlayTitle}>
                    {title}
                  </div>
              }

            </Link>
          </div>

          <BookIndexProgressBar
            monitored={monitored}
            bookCount={bookCount}
            bookFileCount={bookFileCount}
            totalBookCount={totalBookCount}
            posterWidth={posterWidth}
            detailedProgressBar={detailedProgressBar}
          />

          {
            showTitle &&
              <div className={styles.title}>
                {title}
              </div>
          }

          {
            showAuthor &&
              <div className={styles.title}>
                {author.authorName}
              </div>
          }

          {
            showMonitored &&
              <div className={styles.title}>
                {monitored ? 'Monitored' : 'Unmonitored'}
              </div>
          }

          {showQualityProfile && !!qualityProfile?.name ? (
            <div className={styles.title} title={translate('QualityProfile')}>
              {qualityProfile.name}
            </div>
          ) : null}

          {
            nextAiring &&
              <div className={styles.nextAiring}>
                {
                  getRelativeDate(
                    nextAiring,
                    shortDateFormat,
                    showRelativeDates,
                    {
                      timeFormat,
                      timeForToday: true
                    }
                  )
                }
              </div>
          }
          <BookIndexPosterInfo
            author={author}
            bookFileCount={bookFileCount}
            sizeOnDisk={sizeOnDisk}
            qualityProfile={qualityProfile}
            showQualityProfile={showQualityProfile}
            showRelativeDates={showRelativeDates}
            shortDateFormat={shortDateFormat}
            timeFormat={timeFormat}
            {...otherProps}
          />

          <EditAuthorModalConnector
            isOpen={isEditAuthorModalOpen}
            authorId={authorId}
            onModalClose={this.onEditAuthorModalClose}
            onDeleteAuthorPress={this.onDeleteAuthorPress}
          />

          <DeleteAuthorModal
            isOpen={isDeleteAuthorModalOpen}
            authorId={authorId}
            onModalClose={this.onDeleteAuthorModalClose}
          />

          <EditBookModalConnector
            isOpen={isEditBookModalOpen}
            authorId={authorId}
            bookId={id}
            onModalClose={this.onEditBookModalClose}
          />
        </div>
      </div>
    );
  }
}

BookIndexPoster.propTypes = {
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  authorId: PropTypes.number.isRequired,
  author: PropTypes.object.isRequired,
  monitored: PropTypes.bool.isRequired,
  titleSlug: PropTypes.string.isRequired,
  nextAiring: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  posterWidth: PropTypes.number.isRequired,
  posterHeight: PropTypes.number.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired,
  showTitle: PropTypes.bool.isRequired,
  showAuthor: PropTypes.bool.isRequired,
  showMonitored: PropTypes.bool.isRequired,
  showQualityProfile: PropTypes.bool.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  isRefreshingBook: PropTypes.bool.isRequired,
  isSearchingBook: PropTypes.bool.isRequired,
  onRefreshBookPress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

BookIndexPoster.defaultProps = {
  statistics: {
    bookCount: 0,
    bookFileCount: 0,
    totalBookCount: 0
  }
};

export default BookIndexPoster;
