import React from 'react';
import BookFileEditorTableContentConnector from './BookFileEditorTableContentConnector';
import styles from './BookFileEditorTable.css';

function BookFileEditorTable(props) {
  const {
    ...otherProps
  } = props;

  return (
    <div className={styles.container}>
      <BookFileEditorTableContentConnector
        {...otherProps}
      />
    </div>
  );
}

export default BookFileEditorTable;
