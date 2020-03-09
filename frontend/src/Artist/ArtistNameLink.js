import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function ArtistNameLink({ foreignAuthorId, artistName }) {
  const link = `/artist/${foreignAuthorId}`;

  return (
    <Link to={link}>
      {artistName}
    </Link>
  );
}

ArtistNameLink.propTypes = {
  foreignAuthorId: PropTypes.string.isRequired,
  artistName: PropTypes.string.isRequired
};

export default ArtistNameLink;
