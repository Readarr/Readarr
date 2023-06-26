import PropTypes from 'prop-types';
import React from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';
import PageSectionContent from 'Components/Page/PageSectionContent';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import TagConnector from './TagConnector';
import styles from './Tags.css';

function Tags(props) {
  const {
    items,
    ...otherProps
  } = props;

  if (!items.length) {
    const wikiLink = <Link to='https://wiki.servarr.com/readarr/settings#tags'>here</Link>;
    return (
      <Alert kind={kinds.INFO}>
        {translate('NoTagsHaveBeenAddedYet', [wikiLink])}
      </Alert>
    );
  }

  return (
    <FieldSet
      legend={translate('Tags')}
    >
      <PageSectionContent
        errorMessage={translate('UnableToLoadTags')}
        {...otherProps}
      >
        <div className={styles.tags}>
          {
            items.map((item) => {
              return (
                <TagConnector
                  key={item.id}
                  {...item}
                />
              );
            })
          }
        </div>
      </PageSectionContent>
    </FieldSet>
  );
}

Tags.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default Tags;
