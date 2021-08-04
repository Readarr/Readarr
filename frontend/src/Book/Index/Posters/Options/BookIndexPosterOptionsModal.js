import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import BookIndexPosterOptionsModalContentConnector from './BookIndexPosterOptionsModalContentConnector';

function BookIndexPosterOptionsModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <BookIndexPosterOptionsModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

BookIndexPosterOptionsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default BookIndexPosterOptionsModal;
