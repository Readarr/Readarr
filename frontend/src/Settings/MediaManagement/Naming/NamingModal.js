import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import SelectInput from 'Components/Form/SelectInput';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import NamingOption from './NamingOption';
import styles from './NamingModal.css';

const separatorOptions = [
  { key: ' ', value: 'Space ( )' },
  { key: '.', value: 'Period (.)' },
  { key: '_', value: 'Underscore (_)' },
  { key: '-', value: 'Dash (-)' }
];

const caseOptions = [
  { key: 'title', value: 'Default Case' },
  { key: 'lower', value: 'Lowercase' },
  { key: 'upper', value: 'Uppercase' }
];

const fileNameTokens = [
  {
    token: '{Author Name} - {Book Title} - {Quality Full}',
    example: 'Author Name - Book Title - MP3 Proper'
  },
  {
    token: '{Author.Name}.{Book.Title}.{Quality.Full}',
    example: 'Author.Name.Book.Title.MP3'
  },
  {
    token: '{Author Name} - {Book Title}{ (PartNumber)}',
    example: 'Author Name - Book Title (2)'
  },
  {
    token: '{Author Name} - {Book Title}{ (PartNumber/PartCount)}',
    example: 'Author Name - Book Title (2/10)'
  }
];

const authorTokens = [
  { token: '{Author Name}', example: 'Author\'s Name' },

  { token: '{Author NameThe}', example: 'Author\'s Name, The' },

  { token: '{Author NameFirstCharacter}', example: 'A' },

  { token: '{Author CleanName}', example: 'Authors Name' },

  { token: '{Author SortName}', example: 'Name, Author' },

  { token: '{Author Disambiguation}', example: 'Disambiguation' }
];

const bookTokens = [
  { token: '{Book Title}', example: 'The Book\'s Title!: Subtitle!' },

  { token: '{Book TitleThe}', example: 'Book\'s Title!, The: Subtitle!' },

  { token: '{Book CleanTitle}', example: 'The Books Title!: Subtitle' },

  { token: '{Book TitleNoSub}', example: 'The Book\'s Title!' },

  { token: '{Book TitleTheNoSub}', example: 'Book\'s Title!, The' },

  { token: '{Book CleanTitleNoSub}', example: 'The Books Title!' },

  { token: '{Book Subtitle}', example: 'Subtitle!' },

  { token: '{Book SubtitleThe}', example: 'Subtitle!, The' },

  { token: '{Book CleanSubtitle}', example: 'Subtitle' },

  { token: '{Book Disambiguation}', example: 'Disambiguation' },

  { token: '{Book Series}', example: 'Series Title' },

  { token: '{Book SeriesPosition}', example: '1' },

  { token: '{Book SeriesTitle}', example: 'Series Title #1' },

  { token: '{PartNumber:0}', example: '2' },
  { token: '{PartNumber:00}', example: '02' },
  { token: '{PartCount:0}', example: '9' },
  { token: '{PartCount:00}', example: '09' }
];

const releaseDateTokens = [
  { token: '{Release Year}', example: '2016' },
  { token: '{Release YearFirst}', example: '2015' },
  { token: '{Edition Year}', example: '2016' }
];

const qualityTokens = [
  { token: '{Quality Full}', example: 'AZW3 Proper' },
  { token: '{Quality Title}', example: 'AZW3' }
];

const mediaInfoTokens = [
  { token: '{MediaInfo AudioCodec}', example: 'MP3' },
  { token: '{MediaInfo AudioChannels}', example: '2.0' },
  { token: '{MediaInfo AudioBitRate}', example: '320kbps' },
  { token: '{MediaInfo AudioBitsPerSample}', example: '24bit' },
  { token: '{MediaInfo AudioSampleRate}', example: '44.1kHz' }
];

const otherTokens = [
  { token: '{Release Group}', example: 'Rls Grp' },
  { token: '{Custom Formats}', example: 'iNTERNAL' }
];

const originalTokens = [
  { token: '{Original Title}', example: 'Author.Name.Book.Name.2018.AZW3-EVOLVE' },
  { token: '{Original Filename}', example: '01 - book name' }
];

class NamingModal extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this._selectionStart = null;
    this._selectionEnd = null;

    this.state = {
      separator: ' ',
      case: 'title'
    };
  }

  //
  // Listeners

  onTokenSeparatorChange = (event) => {
    this.setState({ separator: event.value });
  };

  onTokenCaseChange = (event) => {
    this.setState({ case: event.value });
  };

  onInputSelectionChange = (selectionStart, selectionEnd) => {
    this._selectionStart = selectionStart;
    this._selectionEnd = selectionEnd;
  };

  onOptionPress = ({ isFullFilename, tokenValue }) => {
    const {
      name,
      value,
      onInputChange
    } = this.props;

    const selectionStart = this._selectionStart;
    const selectionEnd = this._selectionEnd;

    if (isFullFilename) {
      onInputChange({ name, value: tokenValue });
    } else if (selectionStart == null) {
      onInputChange({
        name,
        value: `${value}${tokenValue}`
      });
    } else {
      const start = value.substring(0, selectionStart);
      const end = value.substring(selectionEnd);
      const newValue = `${start}${tokenValue}${end}`;

      onInputChange({ name, value: newValue });
      this._selectionStart = newValue.length - 1;
      this._selectionEnd = newValue.length - 1;
    }
  };

  //
  // Render

  render() {
    const {
      name,
      value,
      isOpen,
      advancedSettings,
      book,
      additional,
      onInputChange,
      onModalClose
    } = this.props;

    const {
      separator: tokenSeparator,
      case: tokenCase
    } = this.state;

    return (
      <Modal
        isOpen={isOpen}
        onModalClose={onModalClose}
      >
        <ModalContent onModalClose={onModalClose}>
          <ModalHeader>
            File Name Tokens
          </ModalHeader>

          <ModalBody>
            <div className={styles.namingSelectContainer}>
              <SelectInput
                className={styles.namingSelect}
                name="separator"
                value={tokenSeparator}
                values={separatorOptions}
                onChange={this.onTokenSeparatorChange}
              />

              <SelectInput
                className={styles.namingSelect}
                name="case"
                value={tokenCase}
                values={caseOptions}
                onChange={this.onTokenCaseChange}
              />
            </div>

            {
              !advancedSettings &&
                <FieldSet legend={translate('FileNames')}>
                  <div className={styles.groups}>
                    {
                      fileNameTokens.map(({ token, example }) => {
                        return (
                          <NamingOption
                            key={token}
                            name={name}
                            value={value}
                            token={token}
                            example={example}
                            isFullFilename={true}
                            tokenSeparator={tokenSeparator}
                            tokenCase={tokenCase}
                            size={sizes.LARGE}
                            onPress={this.onOptionPress}
                          />
                        );
                      }
                      )
                    }
                  </div>
                </FieldSet>
            }

            <FieldSet legend={translate('Author')}>
              <div className={styles.groups}>
                {
                  authorTokens.map(({ token, example }) => {
                    return (
                      <NamingOption
                        key={token}
                        name={name}
                        value={value}
                        token={token}
                        example={example}
                        tokenSeparator={tokenSeparator}
                        tokenCase={tokenCase}
                        onPress={this.onOptionPress}
                      />
                    );
                  }
                  )
                }
              </div>
            </FieldSet>

            {
              book &&
                <div>
                  <FieldSet legend={translate('Book')}>
                    <div className={styles.groups}>
                      {
                        bookTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('ReleaseDate')}>
                    <div className={styles.groups}>
                      {
                        releaseDateTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>
                </div>
            }

            {
              additional &&
                <div>
                  <FieldSet legend={translate('Quality')}>
                    <div className={styles.groups}>
                      {
                        qualityTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('MediaInfo')}>
                    <div className={styles.groups}>
                      {
                        mediaInfoTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('Other')}>
                    <div className={styles.groups}>
                      {
                        otherTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('Original')}>
                    <div className={styles.groups}>
                      {
                        originalTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              size={sizes.LARGE}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>
                </div>
            }
          </ModalBody>

          <ModalFooter>
            <TextInput
              name={name}
              value={value}
              onChange={onInputChange}
              onSelectionChange={this.onInputSelectionChange}
            />
            <Button onPress={onModalClose}>
              Close
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    );
  }
}

NamingModal.propTypes = {
  name: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired,
  isOpen: PropTypes.bool.isRequired,
  advancedSettings: PropTypes.bool.isRequired,
  book: PropTypes.bool.isRequired,
  additional: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

NamingModal.defaultProps = {
  book: false,
  additional: false
};

export default NamingModal;
