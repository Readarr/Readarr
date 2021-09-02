import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { Tab, TabList, TabPanel, Tabs } from 'react-tabs';
import DeleteAuthorModal from 'Author/Delete/DeleteAuthorModal';
import EditAuthorModalConnector from 'Author/Edit/EditAuthorModalConnector';
import AuthorHistoryTable from 'Author/History/AuthorHistoryTable';
import MonitoringOptionsModal from 'Author/MonitoringOptions/MonitoringOptionsModal';
import BookFileEditorTable from 'BookFile/Editor/BookFileEditorTable';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import SwipeHeaderConnector from 'Components/Swipe/SwipeHeaderConnector';
import { align, icons } from 'Helpers/Props';
import InteractiveSearchFilterMenuConnector from 'InteractiveSearch/InteractiveSearchFilterMenuConnector';
import InteractiveSearchTable from 'InteractiveSearch/InteractiveSearchTable';
import OrganizePreviewModalConnector from 'Organize/OrganizePreviewModalConnector';
import RetagPreviewModalConnector from 'Retag/RetagPreviewModalConnector';
import translate from 'Utilities/String/translate';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import InteractiveImportModal from '../../InteractiveImport/InteractiveImportModal';
import AuthorDetailsHeaderConnector from './AuthorDetailsHeaderConnector';
import AuthorDetailsSeasonConnector from './AuthorDetailsSeasonConnector';
import AuthorDetailsSeriesConnector from './AuthorDetailsSeriesConnector';
import styles from './AuthorDetails.css';

function getExpandedState(newState) {
  return {
    allExpanded: newState.allSelected,
    allCollapsed: newState.allUnselected,
    expandedState: newState.selectedState
  };
}

class AuthorDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isRetagModalOpen: false,
      isEditAuthorModalOpen: false,
      isDeleteAuthorModalOpen: false,
      isInteractiveImportModalOpen: false,
      isMonitorOptionsModalOpen: false,
      allExpanded: false,
      allCollapsed: false,
      expandedState: {},
      selectedTabIndex: 0
    };
  }

  //
  // Listeners

  onOrganizePress = () => {
    this.setState({ isOrganizeModalOpen: true });
  }

  onOrganizeModalClose = () => {
    this.setState({ isOrganizeModalOpen: false });
  }

  onRetagPress = () => {
    this.setState({ isRetagModalOpen: true });
  }

  onRetagModalClose = () => {
    this.setState({ isRetagModalOpen: false });
  }

  onInteractiveImportPress = () => {
    this.setState({ isInteractiveImportModalOpen: true });
  }

  onInteractiveImportModalClose = () => {
    this.setState({ isInteractiveImportModalOpen: false });
  }

  onEditAuthorPress = () => {
    this.setState({ isEditAuthorModalOpen: true });
  }

  onEditAuthorModalClose = () => {
    this.setState({ isEditAuthorModalOpen: false });
  }

  onDeleteAuthorPress = () => {
    this.setState({
      isEditAuthorModalOpen: false,
      isDeleteAuthorModalOpen: true
    });
  }

  onDeleteAuthorModalClose = () => {
    this.setState({ isDeleteAuthorModalOpen: false });
  }

  onMonitorOptionsPress = () => {
    this.setState({ isMonitorOptionsModalOpen: true });
  }

  onMonitorOptionsClose = () => {
    this.setState({ isMonitorOptionsModalOpen: false });
  }

  onExpandAllPress = () => {
    const {
      allExpanded,
      expandedState
    } = this.state;

    this.setState(getExpandedState(selectAll(expandedState, !allExpanded)));
  }

  onExpandPress = (bookId, isExpanded) => {
    this.setState((state) => {
      const convertedState = {
        allSelected: state.allExpanded,
        allUnselected: state.allCollapsed,
        selectedState: state.expandedState
      };

      const newState = toggleSelected(convertedState, [], bookId, isExpanded, false);

      return getExpandedState(newState);
    });
  }

  onTabSelect = (index, lastIndex) => {
    this.setState({ selectedTabIndex: index });
  }

  //
  // Render

  render() {
    const {
      id,
      authorName,
      path,
      monitored,
      isRefreshing,
      isSearching,
      isFetching,
      isPopulated,
      booksError,
      bookFilesError,
      hasBooks,
      hasMonitoredBooks,
      hasSeries,
      series,
      hasBookFiles,
      previousAuthor,
      nextAuthor,
      onRefreshPress,
      onSearchPress
    } = this.props;

    const {
      isOrganizeModalOpen,
      isRetagModalOpen,
      isEditAuthorModalOpen,
      isDeleteAuthorModalOpen,
      isInteractiveImportModalOpen,
      isMonitorOptionsModalOpen,
      allExpanded,
      allCollapsed,
      expandedState,
      selectedTabIndex
    } = this.state;

    let expandIcon = icons.EXPAND_INDETERMINATE;

    if (allExpanded) {
      expandIcon = icons.COLLAPSE;
    } else if (allCollapsed) {
      expandIcon = icons.EXPAND;
    }

    return (
      <PageContent title={authorName}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('RefreshScan')}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              title={translate('RefreshInformationAndScanDisk')}
              isSpinning={isRefreshing}
              onPress={onRefreshPress}
            />

            <PageToolbarButton
              label={translate('SearchMonitored')}
              iconName={icons.SEARCH}
              isDisabled={!monitored || !hasMonitoredBooks || !hasBooks}
              isSpinning={isSearching}
              title={hasMonitoredBooks ? undefined : translate('HasMonitoredBooksNoMonitoredBooksForThisAuthor')}
              onPress={onSearchPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('PreviewRename')}
              iconName={icons.ORGANIZE}
              isDisabled={!hasBookFiles}
              onPress={this.onOrganizePress}
            />

            <PageToolbarButton
              label={translate('PreviewRetag')}
              iconName={icons.RETAG}
              isDisabled={!hasBookFiles}
              onPress={this.onRetagPress}
            />

            <PageToolbarButton
              label={translate('ManualImport')}
              iconName={icons.INTERACTIVE}
              onPress={this.onInteractiveImportPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('BookMonitoring')}
              iconName={icons.MONITORED}
              onPress={this.onMonitorOptionsPress}
            />

            <PageToolbarButton
              label={translate('Edit')}
              iconName={icons.EDIT}
              onPress={this.onEditAuthorPress}
            />

            <PageToolbarButton
              label={translate('Delete')}
              iconName={icons.DELETE}
              onPress={this.onDeleteAuthorPress}
            />
          </PageToolbarSection>

          <PageToolbarSection alignContent={align.RIGHT}>
            <PageToolbarButton
              label={allExpanded ? translate('AllExpandedCollapseAll') : translate('AllExpandedExpandAll')}
              iconName={expandIcon}
              onPress={this.onExpandAllPress}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody innerClassName={styles.innerContentBody}>
          <SwipeHeaderConnector
            className={styles.header}
            nextLink={`/author/${nextAuthor.titleSlug}`}
            nextComponent={(width) => <AuthorDetailsHeaderConnector authorId={nextAuthor.id} width={width} />}
            prevLink={`/author/${previousAuthor.titleSlug}`}
            prevComponent={(width) => <AuthorDetailsHeaderConnector authorId={previousAuthor.id} width={width} />}
            currentComponent={(width) => <AuthorDetailsHeaderConnector authorId={id} width={width} />}
          >
            <div className={styles.authorNavigationButtons}>
              <IconButton
                className={styles.authorNavigationButton}
                name={icons.ARROW_LEFT}
                size={30}
                title={translate('GoToInterp', [previousAuthor.authorName])}
                to={`/author/${previousAuthor.titleSlug}`}
              />

              <IconButton
                className={styles.authorUpButton}
                name={icons.ARROW_UP}
                size={30}
                title={translate('GoToAuthorListing')}
                to={{
                  pathname: '/',
                  state: { restoreScrollPosition: true }
                }}
              />

              <IconButton
                className={styles.authorNavigationButton}
                name={icons.ARROW_RIGHT}
                size={30}
                title={translate('GoToInterp', [nextAuthor.authorName])}
                to={`/author/${nextAuthor.titleSlug}`}
              />
            </div>
          </SwipeHeaderConnector>

          <div className={styles.contentContainer}>
            {
              !isPopulated && !booksError && !bookFilesError &&
                <LoadingIndicator />
            }

            {
              !isFetching && booksError &&
                <div>
                  {translate('LoadingBooksFailed')}
                </div>
            }

            {
              !isFetching && bookFilesError &&
                <div>
                  {translate('LoadingBookFilesFailed')}
                </div>
            }

            {
              isPopulated &&
                <Tabs selectedIndex={this.state.tabIndex} onSelect={this.onTabSelect}>
                  <TabList
                    className={styles.tabList}
                  >
                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      Books
                    </Tab>

                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      Series
                    </Tab>

                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      History
                    </Tab>

                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      Search
                    </Tab>

                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      Files
                    </Tab>

                    {
                      selectedTabIndex === 3 &&
                        <div className={styles.filterIcon}>
                          <InteractiveSearchFilterMenuConnector
                            type="author"
                          />
                        </div>
                    }
                  </TabList>

                  <TabPanel>
                    <AuthorDetailsSeasonConnector
                      authorId={id}
                      isExpanded={true}
                      onExpandPress={this.onExpandPress}
                    />
                  </TabPanel>

                  <TabPanel>
                    {
                      isPopulated && hasSeries &&
                        <div>
                          {
                            series.map((item) => {
                              return (
                                <AuthorDetailsSeriesConnector
                                  key={item.id}
                                  seriesId={item.id}
                                  authorId={id}
                                  isExpanded={expandedState[item.id]}
                                  onExpandPress={this.onExpandPress}
                                />
                              );
                            })
                          }
                        </div>
                    }
                  </TabPanel>

                  <TabPanel>
                    <AuthorHistoryTable
                      authorId={id}
                    />
                  </TabPanel>

                  <TabPanel>
                    <InteractiveSearchTable
                      type="author"
                      authorId={id}
                    />
                  </TabPanel>

                  <TabPanel>
                    <BookFileEditorTable
                      authorId={id}
                    />
                  </TabPanel>
                </Tabs>
            }

          </div>

          <div className={styles.metadataMessage}>
            {translate('TooManyBooks')}
            <Link to='/settings/profiles'> {translate('MetadataProfile')} </Link>
            or manually
            <Link to={`/add/search?term=${encodeURIComponent(authorName)}`}> {translate('Search')} </Link>
            for new items!
          </div>

          <OrganizePreviewModalConnector
            isOpen={isOrganizeModalOpen}
            authorId={id}
            onModalClose={this.onOrganizeModalClose}
          />

          <RetagPreviewModalConnector
            isOpen={isRetagModalOpen}
            authorId={id}
            onModalClose={this.onRetagModalClose}
          />

          <EditAuthorModalConnector
            isOpen={isEditAuthorModalOpen}
            authorId={id}
            onModalClose={this.onEditAuthorModalClose}
            onDeleteAuthorPress={this.onDeleteAuthorPress}
          />

          <DeleteAuthorModal
            isOpen={isDeleteAuthorModalOpen}
            authorId={id}
            onModalClose={this.onDeleteAuthorModalClose}
          />

          <InteractiveImportModal
            isOpen={isInteractiveImportModalOpen}
            authorId={id}
            folder={path}
            allowAuthorChange={false}
            showFilterExistingFiles={true}
            showImportMode={false}
            onModalClose={this.onInteractiveImportModalClose}
          />

          <MonitoringOptionsModal
            isOpen={isMonitorOptionsModalOpen}
            authorId={id}
            onModalClose={this.onMonitorOptionsClose}
          />
        </PageContentBody>
      </PageContent>
    );
  }
}

AuthorDetails.propTypes = {
  id: PropTypes.number.isRequired,
  authorName: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  path: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  overview: PropTypes.string,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  alternateTitles: PropTypes.arrayOf(PropTypes.string).isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isRefreshing: PropTypes.bool.isRequired,
  isSearching: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  booksError: PropTypes.object,
  bookFilesError: PropTypes.object,
  hasBooks: PropTypes.bool.isRequired,
  hasMonitoredBooks: PropTypes.bool.isRequired,
  hasSeries: PropTypes.bool.isRequired,
  series: PropTypes.arrayOf(PropTypes.object).isRequired,
  hasBookFiles: PropTypes.bool.isRequired,
  previousAuthor: PropTypes.object.isRequired,
  nextAuthor: PropTypes.object.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onMonitorTogglePress: PropTypes.func.isRequired,
  onRefreshPress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired
};

AuthorDetails.defaultProps = {
  statistics: {},
  tags: [],
  isSaving: false
};

export default AuthorDetails;
