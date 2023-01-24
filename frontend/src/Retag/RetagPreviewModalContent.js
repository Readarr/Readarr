import PropTypes from 'prop-types';
import React, { Component } from 'react';
import CheckInput from 'Components/Form/CheckInput';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import RetagPreviewRow from './RetagPreviewRow';
import styles from './RetagPreviewModalContent.css';

function getValue(allSelected, allUnselected) {
  if (allSelected) {
    return true;
  } else if (allUnselected) {
    return false;
  }

  return null;
}

class RetagPreviewModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {},
      updateCovers: false,
      embedMetadata: false
    };
  }

  //
  // Control

  getSelectedIds = () => {
    return getSelectedIds(this.state.selectedState);
  };

  //
  // Listeners

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    this.setState((state) => {
      return toggleSelected(state, this.props.items, id, value, shiftKey);
    });
  };

  onCheckInputChange = ({ name, value }) => {
    this.setState({ [name]: value });
  };

  onRetagPress = () => {
    this.props.onRetagPress(this.getSelectedIds(), this.state.updateCovers, this.state.embedMetadata);
  };

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      error,
      items,
      onModalClose
    } = this.props;

    const {
      allSelected,
      allUnselected,
      selectedState
    } = this.state;

    const selectAllValue = getValue(allSelected, allUnselected);

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Write Metadata Tags
        </ModalHeader>

        <ModalBody>
          {
            isFetching &&
              <LoadingIndicator />
          }

          {
            !isFetching && error &&
              <div>
                {translate('ErrorLoadingPreviews')}
              </div>
          }

          {
            !isFetching && ((isPopulated && !items.length)) &&
              <div>
                {translate('SuccessMyWorkIsDoneNoFilesToRetag')}
              </div>
          }

          {
            !isFetching && isPopulated && !!items.length &&
              <div>
                <div className={styles.previews}>
                  {
                    items.map((item) => {
                      return (
                        <RetagPreviewRow
                          key={item.bookFileId}
                          id={item.bookFileId}
                          path={item.path}
                          changes={item.changes}
                          isSelected={selectedState[item.bookFileId]}
                          onSelectedChange={this.onSelectedChange}
                        />
                      );
                    })
                  }
                </div>
              </div>
          }
        </ModalBody>

        <ModalFooter>
          {
            isPopulated && !!items.length &&
              <CheckInput
                className={styles.selectAllInput}
                containerClassName={styles.selectAllInputContainer}
                name="selectAll"
                value={selectAllValue}
                onChange={this.onSelectAllChange}
              />
          }

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

          <Button
            onPress={onModalClose}
          >
            Cancel
          </Button>

          <Button
            kind={kinds.PRIMARY}
            onPress={this.onRetagPress}
          >
            Retag
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

RetagPreviewModalContent.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  path: PropTypes.string.isRequired,
  onRetagPress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default RetagPreviewModalContent;
