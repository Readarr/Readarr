import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

const nameOptions = [
  {
    key: 'firstLast',
    get value() {
      return translate('NameFirstLast');
    }
  },
  {
    key: 'lastFirst',
    get value() {
      return translate('NameLastFirst');
    }
  }
];

const posterSizeOptions = [
  {
    key: 'small',
    get value() {
      return translate('Small');
    }
  },
  {
    key: 'medium',
    get value() {
      return translate('Medium');
    }
  },
  {
    key: 'large',
    get value() {
      return translate('Large');
    }
  }
];

class AuthorIndexOverviewOptionsModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      showTitle: props.showTitle,
      detailedProgressBar: props.detailedProgressBar,
      size: props.size,
      showMonitored: props.showMonitored,
      showQualityProfile: props.showQualityProfile,
      showLastBook: props.showLastBook,
      showAdded: props.showAdded,
      showBookCount: props.showBookCount,
      showPath: props.showPath,
      showSizeOnDisk: props.showSizeOnDisk,
      showSearchAction: props.showSearchAction
    };
  }

  componentDidUpdate(prevProps) {
    const {
      showTitle,
      detailedProgressBar,
      size,
      showMonitored,
      showQualityProfile,
      showLastBook,
      showAdded,
      showBookCount,
      showPath,
      showSizeOnDisk,
      showSearchAction
    } = this.props;

    const state = {};

    if (showTitle !== prevProps.showTitle) {
      state.showTitle = showTitle;
    }

    if (detailedProgressBar !== prevProps.detailedProgressBar) {
      state.detailedProgressBar = detailedProgressBar;
    }

    if (size !== prevProps.size) {
      state.size = size;
    }

    if (showMonitored !== prevProps.showMonitored) {
      state.showMonitored = showMonitored;
    }

    if (showQualityProfile !== prevProps.showQualityProfile) {
      state.showQualityProfile = showQualityProfile;
    }

    if (showLastBook !== prevProps.showLastBook) {
      state.showLastBook = showLastBook;
    }

    if (showAdded !== prevProps.showAdded) {
      state.showAdded = showAdded;
    }

    if (showBookCount !== prevProps.showBookCount) {
      state.showBookCount = showBookCount;
    }

    if (showPath !== prevProps.showPath) {
      state.showPath = showPath;
    }

    if (showSizeOnDisk !== prevProps.showSizeOnDisk) {
      state.showSizeOnDisk = showSizeOnDisk;
    }

    if (showSearchAction !== prevProps.showSearchAction) {
      state.showSearchAction = showSearchAction;
    }

    if (!_.isEmpty(state)) {
      this.setState(state);
    }
  }

  //
  // Listeners

  onChangeOverviewOption = ({ name, value }) => {
    this.setState({
      [name]: value
    }, () => {
      this.props.onChangeOverviewOption({ [name]: value });
    });
  };

  //
  // Render

  render() {
    const {
      onModalClose
    } = this.props;

    const {
      showTitle,
      detailedProgressBar,
      size,
      showMonitored,
      showQualityProfile,
      showLastBook,
      showAdded,
      showBookCount,
      showPath,
      showSizeOnDisk,
      showSearchAction
    } = this.state;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Overview Options
        </ModalHeader>

        <ModalBody>
          <Form>
            <FormGroup>
              <FormLabel>
                {translate('NameStyle')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.SELECT}
                name="showTitle"
                value={showTitle}
                values={nameOptions}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('PosterSize')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.SELECT}
                name="size"
                value={size}
                values={posterSizeOptions}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('DetailedProgressBar')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="detailedProgressBar"
                value={detailedProgressBar}
                helpText={translate('DetailedProgressBarHelpText')}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('ShowMonitored')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showMonitored"
                value={showMonitored}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>

              <FormLabel>
                {translate('ShowQualityProfile')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showQualityProfile"
                value={showQualityProfile}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('ShowLastBook')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showLastBook"
                value={showLastBook}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('ShowDateAdded')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showAdded"
                value={showAdded}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('ShowBookCount')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showBookCount"
                value={showBookCount}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('ShowPath')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showPath"
                value={showPath}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('ShowSizeOnDisk')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showSizeOnDisk"
                value={showSizeOnDisk}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('ShowSearch')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showSearchAction"
                value={showSearchAction}
                helpText={translate('ShowSearchActionHelpText')}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>
          </Form>
        </ModalBody>

        <ModalFooter>
          <Button
            onPress={onModalClose}
          >
            Close
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AuthorIndexOverviewOptionsModalContent.propTypes = {
  showTitle: PropTypes.string.isRequired,
  size: PropTypes.string.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired,
  showMonitored: PropTypes.bool.isRequired,
  showQualityProfile: PropTypes.bool.isRequired,
  showLastBook: PropTypes.bool.isRequired,
  showAdded: PropTypes.bool.isRequired,
  showBookCount: PropTypes.bool.isRequired,
  showPath: PropTypes.bool.isRequired,
  showSizeOnDisk: PropTypes.bool.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  onChangeOverviewOption: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default AuthorIndexOverviewOptionsModalContent;
