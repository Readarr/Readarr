import React from 'react';
import AuthorHistoryContentConnector from 'Author/History/AuthorHistoryContentConnector';
import AuthorHistoryTableContent from 'Author/History/AuthorHistoryTableContent';
import styles from './AuthorHistoryTable.css';

function AuthorHistoryTable(props) {
  const {
    ...otherProps
  } = props;

  return (
    <div className={styles.container}>
      <AuthorHistoryContentConnector
        component={AuthorHistoryTableContent}
        {...otherProps}
      />
    </div>
  );
}

AuthorHistoryTable.propTypes = {
};

export default AuthorHistoryTable;
