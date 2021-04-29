import PropTypes from 'prop-types';
import React from 'react';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';
import PageSectionContent from 'Components/Page/PageSectionContent';
import translate from 'Utilities/String/translate';
import TagConnector from './TagConnector';
import styles from './Tags.css';

function Tags(props) {
  const {
    items,
    ...otherProps
  } = props;

  if (!items.length) {
    const wikiLink = <Link to='https://wiki.servarr.com/Readarr_Settings#Tags'>here</Link>;
    return (
      <div>
        {translate('NoTagsHaveBeenAddedYet', [wikiLink])}
      </div>
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
