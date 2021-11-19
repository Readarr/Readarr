import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import DeleteBookModalContentConnector from './DeleteBookModalContentConnector';

function DeleteBookModal(props) {
  const {
    isOpen,
    onModalClose,
    ...otherProps
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <DeleteBookModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

DeleteBookModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default DeleteBookModal;
