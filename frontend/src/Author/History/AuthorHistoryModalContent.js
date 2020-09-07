import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import AuthorHistoryTableContent from './AuthorHistoryTableContent';

class AuthorHistoryModalContent extends Component {

  //
  // Render

  render() {
    const {
      onModalClose
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          History
        </ModalHeader>

        <ModalBody>
          <AuthorHistoryTableContent
            {...this.props}
          />
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            Close
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AuthorHistoryModalContent.propTypes = {
  onModalClose: PropTypes.func.isRequired
};

export default AuthorHistoryModalContent;
