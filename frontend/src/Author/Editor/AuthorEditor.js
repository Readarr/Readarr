import PropTypes from 'prop-types';
import React, { Component } from 'react';
import NoAuthor from 'Author/NoAuthor';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import FilterMenu from 'Components/Menu/FilterMenu';
import PageContent from 'Components/Page/PageContent';
import PageContentBodyConnector from 'Components/Page/PageContentBodyConnector';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import { align, icons, sortDirections } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import RetagAuthorModal from './AudioTags/RetagAuthorModal';
import AuthorEditorFilterModalConnector from './AuthorEditorFilterModalConnector';
import AuthorEditorFooter from './AuthorEditorFooter';
import AuthorEditorRowConnector from './AuthorEditorRowConnector';
import OrganizeAuthorModal from './Organize/OrganizeAuthorModal';

function getColumns(showMetadataProfile) {
  return [
    {
      name: 'status',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'sortName',
      label: 'Name',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'qualityProfileId',
      label: 'Quality Profile',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'metadataProfileId',
      label: 'Metadata Profile',
      isSortable: true,
      isVisible: showMetadataProfile
    },
    {
      name: 'path',
      label: 'Path',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'tags',
      label: 'Tags',
      isSortable: false,
      isVisible: true
    }
  ];
}

class AuthorEditor extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {},
<<<<<<< HEAD:frontend/src/Author/Editor/AuthorEditor.js
      isOrganizingAuthorModalOpen: false,
      isRetaggingAuthorModalOpen: false,
      columns: getColumns(props.showMetadataProfile)
=======
      isOrganizingArtistModalOpen: false,
      isRetaggingArtistModalOpen: false
>>>>>>> Mass Editor size and options:frontend/src/Artist/Editor/ArtistEditor.js
    };
  }

  componentDidUpdate(prevProps) {
    const {
      isDeleting,
      deleteError
    } = this.props;

    const hasFinishedDeleting = prevProps.isDeleting &&
                                !isDeleting &&
                                !deleteError;

    if (hasFinishedDeleting) {
      this.onSelectAllChange({ value: false });
    }
  }

  //
  // Control

  getSelectedIds = () => {
    return getSelectedIds(this.state.selectedState);
  }

  //
  // Listeners

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  }

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    this.setState((state) => {
      return toggleSelected(state, this.props.items, id, value, shiftKey);
    });
  }

  onSaveSelected = (changes) => {
    this.props.onSaveSelected({
      authorIds: this.getSelectedIds(),
      ...changes
    });
  }

  onOrganizeAuthorPress = () => {
    this.setState({ isOrganizingAuthorModalOpen: true });
  }

  onOrganizeAuthorModalClose = (organized) => {
    this.setState({ isOrganizingAuthorModalOpen: false });

    if (organized === true) {
      this.onSelectAllChange({ value: false });
    }
  }

  onRetagAuthorPress = () => {
    this.setState({ isRetaggingAuthorModalOpen: true });
  }

  onRetagAuthorModalClose = (organized) => {
    this.setState({ isRetaggingAuthorModalOpen: false });

    if (organized === true) {
      this.onSelectAllChange({ value: false });
    }
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
      isSaving,
      saveError,
      isDeleting,
      deleteError,
<<<<<<< HEAD:frontend/src/Author/Editor/AuthorEditor.js
      isOrganizingAuthor,
      isRetaggingAuthor,
      showMetadataProfile,
=======
      isOrganizingArtist,
      isRetaggingArtist,
      onTableOptionChange,
>>>>>>> Mass Editor size and options:frontend/src/Artist/Editor/ArtistEditor.js
      onSortPress,
      onFilterSelect
    } = this.props;

    const {
      allSelected,
      allUnselected,
      selectedState
    } = this.state;

    const selectedAuthorIds = this.getSelectedIds();

    return (
      <PageContent title="Author Editor">
        <PageToolbar>
          <PageToolbarSection />
          <PageToolbarSection alignContent={align.RIGHT}>
            <TableOptionsModalWrapper
              columns={columns}
              onTableOptionChange={onTableOptionChange}
            >
              <PageToolbarButton
                label="Options"
                iconName={icons.TABLE}
              />
            </TableOptionsModalWrapper>

            <PageToolbarSeparator />

            <FilterMenu
              alignMenu={align.RIGHT}
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={customFilters}
              filterModalConnectorComponent={AuthorEditorFilterModalConnector}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBodyConnector>
          {
            isFetching && !isPopulated &&
              <LoadingIndicator />
          }

          {
            !isFetching && !!error &&
              <div>{getErrorMessage(error, 'Failed to load author from API')}</div>
          }

          {
            !error && isPopulated && !!items.length &&
              <div>
                <Table
                  columns={columns}
                  sortKey={sortKey}
                  sortDirection={sortDirection}
                  selectAll={true}
                  allSelected={allSelected}
                  allUnselected={allUnselected}
                  onSortPress={onSortPress}
                  onSelectAllChange={this.onSelectAllChange}
                >
                  <TableBody>
                    {
                      items.map((item) => {
                        return (
                          <AuthorEditorRowConnector
                            key={item.id}
                            {...item}
                            columns={columns}
                            isSelected={selectedState[item.id]}
                            onSelectedChange={this.onSelectedChange}
                          />
                        );
                      })
                    }
                  </TableBody>
                </Table>
              </div>
          }

          {
            !error && isPopulated && !items.length &&
              <NoAuthor totalItems={totalItems} />
          }
        </PageContentBodyConnector>

        <AuthorEditorFooter
          authorIds={selectedAuthorIds}
          selectedCount={selectedAuthorIds.length}
          isSaving={isSaving}
          saveError={saveError}
          isDeleting={isDeleting}
          deleteError={deleteError}
<<<<<<< HEAD:frontend/src/Author/Editor/AuthorEditor.js
          isOrganizingAuthor={isOrganizingAuthor}
          isRetaggingAuthor={isRetaggingAuthor}
          showMetadataProfile={showMetadataProfile}
=======
          isOrganizingArtist={isOrganizingArtist}
          isRetaggingArtist={isRetaggingArtist}
          columns={columns}
          showMetadataProfile={columns.find((column) => column.name === 'metadataProfileId').isVisible}
>>>>>>> Mass Editor size and options:frontend/src/Artist/Editor/ArtistEditor.js
          onSaveSelected={this.onSaveSelected}
          onOrganizeAuthorPress={this.onOrganizeAuthorPress}
          onRetagAuthorPress={this.onRetagAuthorPress}
        />

        <OrganizeAuthorModal
          isOpen={this.state.isOrganizingAuthorModalOpen}
          authorIds={selectedAuthorIds}
          onModalClose={this.onOrganizeAuthorModalClose}
        />

        <RetagAuthorModal
          isOpen={this.state.isRetaggingAuthorModalOpen}
          authorIds={selectedAuthorIds}
          onModalClose={this.onRetagAuthorModalClose}
        />

      </PageContent>
    );
  }
}

AuthorEditor.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  totalItems: PropTypes.number.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
<<<<<<< HEAD:frontend/src/Author/Editor/AuthorEditor.js
  isOrganizingAuthor: PropTypes.bool.isRequired,
  isRetaggingAuthor: PropTypes.bool.isRequired,
  showMetadataProfile: PropTypes.bool.isRequired,
=======
  isOrganizingArtist: PropTypes.bool.isRequired,
  isRetaggingArtist: PropTypes.bool.isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
>>>>>>> Mass Editor size and options:frontend/src/Artist/Editor/ArtistEditor.js
  onSortPress: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  onSaveSelected: PropTypes.func.isRequired
};

export default AuthorEditor;
