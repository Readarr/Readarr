import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBodyConnector from 'Components/Page/PageContentBodyConnector';
import { inputTypes, sizes } from 'Helpers/Props';
import RemotePathMappingsConnector from 'Settings/DownloadClients/RemotePathMappings/RemotePathMappingsConnector';
import PageContentBody from 'Components/Page/PageContentBody';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import NamingConnector from './Naming/NamingConnector';
import RootFoldersConnector from './RootFolder/RootFoldersConnector';

const rescanAfterRefreshOptions = [
  { key: 'always', value: 'Always' },
  { key: 'afterManual', value: 'After Manual Refresh' },
  { key: 'never', value: 'Never' }
];

const allowFingerprintingOptions = [
  { key: 'allFiles', value: 'Always' },
  { key: 'newFiles', value: 'For new imports only' },
  { key: 'never', value: 'Never' }
];

const downloadPropersAndRepacksOptions = [
  { key: 'preferAndUpgrade', value: 'Prefer and Upgrade' },
  { key: 'doNotUpgrade', value: 'Do not Upgrade Automatically' },
  { key: 'doNotPrefer', value: 'Do not Prefer' }
];

const fileDateOptions = [
  { key: 'none', value: 'None' },
  { key: 'bookReleaseDate', value: 'Book Release Date' }
];

class MediaManagement extends Component {

  //
  // Render

  render() {
    const {
      advancedSettings,
      isFetching,
      error,
      settings,
      hasSettings,
      isWindows,
      onInputChange,
      onSavePress,
      ...otherProps
    } = this.props;

    return (
      <PageContent title="Media Management Settings">
        <SettingsToolbarConnector
          advancedSettings={advancedSettings}
          {...otherProps}
          onSavePress={onSavePress}
        />

        <PageContentBodyConnector>
          <RootFoldersConnector />
          <RemotePathMappingsConnector />
          <NamingConnector />

          {
            isFetching &&
              <FieldSet legend="Naming Settings">
                <LoadingIndicator />
              </FieldSet>
          }

          {
            !isFetching && error &&
              <FieldSet legend="Naming Settings">
                <div>Unable to load Media Management settings</div>
              </FieldSet>
          }

          {
            hasSettings && !isFetching && !error &&
              <Form
                id="mediaManagementSettings"
                {...otherProps}
              >
                {
                  advancedSettings &&
                    <FieldSet legend="Folders">
                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                        size={sizes.MEDIUM}
                      >
                        <FormLabel>Create empty author folders</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="createEmptyAuthorFolders"
                          helpText="Create missing author folders during disk scan"
                          onChange={onInputChange}
                          {...settings.createEmptyAuthorFolders}
                        />
                      </FormGroup>

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                        size={sizes.MEDIUM}
                      >
                        <FormLabel>Delete empty folders</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="deleteEmptyFolders"
                          helpText="Delete empty author folders during disk scan and when book files are deleted"
                          onChange={onInputChange}
                          {...settings.deleteEmptyFolders}
                        />
                      </FormGroup>
                    </FieldSet>
                }

                {
                  advancedSettings &&
                    <FieldSet
                      legend="Importing"
                    >
                      {
                        !isWindows &&
                          <FormGroup
                            advancedSettings={advancedSettings}
                            isAdvanced={true}
                            size={sizes.MEDIUM}
                          >
                            <FormLabel>Skip Free Space Check</FormLabel>

                            <FormInputGroup
                              type={inputTypes.CHECK}
                              name="skipFreeSpaceCheckWhenImporting"
                              helpText="Use when Readarr is unable to detect free space from your author root folder"
                              onChange={onInputChange}
                              {...settings.skipFreeSpaceCheckWhenImporting}
                            />
                          </FormGroup>
                      }

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                        size={sizes.MEDIUM}
                      >
                        <FormLabel>Minimum Free Space</FormLabel>

                        <FormInputGroup
                          type={inputTypes.NUMBER}
                          unit='MB'
                          name="minimumFreeSpaceWhenImporting"
                          helpText="Prevent import if it would leave less than this amount of disk space available"
                          onChange={onInputChange}
                          {...settings.minimumFreeSpaceWhenImporting}
                        />
                      </FormGroup>

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                        size={sizes.MEDIUM}
                      >
                        <FormLabel>Use Hardlinks instead of Copy</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="copyUsingHardlinks"
                          helpText="Use Hardlinks when trying to copy files from torrents that are still being seeded"
                          helpTextWarning="Occasionally, file locks may prevent renaming files that are being seeded. You may temporarily disable seeding and use Readarr's rename function as a work around."
                          onChange={onInputChange}
                          {...settings.copyUsingHardlinks}
                        />
                      </FormGroup>

                      <FormGroup size={sizes.MEDIUM}>
                        <FormLabel>Import Extra Files</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="importExtraFiles"
                          helpText="Import matching extra files (subtitles, nfo, etc) after importing an book file"
                          onChange={onInputChange}
                          {...settings.importExtraFiles}
                        />
                      </FormGroup>

                      {
                        settings.importExtraFiles.value &&
                          <FormGroup
                            advancedSettings={advancedSettings}
                            isAdvanced={true}
                          >
                            <FormLabel>Import Extra Files</FormLabel>

                            <FormInputGroup
                              type={inputTypes.TEXT}
                              name="extraFileExtensions"
                              helpTexts={[
                                'Comma separated list of extra files to import (.nfo will be imported as .nfo-orig)',
                                'Examples: ".sub, .nfo" or "sub,nfo"'
                              ]}
                              onChange={onInputChange}
                              {...settings.extraFileExtensions}
                            />
                          </FormGroup>
                      }
                    </FieldSet>
                }

                <FieldSet
                  legend="File Management"
                >
                  <FormGroup size={sizes.MEDIUM}>
                    <FormLabel>Ignore Deleted Books</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="autoUnmonitorPreviouslyDownloadedBooks"
                      helpText="Books deleted from disk are automatically unmonitored in Readarr"
                      onChange={onInputChange}
                      {...settings.autoUnmonitorPreviouslyDownloadedBooks}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                    size={sizes.MEDIUM}
                  >
                    <FormLabel>Propers and Repacks</FormLabel>

                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="downloadPropersAndRepacks"
                      helpTexts={[
                        'Whether or not to automatically upgrade to Propers/Repacks',
                        'Use \'Do not Prefer\' to sort by preferred word score over propers/repacks'
                      ]}
                      helpTextWarning={
                        settings.downloadPropersAndRepacks.value === 'doNotPrefer' ?
                          'Use preferred words for automatic upgrades to propers/repacks' :
                          undefined
                      }
                      values={downloadPropersAndRepacksOptions}
                      onChange={onInputChange}
                      {...settings.downloadPropersAndRepacks}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                    size={sizes.MEDIUM}
                  >
                    <FormLabel>Watch Root Folders for file changes</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="watchLibraryForChanges"
                      helpText="Rescan automatically when files change in a root folder"
                      onChange={onInputChange}
                      {...settings.watchLibraryForChanges}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                  >
                    <FormLabel>Rescan Author Folder after Refresh</FormLabel>

                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="rescanAfterRefresh"
                      helpText="Rescan the author folder after refreshing the author"
                      helpTextWarning="Readarr will not automatically detect changes to files when not set to 'Always'"
                      values={rescanAfterRefreshOptions}
                      onChange={onInputChange}
                      {...settings.rescanAfterRefresh}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                  >
                    <FormLabel>Allow Fingerprinting</FormLabel>

                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="allowFingerprinting"
                      helpText="Use fingerprinting to improve accuracy of book matching"
                      helpTextWarning="This requires Readarr to read parts of the file which will slow down scans and may cause high disk or network activity."
                      values={allowFingerprintingOptions}
                      onChange={onInputChange}
                      {...settings.allowFingerprinting}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                  >
                    <FormLabel>Change File Date</FormLabel>

                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="fileDate"
                      helpText="Change file date on import/rescan"
                      values={fileDateOptions}
                      onChange={onInputChange}
                      {...settings.fileDate}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                  >
                    <FormLabel>Recycling Bin</FormLabel>

                    <FormInputGroup
                      type={inputTypes.PATH}
                      name="recycleBin"
                      helpText="Book files will go here when deleted instead of being permanently deleted"
                      onChange={onInputChange}
                      {...settings.recycleBin}
                    />
                  </FormGroup>

                  <FormGroup
                    advancedSettings={advancedSettings}
                    isAdvanced={true}
                  >
                    <FormLabel>Recycling Bin Cleanup</FormLabel>

                    <FormInputGroup
                      type={inputTypes.NUMBER}
                      name="recycleBinCleanupDays"
                      helpText="Set to 0 to disable automatic cleanup"
                      helpTextWarning="Files in the recycle bin older than the selected number of days will be cleaned up automatically"
                      min={0}
                      onChange={onInputChange}
                      {...settings.recycleBinCleanupDays}
                    />
                  </FormGroup>
                </FieldSet>

                {
                  advancedSettings && !isWindows &&
                    <FieldSet
                      legend="Permissions"
                    >
                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                        size={sizes.MEDIUM}
                      >
                        <FormLabel>Set Permissions</FormLabel>

                        <FormInputGroup
                          type={inputTypes.CHECK}
                          name="setPermissionsLinux"
                          helpText="Should chmod be run when files are imported/renamed?"
                          helpTextWarning="If you're unsure what these settings do, do not alter them."
                          onChange={onInputChange}
                          {...settings.setPermissionsLinux}
                        />
                      </FormGroup>

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                      >
                        <FormLabel>chmod Folder</FormLabel>

                        <FormInputGroup
                          type={inputTypes.UMASK}
                          name="chmodFolder"
                          helpText="Octal, applied during import/rename to media folders and files (without execute bits)"
                          helpTextWarning="This only works if the user running Lidarr is the owner of the file. It's better to ensure the download client sets the permissions properly."
                          onChange={onInputChange}
                          {...settings.chmodFolder}
                        />
                      </FormGroup>

                      <FormGroup
                        advancedSettings={advancedSettings}
                        isAdvanced={true}
                      >
                        <FormLabel>chown Group</FormLabel>

                        <FormInputGroup
                          type={inputTypes.TEXT}
                          name="chownGroup"
                          helpText="Group name or gid. Use gid for remote file systems."
                          helpTextWarning="This only works if the user running Readarr is the owner of the file. It's better to ensure the download client uses the same group as Readarr."
                          values={fileDateOptions}
                          onChange={onInputChange}
                          {...settings.chownGroup}
                        />
                      </FormGroup>
                    </FieldSet>
                }
              </Form>
          }
        </PageContentBodyConnector>
      </PageContent>
    );
  }

}

MediaManagement.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  isWindows: PropTypes.bool.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default MediaManagement;
