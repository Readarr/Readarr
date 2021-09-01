import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import BookIndexOverviewOptionsModalContentConnector from './BookIndexOverviewOptionsModalContentConnector';

function BookIndexOverviewOptionsModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <BookIndexOverviewOptionsModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

BookIndexOverviewOptionsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default BookIndexOverviewOptionsModal;
