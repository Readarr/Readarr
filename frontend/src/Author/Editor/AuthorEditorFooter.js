import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import MoveAuthorModal from 'Author/MoveAuthor/MoveAuthorModal';
import MetadataProfileSelectInputConnector from 'Components/Form/MetadataProfileSelectInputConnector';
import MonitorNewItemsSelectInput from 'Components/Form/MonitorNewItemsSelectInput';
import QualityProfileSelectInputConnector from 'Components/Form/QualityProfileSelectInputConnector';
import RootFolderSelectInputConnector from 'Components/Form/RootFolderSelectInputConnector';
import SelectInput from 'Components/Form/SelectInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { kinds } from 'Helpers/Props';
import { fetchRootFolders } from 'Store/Actions/Settings/rootFolders';
import translate from 'Utilities/String/translate';
import AuthorEditorFooterLabel from './AuthorEditorFooterLabel';
import DeleteAuthorModal from './Delete/DeleteAuthorModal';
import TagsModal from './Tags/TagsModal';
import styles from './AuthorEditorFooter.css';

const NO_CHANGE = 'noChange';

const mapDispatchToProps = {
  dispatchFetchRootFolders: fetchRootFolders
};

class AuthorEditorFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      monitored: NO_CHANGE,
      monitorNewItems: NO_CHANGE,
      qualityProfileId: NO_CHANGE,
      metadataProfileId: NO_CHANGE,
      rootFolderPath: NO_CHANGE,
      savingTags: false,
      isDeleteAuthorModalOpen: false,
      isTagsModalOpen: false,
      isConfirmMoveModalOpen: false,
      destinationRootFolder: null
    };
  }

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchRootFolders();
  }

  componentDidUpdate(prevProps) {
    const {
      isSaving,
      saveError
    } = this.props;

    if (prevProps.isSaving && !isSaving && !saveError) {
      this.setState({
        monitored: NO_CHANGE,
        monitorNewItems: NO_CHANGE,
        qualityProfileId: NO_CHANGE,
        metadataProfileId: NO_CHANGE,
        rootFolderPath: NO_CHANGE,
        savingTags: false
      });
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.setState({ [name]: value });

    if (value === NO_CHANGE) {
      return;
    }

    switch (name) {
      case 'rootFolderPath':
        this.setState({
          isConfirmMoveModalOpen: true,
          destinationRootFolder: value
        });
        break;
      case 'monitored':
        this.props.onSaveSelected({ [name]: value === 'monitored' });
        break;
      default:
        this.props.onSaveSelected({ [name]: value });
    }
  };

  onApplyTagsPress = (tags, applyTags) => {
    this.setState({
      savingTags: true,
      isTagsModalOpen: false
    });

    this.props.onSaveSelected({
      tags,
      applyTags
    });
  };

  onDeleteSelectedPress = () => {
    this.setState({ isDeleteAuthorModalOpen: true });
  };

  onDeleteAuthorModalClose = () => {
    this.setState({ isDeleteAuthorModalOpen: false });
  };

  onTagsPress = () => {
    this.setState({ isTagsModalOpen: true });
  };

  onTagsModalClose = () => {
    this.setState({ isTagsModalOpen: false });
  };

  onSaveRootFolderPress = () => {
    this.setState({
      isConfirmMoveModalOpen: false,
      destinationRootFolder: null
    });

    this.props.onSaveSelected({ rootFolderPath: this.state.destinationRootFolder });
  };

  onMoveAuthorPress = () => {
    this.setState({
      isConfirmMoveModalOpen: false,
      destinationRootFolder: null
    });

    this.props.onSaveSelected({
      rootFolderPath: this.state.destinationRootFolder,
      moveFiles: true
    });
  };

  //
  // Render

  render() {
    const {
      authorIds,
      selectedCount,
      isSaving,
      isDeleting,
      isOrganizingAuthor,
      isRetaggingAuthor,
      onOrganizeAuthorPress,
      onRetagAuthorPress
    } = this.props;

    const {
      monitored,
      monitorNewItems,
      qualityProfileId,
      metadataProfileId,
      rootFolderPath,
      savingTags,
      isTagsModalOpen,
      isDeleteAuthorModalOpen,
      isConfirmMoveModalOpen,
      destinationRootFolder
    } = this.state;

    const monitoredOptions = [
      { key: NO_CHANGE, value: translate('NoChange'), isDisabled: true },
      { key: 'monitored', value: translate('Monitored') },
      { key: 'unmonitored', value: translate('Unmonitored') }
    ];

    return (
      <PageContentFooter>
        <div className={styles.footer}>
          <div className={styles.dropdownContainer}>
            <div className={styles.inputContainer}>
              <AuthorEditorFooterLabel
                label={translate('MonitorAuthor')}
                isSaving={isSaving && monitored !== NO_CHANGE}
              />

              <SelectInput
                name="monitored"
                value={monitored}
                values={monitoredOptions}
                isDisabled={!selectedCount}
                onChange={this.onInputChange}
              />
            </div>

            <div className={styles.inputContainer}>
              <AuthorEditorFooterLabel
                label={translate('MonitorNewItems')}
                isSaving={isSaving && monitored !== NO_CHANGE}
              />

              <MonitorNewItemsSelectInput
                name="monitorNewItems"
                value={monitorNewItems}
                includeNoChange={true}
                isDisabled={!selectedCount}
                onChange={this.onInputChange}
              />
            </div>

            <div className={styles.inputContainer}>
              <AuthorEditorFooterLabel
                label={translate('QualityProfile')}
                isSaving={isSaving && qualityProfileId !== NO_CHANGE}
              />

              <QualityProfileSelectInputConnector
                name="qualityProfileId"
                value={qualityProfileId}
                includeNoChange={true}
                isDisabled={!selectedCount}
                onChange={this.onInputChange}
              />
            </div>

            <div
              className={styles.inputContainer}
            >
              <AuthorEditorFooterLabel
                label={translate('MetadataProfile')}
                isSaving={isSaving && metadataProfileId !== NO_CHANGE}
              />

              <MetadataProfileSelectInputConnector
                name="metadataProfileId"
                value={metadataProfileId}
                includeNoChange={true}
                includeNone={true}
                isDisabled={!selectedCount}
                onChange={this.onInputChange}
              />
            </div>

            <div
              className={styles.inputContainer}
            >
              <AuthorEditorFooterLabel
                label={translate('RootFolder')}
                isSaving={isSaving && rootFolderPath !== NO_CHANGE}
              />

              <RootFolderSelectInputConnector
                name="rootFolderPath"
                value={rootFolderPath}
                includeNoChange={true}
                isDisabled={!selectedCount}
                selectedValueOptions={{ includeFreeSpace: false }}
                onChange={this.onInputChange}
              />
            </div>
          </div>

          <div className={styles.buttonContainer}>
            <div className={styles.buttonContainerContent}>
              <AuthorEditorFooterLabel
                label={translate('SelectedCountAuthorsSelectedInterp', [selectedCount])}
                isSaving={false}
              />

              <div className={styles.buttons}>

                <SpinnerButton
                  className={styles.organizeSelectedButton}
                  kind={kinds.WARNING}
                  isSpinning={isOrganizingAuthor}
                  isDisabled={!selectedCount || isOrganizingAuthor || isRetaggingAuthor}
                  onPress={onOrganizeAuthorPress}
                >
                  {translate('RenameFiles')}
                </SpinnerButton>

                <SpinnerButton
                  className={styles.organizeSelectedButton}
                  kind={kinds.WARNING}
                  isSpinning={isRetaggingAuthor}
                  isDisabled={!selectedCount || isOrganizingAuthor || isRetaggingAuthor}
                  onPress={onRetagAuthorPress}
                >
                  {translate('WriteMetadataTags')}
                </SpinnerButton>

                <SpinnerButton
                  className={styles.tagsButton}
                  isSpinning={isSaving && savingTags}
                  isDisabled={!selectedCount || isOrganizingAuthor || isRetaggingAuthor}
                  onPress={this.onTagsPress}
                >
                  {translate('SetReadarrTags')}
                </SpinnerButton>

                <SpinnerButton
                  className={styles.deleteSelectedButton}
                  kind={kinds.DANGER}
                  isSpinning={isDeleting}
                  isDisabled={!selectedCount || isDeleting}
                  onPress={this.onDeleteSelectedPress}
                >
                  {translate('Delete')}
                </SpinnerButton>

              </div>
            </div>
          </div>
        </div>

        <TagsModal
          isOpen={isTagsModalOpen}
          authorIds={authorIds}
          onApplyTagsPress={this.onApplyTagsPress}
          onModalClose={this.onTagsModalClose}
        />

        <DeleteAuthorModal
          isOpen={isDeleteAuthorModalOpen}
          authorIds={authorIds}
          onModalClose={this.onDeleteAuthorModalClose}
        />

        <MoveAuthorModal
          destinationRootFolder={destinationRootFolder}
          isOpen={isConfirmMoveModalOpen}
          onSavePress={this.onSaveRootFolderPress}
          onMoveAuthorPress={this.onMoveAuthorPress}
        />

      </PageContentFooter>
    );
  }
}

AuthorEditorFooter.propTypes = {
  authorIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  selectedCount: PropTypes.number.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  isOrganizingAuthor: PropTypes.bool.isRequired,
  isRetaggingAuthor: PropTypes.bool.isRequired,
  showMetadataProfile: PropTypes.bool.isRequired,
  onSaveSelected: PropTypes.func.isRequired,
  onOrganizeAuthorPress: PropTypes.func.isRequired,
  onRetagAuthorPress: PropTypes.func.isRequired,
  dispatchFetchRootFolders: PropTypes.func.isRequired
};

export default connect(undefined, mapDispatchToProps)(AuthorEditorFooter);
