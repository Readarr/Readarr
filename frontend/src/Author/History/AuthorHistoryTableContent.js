import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AuthorHistoryRowConnector from './AuthorHistoryRowConnector';
import styles from './AuthorHistoryTableContent.css';

const columns = [
  {
    name: 'eventType',
    isVisible: true
  },
  {
    name: 'book',
    label: 'Book',
    isVisible: true
  },
  {
    name: 'sourceTitle',
    label: 'Source Title',
    isVisible: true
  },
  {
    name: 'quality',
    label: 'Quality',
    isVisible: true
  },
  {
    name: 'date',
    label: 'Date',
    isVisible: true
  },
  {
    name: 'details',
    label: 'Details',
    isVisible: true
  },
  {
    name: 'actions',
    label: 'Actions',
    isVisible: true
  }
];

class AuthorHistoryTableContent extends Component {

  //
  // Render

  render() {
    const {
      bookId,
      isFetching,
      isPopulated,
      error,
      items,
      onMarkAsFailedPress
    } = this.props;

    const fullAuthor = bookId == null;
    const hasItems = !!items.length;

    return (
      <div>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && !!error &&
            <Alert kind={kinds.DANGER}>
              {translate('UnableToLoadHistory')}
            </Alert>
        }

        {
          isPopulated && !hasItems && !error &&
            <div className={styles.blankpad}>
              {translate('NoHistory')}
            </div>
        }

        {
          isPopulated && hasItems && !error &&
            <Table columns={columns}>
              <TableBody>
                {
                  items.map((item) => {
                    return (
                      <AuthorHistoryRowConnector
                        key={item.id}
                        fullAuthor={fullAuthor}
                        {...item}
                        onMarkAsFailedPress={onMarkAsFailedPress}
                      />
                    );
                  })
                }
              </TableBody>
            </Table>
        }
      </div>
    );
  }
}

AuthorHistoryTableContent.propTypes = {
  bookId: PropTypes.number,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  onMarkAsFailedPress: PropTypes.func.isRequired
};

export default AuthorHistoryTableContent;
