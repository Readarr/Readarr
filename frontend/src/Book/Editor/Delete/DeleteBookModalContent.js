import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './DeleteBookModalContent.css';

class DeleteBookModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      deleteFiles: false,
      addImportListExclusion: true
    };
  }

  //
  // Listeners

  onDeleteFilesChange = ({ value }) => {
    this.setState({ deleteFiles: value });
  };

  onAddImportListExclusionChange = ({ value }) => {
    this.setState({ addImportListExclusion: value });
  };

  onDeleteBookConfirmed = () => {
    const {
      deleteFiles,
      addImportListExclusion
    } = this.state;

    this.setState({ deleteFiles: false });
    this.props.onDeleteSelectedPress(deleteFiles, addImportListExclusion);
  };

  //
  // Render

  render() {
    const {
      book,
      files,
      onModalClose
    } = this.props;

    const {
      deleteFiles,
      addImportListExclusion
    } = this.state;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Delete Selected Book
        </ModalHeader>

        <ModalBody>
          <div>
            <FormGroup>
              <FormLabel>{`Delete File${book.length > 1 ? 's' : ''}`}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="deleteFiles"
                value={deleteFiles}
                helpText={translate('DeleteFilesHelpText')}
                kind={kinds.DANGER}
                isDisabled={files.length === 0}
                onChange={this.onDeleteFilesChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('AddListExclusion')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="addImportListExclusion"
                value={addImportListExclusion}
                helpText={translate('AddImportListExclusionHelpText')}
                kind={kinds.DANGER}
                onChange={this.onAddImportListExclusionChange}
              />
            </FormGroup>

            {
              !addImportListExclusion &&
                <div className={styles.deleteFilesMessage}>
                  <div>
                    {translate('IfYouDontAddAnImportListExclusionAndTheAuthorHasAMetadataProfileOtherThanNoneThenThisBookMayBeReaddedDuringTheNextAuthorRefresh')}
                  </div>
                </div>
            }

          </div>

          <div className={styles.message}>
            {`Are you sure you want to delete ${book.length} selected book${book.length > 1 ? 's' : ''}${deleteFiles ? ' and their files' : ''}?`}
          </div>

          <ul>
            {
              book.map((s) => {
                return (
                  <li key={s.title}>
                    <span>{s.title}</span>
                  </li>
                );
              })
            }
          </ul>

          {
            deleteFiles &&
              <div>
                <div className={styles.deleteFilesMessage}>
                  {translate('TheFollowingFilesWillBeDeleted')}
                </div>
                <ul>
                  {
                    files.map((s) => {
                      return (
                        <li key={s.path}>
                          <span>{s.path}</span>
                        </li>
                      );
                    })
                  }
                </ul>
              </div>
          }
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            Cancel
          </Button>

          <Button
            kind={kinds.DANGER}
            onPress={this.onDeleteBookConfirmed}
          >
            Delete
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

DeleteBookModalContent.propTypes = {
  book: PropTypes.arrayOf(PropTypes.object).isRequired,
  files: PropTypes.arrayOf(PropTypes.object).isRequired,
  onModalClose: PropTypes.func.isRequired,
  onDeleteSelectedPress: PropTypes.func.isRequired
};

export default DeleteBookModalContent;
