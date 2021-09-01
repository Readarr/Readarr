import PropTypes from 'prop-types';
import React from 'react';
import AuthorPoster from 'Author/AuthorPoster';
import styles from './BookSearchResult.css';

function BookSearchResult(props) {
  const {
    name,
    images
  } = props;

  return (
    <div className={styles.result}>
      <AuthorPoster
        className={styles.poster}
        images={images}
        coverType={'cover'}
        size={250}
        lazy={false}
        overflow={true}
      />

      <div className={styles.titles}>
        <div className={styles.title}>
          {name}
        </div>
      </div>
    </div>
  );
}

BookSearchResult.propTypes = {
  name: PropTypes.string.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  tags: PropTypes.arrayOf(PropTypes.object).isRequired,
  match: PropTypes.object.isRequired
};

export default BookSearchResult;
