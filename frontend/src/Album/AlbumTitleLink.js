import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function AlbumTitleLink({ foreignBookId, title, disambiguation }) {
  const link = `/album/${foreignBookId}`;

  return (
    <Link to={link}>
      {title}{disambiguation ? ` (${disambiguation})` : ''}
    </Link>
  );
}

AlbumTitleLink.propTypes = {
  foreignBookId: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  disambiguation: PropTypes.string
};

export default AlbumTitleLink;
