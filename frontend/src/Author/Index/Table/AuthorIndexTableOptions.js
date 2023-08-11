import PropTypes from 'prop-types';
import React, { Component, Fragment } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
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

class AuthorIndexTableOptions extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      showBanners: props.showBanners,
      showSearchAction: props.showSearchAction,
      showTitle: props.showTitle
    };
  }

  componentDidUpdate(prevProps) {
    const {
      showBanners,
      showSearchAction,
      showTitle
    } = this.props;

    if (
      showBanners !== prevProps.showBanners ||
      showSearchAction !== prevProps.showSearchAction ||
      showTitle !== prevProps.showTitle
    ) {
      this.setState({
        showBanners,
        showSearchAction,
        showTitle
      });
    }
  }

  //
  // Listeners

  onTableOptionChange = ({ name, value }) => {
    this.setState({
      [name]: value
    }, () => {
      this.props.onTableOptionChange({
        tableOptions: {
          ...this.state,
          [name]: value
        }
      });
    });
  };

  //
  // Render

  render() {
    const {
      showBanners,
      showSearchAction,
      showTitle
    } = this.state;

    return (
      <Fragment>
        <FormGroup>
          <FormLabel>
            {translate('NameStyle')}
          </FormLabel>

          <FormInputGroup
            type={inputTypes.SELECT}
            name="showTitle"
            value={showTitle}
            values={nameOptions}
            onChange={this.onTableOptionChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>
            {translate('ShowBanners')}
          </FormLabel>

          <FormInputGroup
            type={inputTypes.CHECK}
            name="showBanners"
            value={showBanners}
            helpText={translate('ShowBannersHelpText')}
            onChange={this.onTableOptionChange}
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
            onChange={this.onTableOptionChange}
          />
        </FormGroup>
      </Fragment>
    );
  }
}

AuthorIndexTableOptions.propTypes = {
  showTitle: PropTypes.string.isRequired,
  showBanners: PropTypes.bool.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  onTableOptionChange: PropTypes.func.isRequired
};

export default AuthorIndexTableOptions;
