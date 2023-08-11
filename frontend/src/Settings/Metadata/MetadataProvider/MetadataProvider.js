import PropTypes from 'prop-types';
import React from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

const writeAudioTagOptions = [
  {
    key: 'no',
    get value() {
      return translate('WriteTagsNo');
    }
  },
  {
    key: 'sync',
    get value() {
      return translate('WriteTagsSync');
    }
  },
  {
    key: 'allFiles',
    get value() {
      return translate('WriteTagsAll');
    }
  },
  {
    key: 'newFiles',
    get value() {
      return translate('WriteTagsNew');
    }
  }
];

const writeBookTagOptions = [
  {
    key: 'sync',
    get value() {
      return translate('WriteTagsSync');
    }
  },
  {
    key: 'allFiles',
    get value() {
      return translate('WriteTagsAll');
    }
  },
  {
    key: 'newFiles',
    get value() {
      return translate('WriteTagsNew');
    }
  }
];

function MetadataProvider(props) {
  const {
    isFetching,
    error,
    settings,
    hasSettings,
    onInputChange
  } = props;

  return (

    <div>
      {
        isFetching &&
          <LoadingIndicator />
      }

      {
        !isFetching && error &&
          <Alert kind={kinds.DANGER}>
            {translate('UnableToLoadMetadataProviderSettings')}
          </Alert>
      }

      {
        hasSettings && !isFetching && !error &&
          <Form>
            <FieldSet legend={translate('CalibreMetadata')}>
              <FormGroup>
                <FormLabel>
                  {translate('SendMetadataToCalibre')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="writeBookTags"
                  helpTextWarning={translate('WriteBookTagsHelpTextWarning')}
                  helpLink="https://wiki.servarr.com/readarr/settings#write-metadata-to-book-files"
                  values={writeBookTagOptions}
                  onChange={onInputChange}
                  {...settings.writeBookTags}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('UpdateCovers')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="updateCovers"
                  helpText={translate('UpdateCoversHelpText')}
                  onChange={onInputChange}
                  {...settings.updateCovers}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('EmbedMetadataInBookFiles')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="embedMetadata"
                  helpText={translate('EmbedMetadataHelpText')}
                  onChange={onInputChange}
                  {...settings.embedMetadata}
                />
              </FormGroup>

            </FieldSet>

            <FieldSet legend={translate('AudioFileMetadata')}>
              <FormGroup>
                <FormLabel>{translate('WriteAudioTags')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="writeAudioTags"
                  helpTextWarning={translate('WriteBookTagsHelpTextWarning')}
                  helpLink="https://wiki.servarr.com/readarr/settings#write-metadata-to-audio-files"
                  values={writeAudioTagOptions}
                  onChange={onInputChange}
                  {...settings.writeAudioTags}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('WriteAudioTagsScrub')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="scrubAudioTags"
                  helpTextWarning={translate('WriteAudioTagsScrubHelp')}
                  onChange={onInputChange}
                  {...settings.scrubAudioTags}
                />
              </FormGroup>

            </FieldSet>
          </Form>
      }
    </div>

  );
}

MetadataProvider.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default MetadataProvider;
