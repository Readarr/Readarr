import PropTypes from 'prop-types';
import React from 'react';
import AuthorMetadataProfilePopoverContent from 'AddAuthor/AuthorMetadataProfilePopoverContent';
import AuthorMonitoringOptionsPopoverContent from 'AddAuthor/AuthorMonitoringOptionsPopoverContent';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Popover from 'Components/Tooltip/Popover';
import { calibreProfiles, icons, inputTypes, kinds, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './EditRootFolderModalContent.css';

function EditRootFolderModalContent(props) {

  const {
    advancedSettings,
    isFetching,
    error,
    isSaving,
    saveError,
    item,
    onInputChange,
    onModalClose,
    onSavePress,
    onDeleteRootFolderPress,
    showMetadataProfile,
    ...otherProps
  } = props;

  const {
    id,
    name,
    path,
    defaultQualityProfileId,
    defaultMetadataProfileId,
    defaultMonitorOption,
    defaultTags,
    isCalibreLibrary,
    host,
    port,
    urlBase,
    username,
    password,
    library,
    outputFormat,
    outputProfile,
    useSsl
  } = item;

  const profileHelpText = calibreProfiles.options.find((x) => x.key === outputProfile.value).description;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? 'Edit Root Folder' : 'Add Root Folder'}
      </ModalHeader>

      <ModalBody>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && !!error &&
            <div>
              {translate('UnableToAddANewRootFolderPleaseTryAgain')}
            </div>
        }

        {
          !isFetching && !error &&
            <Form {...otherProps}>
              <FormGroup>
                <FormLabel>
                  {translate('Name')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="name"
                  {...name}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('Path')}
                </FormLabel>

                <FormInputGroup
                  type={id ? inputTypes.TEXT : inputTypes.PATH}
                  readOnly={!!id}
                  name="path"
                  helpText={translate('PathHelpText')}
                  helpTextWarning={translate('PathHelpTextWarning')}
                  {...path}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Monitor

                  <Popover
                    anchor={
                      <Icon
                        className={styles.labelIcon}
                        name={icons.INFO}
                      />
                    }
                    title={translate('MonitoringOptions')}
                    body={<AuthorMonitoringOptionsPopoverContent />}
                    position={tooltipPositions.RIGHT}
                  />
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.MONITOR_BOOKS_SELECT}
                  name="defaultMonitorOption"
                  onChange={onInputChange}
                  {...defaultMonitorOption}
                  helpText={translate('DefaultMonitorOptionHelpText')}
                />

              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('QualityProfile')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.QUALITY_PROFILE_SELECT}
                  name="defaultQualityProfileId"
                  helpText={translate('DefaultQualityProfileIdHelpText')}
                  {...defaultQualityProfileId}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup className={showMetadataProfile ? undefined : styles.hideMetadataProfile}>
                <FormLabel>
                  Metadata Profile
                  <Popover
                    anchor={
                      <Icon
                        className={styles.labelIcon}
                        name={icons.INFO}
                      />
                    }
                    title={translate('MetadataProfile')}
                    body={<AuthorMetadataProfilePopoverContent />}
                    position={tooltipPositions.RIGHT}
                  />
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.METADATA_PROFILE_SELECT}
                  name="defaultMetadataProfileId"
                  helpText={translate('DefaultMetadataProfileIdHelpText')}
                  {...defaultMetadataProfileId}
                  includeNone={true}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('DefaultReadarrTags')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TAG}
                  name="defaultTags"
                  helpText={translate('DefaultTagsHelpText')}
                  {...defaultTags}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Use Calibre Content Server
                  <Popover
                    anchor={
                      <Icon
                        className={styles.labelIcon}
                        name={icons.INFO}
                      />
                    }
                    title={translate('CalibreContentServer')}
                    body={translate('CalibreContentServerText')}
                    position={tooltipPositions.RIGHT}
                  />
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  isDisabled={!!id}
                  name="isCalibreLibrary"
                  helpText={translate('IsCalibreLibraryHelpText')}
                  {...isCalibreLibrary}
                  onChange={onInputChange}
                  helpLink={'https://manual.calibre-ebook.com/server.html'}
                />
              </FormGroup>

              {
                isCalibreLibrary !== undefined && isCalibreLibrary.value &&
                  <div>
                    <FormGroup>
                      <FormLabel>
                        {translate('CalibreHost')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.TEXT}
                        name="host"
                        helpText={translate('HostHelpText')}
                        {...host}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <FormGroup>
                      <FormLabel>
                        {translate('CalibrePort')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.NUMBER}
                        name="port"
                        helpText={translate('PortHelpText')}
                        {...port}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <FormGroup
                      advancedSettings={advancedSettings}
                      isAdvanced={true}
                    >
                      <FormLabel>
                        {translate('CalibreUrlBase')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.TEXT}
                        name="urlBase"
                        helpText={translate('UrlBaseHelpText')}
                        {...urlBase}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <FormGroup>
                      <FormLabel>
                        {translate('CalibreUsername')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.TEXT}
                        name="username"
                        helpText={translate('UsernameHelpText')}
                        {...username}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <FormGroup>
                      <FormLabel>
                        {translate('CalibrePassword')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.PASSWORD}
                        name="password"
                        helpText={translate('PasswordHelpText')}
                        {...password}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <FormGroup>
                      <FormLabel>
                        {translate('CalibreLibrary')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.TEXT}
                        name="library"
                        helpText={translate('LibraryHelpText')}
                        {...library}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <FormGroup>
                      <FormLabel>
                        Convert to format
                        <Popover
                          anchor={
                            <Icon
                              className={styles.labelIcon}
                              name={icons.INFO}
                            />
                          }
                          title={translate('CalibreOutputFormat')}
                          body={'Specify the output format.  Options are: MOBI, EPUB, AZW3, DOCX, FB2, HTMLZ, LIT, LRF, PDB, PDF, PMLZ, RB, RTF, SNB, TCR, TXT, TXTZ, ZIP'}
                          position={tooltipPositions.RIGHT}
                        />
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.TEXT}
                        name="outputFormat"
                        helpText={translate('OutputFormatHelpText')}
                        {...outputFormat}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <FormGroup>
                      <FormLabel>
                        Calibre Output Profile
                        <Popover
                          anchor={
                            <Icon
                              className={styles.labelIcon}
                              name={icons.INFO}
                            />
                          }
                          title={translate('CalibreOutputProfile')}
                          body={'Specify the output profile. The output profile tells the Calibre conversion system how to optimize the created document for the specified device (such as by resizing images for the device screen size). In some cases, an output profile can be used to optimize the output for a particular device, but this is rarely necessary.'}
                          position={tooltipPositions.RIGHT}
                        />
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.SELECT}
                        name="outputProfile"
                        values={calibreProfiles.options}
                        helpText={profileHelpText}
                        {...outputProfile}
                        onChange={onInputChange}
                      />
                    </FormGroup>

                    <FormGroup>
                      <FormLabel>
                        {translate('UseSSL')}
                      </FormLabel>

                      <FormInputGroup
                        type={inputTypes.CHECK}
                        name="useSsl"
                        helpText={translate('UseSslHelpText')}
                        {...useSsl}
                        onChange={onInputChange}
                      />
                    </FormGroup>
                  </div>
              }

            </Form>
        }
      </ModalBody>
      <ModalFooter>
        {
          id &&
            <Button
              className={styles.deleteButton}
              kind={kinds.DANGER}
              onPress={onDeleteRootFolderPress}
            >
              Delete
            </Button>
        }

        <Button
          onPress={onModalClose}
        >
          Cancel
        </Button>

        <SpinnerErrorButton
          isSpinning={isSaving}
          error={saveError}
          onPress={onSavePress}
        >
          Save
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

EditRootFolderModalContent.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  showMetadataProfile: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onDeleteRootFolderPress: PropTypes.func
};

export default EditRootFolderModalContent;
