import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputButton from 'Components/Form/FormInputButton';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import NamingModal from './NamingModal';
import styles from './Naming.css';

class Naming extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isNamingModalOpen: false,
      namingModalOptions: null
    };
  }

  //
  // Listeners

  onStandardNamingModalOpenClick = () => {
    this.setState({
      isNamingModalOpen: true,
      namingModalOptions: {
        name: 'standardBookFormat',
        book: true,
        additional: true
      }
    });
  };

  onAuthorFolderNamingModalOpenClick = () => {
    this.setState({
      isNamingModalOpen: true,
      namingModalOptions: {
        name: 'authorFolderFormat'
      }
    });
  };

  onNamingModalClose = () => {
    this.setState({ isNamingModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      advancedSettings,
      isFetching,
      error,
      settings,
      hasSettings,
      examples,
      examplesPopulated,
      onInputChange
    } = this.props;

    const {
      isNamingModalOpen,
      namingModalOptions
    } = this.state;

    const renameBooks = hasSettings && settings.renameBooks.value;
    const replaceIllegalCharacters = hasSettings && settings.replaceIllegalCharacters.value;

    const colonReplacementOptions = [
      { key: 0, value: translate('Delete') },
      { key: 1, value: translate('ReplaceWithDash') },
      { key: 2, value: translate('ReplaceWithSpaceDash') },
      { key: 3, value: translate('ReplaceWithSpaceDashSpace') },
      { key: 4, value: translate('SmartReplace'), hint: translate('DashOrSpaceDashDependingOnName') }
    ];

    const standardBookFormatHelpTexts = [];
    const standardBookFormatErrors = [];
    const authorFolderFormatHelpTexts = [];
    const authorFolderFormatErrors = [];

    if (examplesPopulated) {
      if (examples.singleBookExample) {
        standardBookFormatHelpTexts.push(`Single Book: ${examples.singleBookExample}`);
      } else {
        standardBookFormatErrors.push({ message: 'Single Book: Invalid Format' });
      }

      if (examples.multiPartBookExample) {
        standardBookFormatHelpTexts.push(`Multi-part Book: ${examples.multiPartBookExample}`);
      } else {
        standardBookFormatErrors.push({ message: 'Multi-part Book: Invalid Format' });
      }

      if (examples.authorFolderExample) {
        authorFolderFormatHelpTexts.push(`Example: ${examples.authorFolderExample}`);
      } else {
        authorFolderFormatErrors.push({ message: 'Invalid Format' });
      }
    }

    return (
      <FieldSet legend={translate('BookNaming')}>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && error &&
            <Alert kind={kinds.DANGER}>
              {translate('UnableToLoadNamingSettings')}
            </Alert>
        }

        {
          hasSettings && !isFetching && !error &&
            <Form>
              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>
                  {translate('RenameBooks')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="renameBooks"
                  helpText={translate('RenameBooksHelpText')}
                  onChange={onInputChange}
                  {...settings.renameBooks}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>
                  {translate('ReplaceIllegalCharacters')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="replaceIllegalCharacters"
                  helpText={translate('ReplaceIllegalCharactersHelpText')}
                  onChange={onInputChange}
                  {...settings.replaceIllegalCharacters}
                />
              </FormGroup>

              {
                replaceIllegalCharacters ?
                  <FormGroup>
                    <FormLabel>
                      {translate('ColonReplacement')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="colonReplacementFormat"
                      values={colonReplacementOptions}
                      onChange={onInputChange}
                      {...settings.colonReplacementFormat}
                    />
                  </FormGroup> :
                  null
              }

              {
                renameBooks &&
                  <div>
                    <FormGroup size={sizes.LARGE}>
                      <FormLabel>
                        {translate('StandardBookFormat')}
                      </FormLabel>

                      <FormInputGroup
                        inputClassName={styles.namingInput}
                        type={inputTypes.TEXT}
                        name="standardBookFormat"
                        buttons={<FormInputButton onPress={this.onStandardNamingModalOpenClick}>?</FormInputButton>}
                        onChange={onInputChange}
                        {...settings.standardBookFormat}
                        helpTexts={standardBookFormatHelpTexts}
                        errors={[...standardBookFormatErrors, ...settings.standardBookFormat.errors]}
                      />
                    </FormGroup>
                  </div>
              }

              <FormGroup
                advancedSettings={advancedSettings}
                isAdvanced={true}
              >
                <FormLabel>
                  {translate('AuthorFolderFormat')}
                </FormLabel>

                <FormInputGroup
                  inputClassName={styles.namingInput}
                  type={inputTypes.TEXT}
                  name="authorFolderFormat"
                  buttons={<FormInputButton onPress={this.onAuthorFolderNamingModalOpenClick}>?</FormInputButton>}
                  onChange={onInputChange}
                  {...settings.authorFolderFormat}
                  helpTexts={['Used when adding a new author or moving an author via the author editor', ...authorFolderFormatHelpTexts]}
                  errors={[...authorFolderFormatErrors, ...settings.authorFolderFormat.errors]}
                />
              </FormGroup>

              {
                namingModalOptions &&
                  <NamingModal
                    isOpen={isNamingModalOpen}
                    advancedSettings={advancedSettings}
                    {...namingModalOptions}
                    value={settings[namingModalOptions.name].value}
                    onInputChange={onInputChange}
                    onModalClose={this.onNamingModalClose}
                  />
              }
            </Form>
        }
      </FieldSet>
    );
  }

}

Naming.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  examples: PropTypes.object.isRequired,
  examplesPopulated: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default Naming;
