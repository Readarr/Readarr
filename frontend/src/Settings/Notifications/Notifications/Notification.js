import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditNotificationModalConnector from './EditNotificationModalConnector';
import styles from './Notification.css';

class Notification extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditNotificationModalOpen: false,
      isDeleteNotificationModalOpen: false
    };
  }

  //
  // Listeners

  onEditNotificationPress = () => {
    this.setState({ isEditNotificationModalOpen: true });
  }

  onEditNotificationModalClose = () => {
    this.setState({ isEditNotificationModalOpen: false });
  }

  onDeleteNotificationPress = () => {
    this.setState({
      isEditNotificationModalOpen: false,
      isDeleteNotificationModalOpen: true
    });
  }

  onDeleteNotificationModalClose= () => {
    this.setState({ isDeleteNotificationModalOpen: false });
  }

  onConfirmDeleteNotification = () => {
    this.props.onConfirmDeleteNotification(this.props.id);
  }

  //
  // Render

  render() {
    const {
      id,
      name,
      onGrab,
      onReleaseImport,
      onUpgrade,
      onRename,
      onAuthorDelete,
      onBookDelete,
      onBookFileDelete,
      onBookFileDeleteForUpgrade,
      onHealthIssue,
      onDownloadFailure,
      onImportFailure,
      onBookRetag,
      supportsOnGrab,
      supportsOnReleaseImport,
      supportsOnUpgrade,
      supportsOnRename,
      supportsOnAuthorDelete,
      supportsOnBookDelete,
      supportsOnBookFileDelete,
      supportsOnBookFileDeleteForUpgrade,
      supportsOnHealthIssue,
      supportsOnDownloadFailure,
      supportsOnImportFailure,
      supportsOnBookRetag
    } = this.props;

    return (
      <Card
        className={styles.notification}
        overlayContent={true}
        onPress={this.onEditNotificationPress}
      >
        <div className={styles.name}>
          {name}
        </div>

        {
          supportsOnGrab && onGrab ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnGrab')}
            </Label> :
            null
        }

        {
          supportsOnReleaseImport && onReleaseImport ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnReleaseImport')}
            </Label> :
            null
        }

        {
          supportsOnUpgrade && onReleaseImport && onUpgrade ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnUpgrade')}
            </Label> :
            null
        }

        {
          supportsOnRename && onRename ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnRename')}
            </Label> :
            null
        }

        {
          supportsOnBookRetag && onBookRetag ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnBookTagUpdate')}
            </Label> :
            null
        }

        {
          supportsOnAuthorDelete && onAuthorDelete ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnAuthorDelete')}
            </Label> :
            null
        }

        {
          supportsOnBookDelete && onBookDelete ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnBookDelete')}
            </Label> :
            null
        }

        {
          supportsOnBookFileDelete && onBookFileDelete ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnBookFileDelete')}
            </Label> :
            null
        }

        {
          supportsOnBookFileDeleteForUpgrade && onBookFileDelete && onBookFileDeleteForUpgrade ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnBookFileDeleteForUpgrade')}
            </Label> :
            null
        }

        {
          supportsOnHealthIssue && onHealthIssue ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnHealthIssue')}
            </Label> :
            null
        }

        {
          supportsOnDownloadFailure && onDownloadFailure ?
            <Label kind={kinds.SUCCESS} >
              {translate('OnDownloadFailure')}
            </Label> :
            null
        }

        {
          supportsOnImportFailure && onImportFailure ?
            <Label kind={kinds.SUCCESS} >
              {translate('OnImportFailure')}
            </Label> :
            null
        }

        {
          !onGrab && !onReleaseImport && !onRename && !onBookRetag && !onHealthIssue && !onDownloadFailure && !onImportFailure ?
            <Label
              kind={kinds.DISABLED}
              outline={true}
            >
              {translate('Disabled')}
            </Label> :
            null
        }

        <EditNotificationModalConnector
          id={id}
          isOpen={this.state.isEditNotificationModalOpen}
          onModalClose={this.onEditNotificationModalClose}
          onDeleteNotificationPress={this.onDeleteNotificationPress}
        />

        <ConfirmModal
          isOpen={this.state.isDeleteNotificationModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteNotification')}
          message={translate('DeleteNotificationMessageText', [name])}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDeleteNotification}
          onCancel={this.onDeleteNotificationModalClose}
        />
      </Card>
    );
  }
}

Notification.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  onGrab: PropTypes.bool.isRequired,
  onReleaseImport: PropTypes.bool.isRequired,
  onUpgrade: PropTypes.bool.isRequired,
  onRename: PropTypes.bool.isRequired,
  onAuthorDelete: PropTypes.bool.isRequired,
  onBookDelete: PropTypes.bool.isRequired,
  onBookFileDelete: PropTypes.bool.isRequired,
  onBookFileDeleteForUpgrade: PropTypes.bool.isRequired,
  onHealthIssue: PropTypes.bool.isRequired,
  onDownloadFailure: PropTypes.bool.isRequired,
  onImportFailure: PropTypes.bool.isRequired,
  onBookRetag: PropTypes.bool.isRequired,
  supportsOnGrab: PropTypes.bool.isRequired,
  supportsOnReleaseImport: PropTypes.bool.isRequired,
  supportsOnUpgrade: PropTypes.bool.isRequired,
  supportsOnRename: PropTypes.bool.isRequired,
  supportsOnAuthorDelete: PropTypes.bool.isRequired,
  supportsOnBookDelete: PropTypes.bool.isRequired,
  supportsOnBookFileDelete: PropTypes.bool.isRequired,
  supportsOnBookFileDeleteForUpgrade: PropTypes.bool.isRequired,
  supportsOnHealthIssue: PropTypes.bool.isRequired,
  supportsOnDownloadFailure: PropTypes.bool.isRequired,
  supportsOnImportFailure: PropTypes.bool.isRequired,
  supportsOnBookRetag: PropTypes.bool.isRequired,
  onConfirmDeleteNotification: PropTypes.func.isRequired
};

export default Notification;
