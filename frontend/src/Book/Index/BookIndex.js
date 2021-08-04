import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import NoAuthor from 'Author/NoAuthor';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import { align, icons, sortDirections } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import BookIndexFooterConnector from './BookIndexFooterConnector';
import BookIndexFilterMenu from './Menus/BookIndexFilterMenu';
import BookIndexSortMenu from './Menus/BookIndexSortMenu';
import BookIndexViewMenu from './Menus/BookIndexViewMenu';
import BookIndexOverviewsConnector from './Overview/BookIndexOverviewsConnector';
import BookIndexOverviewOptionsModal from './Overview/Options/BookIndexOverviewOptionsModal';
import BookIndexPostersConnector from './Posters/BookIndexPostersConnector';
import BookIndexPosterOptionsModal from './Posters/Options/BookIndexPosterOptionsModal';
import BookIndexTableConnector from './Table/BookIndexTableConnector';
import BookIndexTableOptionsConnector from './Table/BookIndexTableOptionsConnector';
import styles from './BookIndex.css';

function getViewComponent(view) {
  if (view === 'posters') {
    return BookIndexPostersConnector;
  }

  if (view === 'overview') {
    return BookIndexOverviewsConnector;
  }

  return BookIndexTableConnector;
}

class BookIndex extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      scroller: null,
      jumpBarItems: { order: [] },
      jumpToCharacter: null,
      isPosterOptionsModalOpen: false,
      isOverviewOptionsModalOpen: false
    };
  }

  componentDidMount() {
    this.setJumpBarItems();
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      sortKey,
      sortDirection
    } = this.props;

    if (sortKey !== prevProps.sortKey ||
        sortDirection !== prevProps.sortDirection ||
        hasDifferentItemsOrOrder(prevProps.items, items)
    ) {
      this.setJumpBarItems();
    }

    if (this.state.jumpToCharacter != null) {
      this.setState({ jumpToCharacter: null });
    }
  }

  //
  // Control

  setScrollerRef = (ref) => {
    this.setState({ scroller: ref });
  }

  setJumpBarItems() {
    const {
      items,
      sortKey,
      sortDirection,
      isPopulated
    } = this.props;

    // Reset if not sorting by sortName
    if (!isPopulated || (sortKey !== 'title' && sortKey !== 'authorTitle')) {
      this.setState({ jumpBarItems: { order: [] } });
      return;
    }

    const characters = _.reduce(items, (acc, item) => {
      let char = item[sortKey].charAt(0);

      if (!isNaN(char)) {
        char = '#';
      }

      if (char in acc) {
        acc[char] = acc[char] + 1;
      } else {
        acc[char] = 1;
      }

      return acc;
    }, {});

    const order = Object.keys(characters).sort();

    // Reverse if sorting descending
    if (sortDirection === sortDirections.DESCENDING) {
      order.reverse();
    }

    const jumpBarItems = {
      characters,
      order
    };

    this.setState({ jumpBarItems });
  }

  //
  // Listeners

  onPosterOptionsPress = () => {
    this.setState({ isPosterOptionsModalOpen: true });
  }

  onPosterOptionsModalClose = () => {
    this.setState({ isPosterOptionsModalOpen: false });
  }

  onOverviewOptionsPress = () => {
    this.setState({ isOverviewOptionsModalOpen: true });
  }

  onOverviewOptionsModalClose = () => {
    this.setState({ isOverviewOptionsModalOpen: false });
  }

  onJumpBarItemPress = (jumpToCharacter) => {
    this.setState({ jumpToCharacter });
  }

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      error,
      totalItems,
      items,
      columns,
      selectedFilterKey,
      filters,
      customFilters,
      sortKey,
      sortDirection,
      view,
      isRefreshingBook,
      isRssSyncExecuting,
      onScroll,
      onSortSelect,
      onFilterSelect,
      onViewSelect,
      onRefreshAuthorPress,
      onRssSyncPress,
      ...otherProps
    } = this.props;

    const {
      scroller,
      jumpBarItems,
      jumpToCharacter,
      isPosterOptionsModalOpen,
      isOverviewOptionsModalOpen
    } = this.state;

    const ViewComponent = getViewComponent(view);
    const isLoaded = !!(!error && isPopulated && items.length && scroller);
    const hasNoAuthor = !totalItems;

    return (
      <PageContent>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('UpdateAll')}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              isSpinning={isRefreshingBook}
              onPress={onRefreshAuthorPress}
            />

            <PageToolbarButton
              label={translate('RSSSync')}
              iconName={icons.RSS}
              isSpinning={isRssSyncExecuting}
              isDisabled={hasNoAuthor}
              onPress={onRssSyncPress}
            />

          </PageToolbarSection>

          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >
            {
              view === 'table' ?
                <TableOptionsModalWrapper
                  {...otherProps}
                  columns={columns}
                  optionsComponent={BookIndexTableOptionsConnector}
                >
                  <PageToolbarButton
                    label={translate('Options')}
                    iconName={icons.TABLE}
                  />
                </TableOptionsModalWrapper> :
                null
            }

            {
              view === 'posters' ?
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.POSTER}
                  isDisabled={hasNoAuthor}
                  onPress={this.onPosterOptionsPress}
                /> :
                null
            }

            {
              view === 'overview' ?
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.OVERVIEW}
                  isDisabled={hasNoAuthor}
                  onPress={this.onOverviewOptionsPress}
                /> :
                null
            }

            <PageToolbarSeparator />

            <BookIndexViewMenu
              view={view}
              isDisabled={hasNoAuthor}
              onViewSelect={onViewSelect}
            />

            <BookIndexSortMenu
              sortKey={sortKey}
              sortDirection={sortDirection}
              isDisabled={hasNoAuthor}
              onSortSelect={onSortSelect}
            />

            <BookIndexFilterMenu
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={customFilters}
              isDisabled={hasNoAuthor}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>

        <div className={styles.pageContentBodyWrapper}>
          <PageContentBody
            registerScroller={this.setScrollerRef}
            className={styles.contentBody}
            innerClassName={styles[`${view}InnerContentBody`]}
            onScroll={onScroll}
          >
            {
              isFetching && !isPopulated &&
                <LoadingIndicator />
            }

            {
              !isFetching && !!error &&
                <div className={styles.errorMessage}>
                  {getErrorMessage(error, 'Failed to load books from API')}
                </div>
            }

            {
              isLoaded &&
                <div className={styles.contentBodyContainer}>
                  <ViewComponent
                    scroller={scroller}
                    items={items}
                    filters={filters}
                    sortKey={sortKey}
                    sortDirection={sortDirection}
                    jumpToCharacter={jumpToCharacter}
                    {...otherProps}
                  />

                  <BookIndexFooterConnector />
                </div>
            }

            {
              !error && isPopulated && !items.length &&
                <NoAuthor
                  totalItems={totalItems}
                  itemType={'books'}
                />
            }
          </PageContentBody>

          {
            isLoaded && !!jumpBarItems.order.length &&
              <PageJumpBar
                items={jumpBarItems}
                onItemPress={this.onJumpBarItemPress}
              />
          }
        </div>

        <BookIndexPosterOptionsModal
          isOpen={isPosterOptionsModalOpen}
          onModalClose={this.onPosterOptionsModalClose}
        />

        <BookIndexOverviewOptionsModal
          isOpen={isOverviewOptionsModalOpen}
          onModalClose={this.onOverviewOptionsModalClose}

        />
      </PageContent>
    );
  }
}

BookIndex.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  totalItems: PropTypes.number.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  view: PropTypes.string.isRequired,
  isRefreshingBook: PropTypes.bool.isRequired,
  isRssSyncExecuting: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onSortSelect: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  onViewSelect: PropTypes.func.isRequired,
  onRefreshAuthorPress: PropTypes.func.isRequired,
  onRssSyncPress: PropTypes.func.isRequired,
  onScroll: PropTypes.func.isRequired
};

export default BookIndex;
