import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function BookNameLink({ titleSlug, title }) {
  const link = `/book/${titleSlug}`;

  return (
    <Link to={link}>
      {title}
    </Link>
  );
}

BookNameLink.propTypes = {
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired
};

export default BookNameLink;
