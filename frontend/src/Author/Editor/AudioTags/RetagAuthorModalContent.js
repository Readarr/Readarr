import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import CheckInput from 'Components/Form/CheckInput';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons, kinds } from 'Helpers/Props';
import styles from './RetagAuthorModalContent.css';

class RetagAuthorModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      updateCovers: false,
      embedMetadata: false
    };
  }

  //
  // Listeners

  onCheckInputChange = ({ name, value }) => {
    this.setState({ [name]: value });
  }

  onRetagAuthorPress = () => {
    this.props.onRetagAuthorPress(this.state.updateCovers, this.state.embedMetadata);
  }

  //
  // Render

  render() {
    const {
      authorNames,
      onModalClose
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Retag Selected Author
        </ModalHeader>

        <ModalBody>
          <Alert>
            Tip: To preview the tags that will be written... select "Cancel" then click any author name and use the
            <Icon
              className={styles.retagIcon}
              name={icons.RETAG}
            />
          </Alert>

          <div className={styles.message}>
            Are you sure you want to re-tag all files in the {authorNames.length} selected author?
          </div>
          <ul>
            {
              authorNames.map((authorName) => {
                return (
                  <li key={authorName}>
                    {authorName}
                  </li>
                );
              })
            }
          </ul>
        </ModalBody>

        <ModalFooter>
          <label className={styles.searchForNewBookLabelContainer}>
            <span className={styles.searchForNewBookLabel}>
              Update Covers
            </span>

            <CheckInput
              containerClassName={styles.searchForNewBookContainer}
              className={styles.searchForNewBookInput}
              name="updateCovers"
              value={this.state.updateCovers}
              onChange={this.onCheckInputChange}
            />
          </label>

          <label className={styles.searchForNewBookLabelContainer}>
            <span className={styles.searchForNewBookLabel}>
              Embed Metadata
            </span>

            <CheckInput
              containerClassName={styles.searchForNewBookContainer}
              className={styles.searchForNewBookInput}
              name="embedMetadata"
              value={this.state.embedMetadata}
              onChange={this.onCheckInputChange}
            />
          </label>

          <Button onPress={onModalClose}>
            Cancel
          </Button>

          <Button
            kind={kinds.DANGER}
            onPress={this.onRetagAuthorPress}
          >
            Retag
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

RetagAuthorModalContent.propTypes = {
  authorNames: PropTypes.arrayOf(PropTypes.string).isRequired,
  onModalClose: PropTypes.func.isRequired,
  onRetagAuthorPress: PropTypes.func.isRequired
};

export default RetagAuthorModalContent;
