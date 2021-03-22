import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { inputTypes } from 'Helpers/Props';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';

const logLevelOptions = [
  { key: 'info', value: 'Info' },
  { key: 'debug', value: 'Debug' },
  { key: 'trace', value: 'Trace' }
];

class DevelopmentSettings extends Component {

  //
  // Render

  render() {
    const {
      isFetching,
      error,
      settings,
      hasSettings,
      onInputChange,
      onSavePress,
      ...otherProps
    } = this.props;

    return (
      <PageContent title="Development">
        <SettingsToolbarConnector
          {...otherProps}
          onSavePress={onSavePress}
        />

        <PageContentBody>
          {
            isFetching &&
              <LoadingIndicator />
          }

          {
            !isFetching && error &&
              <div>
                Unable to load Development settings
              </div>
          }

          {
            hasSettings && !isFetching && !error &&
              <Form
                id="developmentSettings"
                {...otherProps}
              >
                <FieldSet legend="Logging">
                  <FormGroup>
                    <FormLabel>Log Rotation</FormLabel>

                    <FormInputGroup
                      type={inputTypes.NUMBER}
                      name="logRotate"
                      helpText="Max number of log files to keep saved in logs folder"
                      onChange={onInputChange}
                      {...settings.logRotate}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel>Console Log Level</FormLabel>
                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="consoleLogLevel"
                      values={logLevelOptions}
                      onChange={onInputChange}
                      {...settings.consoleLogLevel}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel>Log SQL</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="logSql"
                      helpText="Log all SQL queries from Readarr"
                      onChange={onInputChange}
                      {...settings.logSql}
                    />
                  </FormGroup>
                </FieldSet>

                <FieldSet legend="Analytics">
                  <FormGroup>
                    <FormLabel>Filter Analytics Events</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="filterSentryEvents"
                      helpText="Filter known user error events from being sent as Analytics"
                      onChange={onInputChange}
                      {...settings.filterSentryEvents}
                    />
                  </FormGroup>
                </FieldSet>
              </Form>
          }
        </PageContentBody>
      </PageContent>
    );
  }

}

DevelopmentSettings.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default DevelopmentSettings;
