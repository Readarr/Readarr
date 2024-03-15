import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function AuthorNameLink({ titleSlug, authorName, ...otherProps }) {
  const link = `/author/${titleSlug}`;

  return (
    <Link to={link} {...otherProps}>
      {authorName}
    </Link>
  );
}

AuthorNameLink.propTypes = {
  titleSlug: PropTypes.string.isRequired,
  authorName: PropTypes.string.isRequired
};

export default AuthorNameLink;
