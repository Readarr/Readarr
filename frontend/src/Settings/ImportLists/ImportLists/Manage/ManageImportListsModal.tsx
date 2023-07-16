import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import ManageImportListsModalContent from './ManageImportListsModalContent';

interface ManageImportListsModalProps {
  isOpen: boolean;
  onModalClose(): void;
}

function ManageImportListsModal(props: ManageImportListsModalProps) {
  const { isOpen, onModalClose } = props;

  return (
    <Modal size={sizes.EXTRA_LARGE} isOpen={isOpen} onModalClose={onModalClose}>
      <ManageImportListsModalContent onModalClose={onModalClose} />
    </Modal>
  );
}

export default ManageImportListsModal;
