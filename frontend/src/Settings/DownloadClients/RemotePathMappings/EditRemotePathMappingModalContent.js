import PropTypes from 'prop-types';
import React from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import { stringSettingShape } from 'Helpers/Props/Shapes/settingShape';
import translate from 'Utilities/String/translate';
import styles from './EditRemotePathMappingModalContent.css';

function EditRemotePathMappingModalContent(props) {
  const {
    id,
    isFetching,
    error,
    isSaving,
    saveError,
    item,
    downloadClientHosts,
    onInputChange,
    onSavePress,
    onModalClose,
    onDeleteRemotePathMappingPress,
    ...otherProps
  } = props;

  const {
    host,
    remotePath,
    localPath
  } = item;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? 'Edit Remote Path Mapping' : 'Add Remote Path Mapping'}
      </ModalHeader>

      <ModalBody className={styles.body}>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && !!error &&
            <div>
              {translate('UnableToAddANewRemotePathMappingPleaseTryAgain')}
            </div>
        }

        {
          !isFetching && !error &&
            <Form {...otherProps}>
              <FormGroup>
                <FormLabel>
                  {translate('Host')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="host"
                  helpText={translate('HostHelpText')}
                  {...host}
                  values={downloadClientHosts}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('SettingsRemotePathMappingRemotePath')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="remotePath"
                  helpText={translate('SettingsRemotePathMappingRemotePathHelpText')}
                  {...remotePath}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('SettingsRemotePathMappingLocalPath')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.PATH}
                  name="localPath"
                  helpText={translate('SettingsRemotePathMappingLocalPathHelpText')}
                  {...localPath}
                  onChange={onInputChange}
                />
              </FormGroup>
            </Form>
        }
      </ModalBody>

      <ModalFooter>
        {
          id &&
            <Button
              className={styles.deleteButton}
              kind={kinds.DANGER}
              onPress={onDeleteRemotePathMappingPress}
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

const remotePathMappingShape = {
  host: PropTypes.shape(stringSettingShape).isRequired,
  remotePath: PropTypes.shape(stringSettingShape).isRequired,
  localPath: PropTypes.shape(stringSettingShape).isRequired
};

EditRemotePathMappingModalContent.propTypes = {
  id: PropTypes.number,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.shape(remotePathMappingShape).isRequired,
  downloadClientHosts: PropTypes.arrayOf(PropTypes.object).isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onDeleteRemotePathMappingPress: PropTypes.func
};

export default EditRemotePathMappingModalContent;
